using System;

namespace TagCloudCommon
{
    /// <summary>
    /// The state passed between the step function executions.
    /// </summary>
    public class State
    {
        /// <summary>
        /// Input value when starting the execution
        /// </summary>
        public string VideoName { get; set; }

        /// <summary>
        /// Unique ID Identifing the tag cloud
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The message built through the step function execution.
        /// </summary>
        public string OriginalUrl { get; set; }



        public string S3Url { get; set; }

        public string TranscriptionJobName { get; set; }

        public string TranscriptionJobStatus { get; set; }
    }
}
