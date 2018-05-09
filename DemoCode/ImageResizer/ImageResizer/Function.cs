using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using ImageMagick;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ImageResizer
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>


        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }

        HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".png", ".jpg", ".jpeg" };

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandlerAsync(S3Event evnt, ILambdaContext context)
        {
            foreach (var record in evnt.Records)
            {
                var bucket = record.S3.Bucket.Name;
                var key = record.S3.Object.Key;

                if (key.StartsWith("thumbnails"))
                {
                    context.Logger.LogLine($"Object s3://{bucket}/{key} is already a thumbnail");
                    continue;
                }
                if (!SupportedImageTypes.Contains(Path.GetExtension(key.ToLower())))
                {
                    context.Logger.LogLine($"Object s3://{bucket}/{key} is not a supported image type");
                    continue;
                }

                context.Logger.LogLine($"Processing s3://{bucket}/{key}");

                MemoryStream resizedImageStream;
                using (var response = await this.S3Client.GetObjectAsync(bucket, key))
                {
                    using (MagickImageCollection collection = new MagickImageCollection(response.ResponseStream))
                    {
                        foreach (MagickImage image in collection)
                        {
                            image.Resize(200, 200);
                        }
                        context.Logger.LogLine($"   Image resized");

                        resizedImageStream = new MemoryStream();
                        collection.Write(resizedImageStream);
                        resizedImageStream.Position = 0;
                    }
                }

                var index = key.LastIndexOf('/');
                var thumbnailKey = "thumbnails/" + (index != -1 ? key.Substring(index + 1) : key);
                await this.S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = thumbnailKey,
                    InputStream = resizedImageStream
                });

                context.Logger.LogLine($"   Thumbnail saved to s3://{bucket}/{thumbnailKey}");
            }
        }
    }
}
