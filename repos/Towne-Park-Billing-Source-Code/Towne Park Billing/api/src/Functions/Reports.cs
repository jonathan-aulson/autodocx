using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace api.Functions
{
    public class Reports
    {
        private readonly ILogger _logger;

        public Reports(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Reports>();
        }

        [Function("GetSupportingReportByName")]
        public async Task<HttpResponseData> GetSupportingReportByName(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reports/{period}/{siteNumber}/{reportName}")]
            HttpRequestData req,
            string period,
            string siteNumber,
            string reportName)
        {
            if (string.IsNullOrEmpty(period) || string.IsNullOrEmpty(siteNumber))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_BLOB_STORAGE_CONNECTION_STRING"));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("reports");

            string directoryPath = $"{period}/{siteNumber}/{reportName}.pdf";
            var blobItems = containerClient.GetBlobsAsync(prefix: directoryPath);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");

            var pdfStreams = new List<MemoryStream>();

            await foreach (var blobItem in blobItems)
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                var blobDownloadInfo = await blobClient.DownloadAsync();

                using var memoryStream = new MemoryStream();
                await blobDownloadInfo.Value.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                pdfStreams.Add(memoryStream);

                await memoryStream.CopyToAsync(response.Body);
            }

            return response;
        }

        [Function("ListSupportingReportNames")]
        public async Task<HttpResponseData> ListSupportingReportNames(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "reports/{period}/{siteNumber}")]
            HttpRequestData req,
            string period,
            string siteNumber)
        {
            if (string.IsNullOrEmpty(period) || string.IsNullOrEmpty(siteNumber))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("AZURE_BLOB_STORAGE_CONNECTION_STRING"));
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("reports");

            string directoryPath = $"{period}/{siteNumber}";
            var blobItems = containerClient.GetBlobsAsync(prefix: directoryPath);

            List<string> reportNames = new List<string>();

            await foreach (var blobItem in blobItems)
            {
                var reportName = blobItem.Name.Split('/').Last().Split('.').First();
                if (!reportNames.Contains(reportName))
                {
                    reportNames.Add(reportName);
                }
            }

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");

            response.WriteString(JsonConvert.SerializeObject(reportNames));

            return response;
        }
    }
}
