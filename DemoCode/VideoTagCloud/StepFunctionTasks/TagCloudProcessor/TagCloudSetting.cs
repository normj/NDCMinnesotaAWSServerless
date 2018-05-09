using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StepFunctionTasks.TagCloudProcessor
{
    public class TagCloudSetting
    {
        private static Regex defaultWordFinder = new Regex(@"[\w\']+", RegexOptions.Compiled);
        private static HashSet<string> defaultStopWords = LoadStopWords();

        /// <summary>
        /// Initializes a new instance of the TagCloudSetting class. 
        /// </summary>
        public TagCloudSetting()
        {
            this.WordFinder = defaultWordFinder;
            this.StopWords = defaultStopWords;
            this.MaxCloudSize = 100;
            this.NumCategories = 10;
        }

        /// <summary>
        /// Gets or sets the regex used to find words within strings.
        /// </summary>
        public Regex WordFinder { get; set; }

        /// <summary>
        /// Gets or sets the set of word roots (e.g. "be" rather than "am") to 
        /// be ignored.
        /// </summary>
        public ISet<string> StopWords { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the tag cloud.
        /// </summary>
        public int MaxCloudSize { get; set; }

        /// <summary>
        /// Gets or sets the number of tag categories.
        /// </summary>
        public int NumCategories { get; set; }

        private static HashSet<string> LoadStopWords()
        {
            string content;
            using (var reader = new StreamReader(typeof(TagCloudSetting).Assembly.GetManifestResourceStream("StepFunctionTasks.TagCloudProcessor.en_US_stop.txt")))
            {
                content = reader.ReadToEnd();
            }

            var wordList = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var word in wordList)
            {
                set.Add(word.Trim().ToLower());
            }
            return set;
        }
    }
}
