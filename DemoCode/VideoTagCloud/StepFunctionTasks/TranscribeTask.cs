using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Amazon.Lambda.Core;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Amazon.S3;
using Amazon.S3.Model;
using TagCloudCommon;

namespace StepFunctionTasks
{
    public class TranscribeTask
    {
        HttpClient _httpClient = new HttpClient();
        IAmazonTranscribeService _transcribeClient = new AmazonTranscribeServiceClient();

        IAmazonS3 _s3Client = new AmazonS3Client();

        public TranscribeTask()
        {
        }

        public async Task<State> StartTranscribeAsync(State state, ILambdaContext context)
        {
            context.Logger.LogLine($"Starting transcribe {state.VideoName}, S3 location {state.S3Url}");
            var request = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = $"{state.VideoName.Replace(" ", "")}-{Guid.NewGuid().ToString()}",
                LanguageCode = LanguageCode.EnUS,
                MediaFormat = MediaFormat.Mp4,
                Media = new Media
                {
                    MediaFileUri = state.S3Url
                }
            };

            var response = await _transcribeClient.StartTranscriptionJobAsync(request);
            state.TranscriptionJobName = request.TranscriptionJobName;
            context.Logger.LogLine($"Transcribe initiated, job name: {response.TranscriptionJob.TranscriptionJobName}");
            return state;
        }

        public async Task<State> GetStatusForTranscribeAsync(State state, ILambdaContext context)
        {
            var request = new ListTranscriptionJobsRequest
            {
                JobNameContains = state.TranscriptionJobName
            };

            context.Logger.LogLine($"Getting transcription job status: {state.TranscriptionJobName}");
            var response = await this._transcribeClient.ListTranscriptionJobsAsync(request);

            if (response.TranscriptionJobSummaries.Count == 1)
                state.TranscriptionJobStatus = response.TranscriptionJobSummaries[0].TranscriptionJobStatus;

            context.Logger.LogLine($"... {state.TranscriptionJobStatus}");
            return state;
        }

        public async Task<State> ProcessCompletedTranscribeJobsAsync(State state, ILambdaContext context)
        {
            var request = new GetTranscriptionJobRequest
            {
                TranscriptionJobName = state.TranscriptionJobName
            };

            context.Logger.LogLine($"Processing transcription job completion: {state.TranscriptionJobName}");
            var response = await this._transcribeClient.GetTranscriptionJobAsync(request);

            context.Logger.LogLine($"... Download transcript");
            var transcript = await _httpClient.GetStringAsync(response.TranscriptionJob.Transcript.TranscriptFileUri);

            context.Logger.LogLine("... Uploading transcript to S3 bucket");
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = Utilites.BUCKET,
                Key = Utilites.GetTranscriptKey(state.Id),
                ContentBody = transcript
            });

            return state;
        }
    }
}
