using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Amazon.S3;
using Amazon.S3.Model;

using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

using TagCloudWebAPI.Models;
using TagCloudCommon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TagCloudWebAPI.Controllers
{
    [Route("api/[controller]")]
    public class TagCloudsController : Controller
    {
        private ILogger<TagCloudsController> _logger;
        private string _stateMachineArn;
        private IAmazonStepFunctions _stepClient;
        private IAmazonS3 _s3Client;

        public TagCloudsController(ILogger<TagCloudsController> logger, IConfiguration config, IAmazonStepFunctions stepClient, IAmazonS3 s3Client)
        {
            this._logger = logger;
            this._stateMachineArn = config["StateMachineArn"];
            this._stepClient = stepClient;
            this._s3Client = s3Client;

            this._logger.LogInformation($"State Machine Arn: {this._stateMachineArn}");
        }

        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<TagCloudCatalog.TagCloudCatalogItem>> Get()
        {
            var catalog = new TagCloudCatalog(this._s3Client);
            var items = await catalog.GetCatalogItemsAsync();
            return items;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> Get(string id)
        {
            var catalog = new TagCloudCatalog(this._s3Client);
            var summary = await catalog.GetCatalogItemAsync(id);
            if (summary == null)
                return this.NotFound();

            if (summary.CompletedDate.HasValue)
            {
                using (var s3Response = await this._s3Client.GetObjectAsync(Utilites.BUCKET, Utilites.GetTagCloudAnalysisKey(id)))
                using (var reader = new StreamReader(s3Response.ResponseStream))
                {
                    var tagData = await reader.ReadToEndAsync();

                    return new JsonResult(new { Summary = summary, Tags = JsonConvert.DeserializeObject(tagData) });
                }
            }
            else
            {
                return new JsonResult(new { Summary = summary });
            }
        }


        [HttpPost]
        public async Task<string> Post([FromBody]TagCloudJob value)
        {
            var state = new State
            {
                VideoName = value.VideoName,
                OriginalUrl = value.VideoUrl,
                Id = Guid.NewGuid().ToString()
            };

            await this._stepClient.StartExecutionAsync(new StartExecutionRequest
            {
                Name = value.VideoName.Replace(" ", "-") + Guid.NewGuid().ToString(),
                Input = JsonConvert.SerializeObject(state),
                StateMachineArn = this._stateMachineArn
            });


            return state.Id;
        }
    }
}
