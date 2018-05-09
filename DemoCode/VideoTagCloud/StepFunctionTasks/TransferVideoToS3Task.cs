using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using TagCloudCommon;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace StepFunctionTasks
{
    public class TransferVideoToS3Task
    {
        const string TEMP_FILE = "/tmp/video.mp4";
        IAmazonS3 _s3Client = new AmazonS3Client();
        TransferUtility _transfer;

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public TransferVideoToS3Task()
        {
            _transfer = new TransferUtility(this._s3Client);
        }


        public async Task<State> UploadVideoToS3Async(State state, ILambdaContext context)
        {
            // Register the new tag cloud with the tag cloud catalog
            var item = new TagCloudCatalog.TagCloudCatalogItem
            {
                DisplayName = state.VideoName,
                Id = state.Id,
                StartDate = DateTime.UtcNow
            };
            var catalog = new TagCloudCatalog(this._s3Client);
            await catalog.RegisterTagCloudAsync(item);

            context.Logger.LogLine($"Processing Word Cloud Job: {state.VideoName}");
            context.Logger.LogLine($"Video being converted: {state.OriginalUrl}");
            await DownloadAsync(state, context);
            await UploadAsync(state, context);

            return state;
        }

        private async Task DownloadAsync(State state, ILambdaContext context)
        {
            context.Logger.LogLine("Begining Download");
            if (File.Exists(TEMP_FILE))
            {
                context.Logger.LogLine("Deleting existing temp file");
                File.Delete(TEMP_FILE);
            }

            using (var client = new HttpClient())
            using (var inputStream = await client.GetStreamAsync(state.OriginalUrl))
            {
                context.Logger.LogLine("Connection Open");
                using (var outputStream = File.Open(TEMP_FILE, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    context.Logger.LogLine("Starting Download");
                    await inputStream.CopyToAsync(outputStream);
                    context.Logger.LogLine($"Download complete, file size is {new FileInfo(TEMP_FILE).Length}");
                }
            }
        }

        private async Task UploadAsync(State state, ILambdaContext context)
        {
            var request = new TransferUtilityUploadRequest
            {
                BucketName = Utilites.BUCKET,
                Key = Utilites.GetVideoKey(state.Id),
                FilePath = TEMP_FILE
            };

            int lastPercent = 0;
            EventHandler<UploadProgressArgs> callback = (s, e) =>
            {
                if(lastPercent != (int)e.PercentDone)
                {
                    lastPercent = (int)e.PercentDone;
                    context.Logger.LogLine($"... Upload Progress: {lastPercent}%");
                }
            };

            request.UploadProgressEvent += callback;

            context.Logger.LogLine($"Starting upload to S3: {request.BucketName}:{request.Key}");
            await _transfer.UploadAsync(request);
            context.Logger.LogLine($"Upload complete");

            var uri = new Uri(new Uri($"https://{Utilites.BUCKET}.s3.us-east-2.amazonaws.com/"), request.Key);

            state.S3Url = uri.ToString();
        }

        
    }
}
