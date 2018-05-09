using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TagCloudCommon;
using StepFunctionTasks.TagCloudProcessor;

namespace StepFunctionTasks
{
    public class TagCloudTask
    {
        IAmazonS3 _s3Client = new AmazonS3Client();

        public async Task<State> ComputeTagCloudCategoriesAsync(State state, ILambdaContext context)
        {
            var request = new GetObjectRequest
            {
                BucketName = Utilites.BUCKET,
                Key = Utilites.GetTranscriptKey(state.Id)
            };

            context.Logger.LogLine($"Computing tag cloud categories for {state.VideoName}");
            using (var response = await _s3Client.GetObjectAsync(request))
            using (var reader = new StreamReader(response.ResponseStream))
            {
                var jsonString = await reader.ReadToEndAsync();
                context.Logger.LogLine("... Downloaded transcript");

                var root = JsonConvert.DeserializeObject(jsonString) as JObject;
                var transcripts = root["results"]["transcripts"] as JArray;

                StringBuilder sb = new StringBuilder();
                foreach(JObject transcript in transcripts)
                {
                    sb.Append(transcript["transcript"]?.ToString());
                }

                var content = sb.ToString();
                context.Logger.LogLine($"Parse json transcript file and found transcript with length: {content.Length}");


                var phrases = content.Split(' ', ',');
                var tags = new TagCloudAnalyzer()
                    .ComputeTagCloud(phrases)
                    .Shuffle();
                context.Logger.LogLine($"... Found {tags.Count()} tag categories");


                var tagCloudAnaylsis = JsonConvert.SerializeObject(tags);

                await _s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = Utilites.BUCKET,
                    Key = Utilites.GetTagCloudAnalysisKey(state.Id),
                    ContentBody = tagCloudAnaylsis
                });

                context.Logger.LogLine("... Uploaded tag cloud anaylysis");
            }

            var catalog = new TagCloudCatalog(this._s3Client);

            var items = await catalog.GetCatalogItemsAsync();

            var item = items.FirstOrDefault(x => string.Equals(x.Id, state.Id, StringComparison.InvariantCulture));

            if(item != null)
            {
                item.CompletedDate = DateTime.UtcNow;
                await catalog.RegisterTagCloudAsync(item);
            }


            return state;
        }
    }
}
