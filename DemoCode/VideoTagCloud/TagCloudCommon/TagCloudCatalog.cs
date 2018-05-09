using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

using Newtonsoft.Json;

namespace TagCloudCommon
{
    /// <summary>
    /// This is a quick and simple registry which is a JSON file stored in S3. There is currently
    /// no protection setup for multiple writes overwriting their changes to the JSON file.
    /// A better implementation would be to use DynamoDB which would allow conditional writes.
    /// </summary>
    public class TagCloudCatalog
    {
        const string CATALOG_KEY = "tag-cloud-catalog.json";

        IAmazonS3 _s3Client;

        public TagCloudCatalog(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task RegisterTagCloudAsync(TagCloudCatalogItem item)
        {
            bool successful = false;
            do
            {
                var catalogData = await GetCurrentCatalogAsync();

                var originalItem = catalogData.Items.FirstOrDefault(x => string.Equals(x.Id, item.Id, StringComparison.InvariantCulture));
                if(originalItem != null)
                {
                    originalItem.DisplayName = item.DisplayName;
                    originalItem.StartDate = item.StartDate;
                    originalItem.CompletedDate = item.CompletedDate;
                }
                else
                {
                    catalogData.Items.Add(item);
                }
                successful = await SaveCatalogDataAsync(catalogData);
            } while (!successful);
        }

        public async Task<TagCloudCatalogItem> GetCatalogItemAsync(string id)
        {
            var data = await GetCurrentCatalogAsync();
            var item = data.Items.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.InvariantCulture));
            return item;
        }

        public async Task<IList<TagCloudCatalogItem>> GetCatalogItemsAsync()
        {
            var data = await GetCurrentCatalogAsync();
            return data.Items;
        }

        private async Task<CatalogData> GetCurrentCatalogAsync()
        {
            try
            {
                using (var response = await _s3Client.GetObjectAsync(Utilites.BUCKET, CATALOG_KEY))
                using (var reader = new StreamReader(response.ResponseStream))
                {
                    var content = await reader.ReadToEndAsync();

                    CatalogData data = new CatalogData();
                    data.Items = JsonConvert.DeserializeObject<List<TagCloudCatalogItem>>(content);
                    return data;
                }
            }
            catch(AmazonS3Exception e)
            {
                if(e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new CatalogData {Items = new List<TagCloudCatalogItem>() };
                }
                throw;
            }
        }

        private async Task<bool> SaveCatalogDataAsync(CatalogData data)
        {
            var request = new PutObjectRequest
            {
                BucketName = Utilites.BUCKET,
                Key = CATALOG_KEY,
                ContentBody = JsonConvert.SerializeObject(data.Items)
            };

            await _s3Client.PutObjectAsync(request);

            return true;
        }

        class CatalogData
        {
            public List<TagCloudCatalogItem> Items { get; set; }
        }

        public class TagCloudCatalogItem
        {
            public string DisplayName { get; set; }
            public string Id { get; set; }

            public DateTime? StartDate { get; set; }
            public DateTime? CompletedDate { get; set; }
        }
    }

    
}
