using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Blazor;

using LitJson;

namespace TagCloudFrontend
{
    public class TagCloudClient
    {
        // TODO: Update baseURL to the REST endpoint for the deployed TagCloudWebAPI with a trailing slash.
        string baseUrl = "";
        private HttpClient _client;

        public TagCloudClient(HttpClient client)
        {
            this._client = client;
        }

        public async Task<string> SubmitJobAsync(TagCloudJob job)
        {
            Console.WriteLine($"Submitting job {job.VideoName}: {job.VideoUrl}");

            var request = new HttpRequestMessage();
            request.RequestUri = new Uri(baseUrl + "api/TagClouds");
            request.Method = HttpMethod.Post;
            request.Content = new StringContent(JsonUtil.Serialize(job));
            request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
            using (var response = await _client.SendAsync(request))
            {
                var id = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Job id returned: {id}");
                return id;
            }
        }

        public async Task<TagCloudSummary[]> FetchTagCloudSummary()
        {
            var dics = await _client.GetJsonAsync<Dictionary<string, object>[]>(baseUrl + "api/TagClouds");
            Console.WriteLine($"Number of tag clouds {dics.Length}");
            var tags = new TagCloudSummary[dics.Length];

            int index = 0;
            foreach (var dic in dics)
            {
                Console.WriteLine($"Processing tag cloud {index}: {dic == null}");

                DateTime compDate = DateTime.MinValue;
                if (dic.ContainsKey("completedDate") && dic["completedDate"] != null)
                    DateTime.TryParse(dic["completedDate"].ToString(), out compDate);

                tags[index] = new TagCloudSummary
                {
                    DisplayName = dic["displayName"].ToString(),
                    Id = dic["id"].ToString(),
                    CompletedDate = compDate
                };

                if(compDate != DateTime.MinValue)
                {
                    tags[index].CompletedDate = compDate;
                }

                index++;
            }

            return tags;
        }

        public async Task<TagData> FetchTagCloudData(string id)
        {
            try
            {
                var rawData = await _client.GetStringAsync(baseUrl + "api/TagClouds/" + id);

                Console.WriteLine($"Marshalling: {rawData == null}");
                var rootData = JsonMapper.ToObject(rawData);

                var data = new TagData() { Summary = new TagCloudSummary() };
                data.Summary.DisplayName = rootData["summary"]["displayName"].ToString();
                data.Summary.Id = rootData["summary"]["id"].ToString();

                DateTime compDate = DateTime.MinValue;
                if (rootData["summary"]["completedDate"] != null)
                    DateTime.TryParse(rootData["summary"]["completedDate"].ToString(), out compDate);

                var tags = new List<TagCloudCategory>();
                if (rootData.Keys.Contains("tags"))
                {
                    foreach (JsonData jsonTag in rootData["tags"])
                    {
                        tags.Add(new TagCloudCategory
                        {
                            Text = jsonTag["Text"].ToString(),
                            Category = jsonTag["Category"].ToString()
                        });
                    }
                }
                data.Tags = tags.ToArray();

                return data;
            }
            catch(System.Net.Http.HttpRequestException e)
            {
                Console.WriteLine($"Error getting status or tag cloud: {e.Message}");
                return new TagData();
            }
        }
 
        public class TagCloudJob
        {
            public string VideoName { get; set; }
            public string VideoUrl { get; set; }
        }

        public class TagData
        {
            public TagCloudSummary Summary { get; set; }
            public TagCloudClient.TagCloudCategory[] Tags { get; set; }
        }

        public class TagCloudCategory
        {
            public string Text { get; set; }
            public string Category { get; set; }
            public int Count { get; set; }
        }

        public class TagCloudSummary
        {
            public string DisplayName { get; set; }
            public string Id { get; set; }
            public DateTime? CompletedDate { get; set; }
            public string Status { get; set; }
        }

    }
}
