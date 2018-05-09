using System;
using System.Collections.Generic;
using System.Text;

namespace TagCloudCommon
{
    public static class Utilites
    {
        public static string BUCKET;

        static Utilites()
        {
            BUCKET = Environment.GetEnvironmentVariable("StorageBucket");

            // If not set then use local development bucket;
            if (string.IsNullOrEmpty(BUCKET))
            {
                BUCKET = "tagclouds-dev";
            }

            Console.WriteLine($"Using bucket {BUCKET} for storage of tag clouds");
        }




        public static string GetVideoKey(string id)
        {
            return $"{id}/video.mp4";
        }

        public static string GetTranscriptKey(string id)
        {
            return $"{id}/transcript.json";
        }

        public static string GetTagCloudAnalysisKey(string id)
        {
            return $"{id}/tag-cloud-analysis.json";
        }
    }
}
