using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Authentication.Library.Middleware;

namespace ValetKey.Web
{
    public class FileServices(ILoggerFactory loggerFactory)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<FileServices>();

        [Function(nameof(FileServices))]
        [RequireAuthentication]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "file-services/access")] HttpRequestData req,
            [BlobInput("uploads", Connection = "UploadStorage")] BlobContainerClient blobContainerClient,
            FunctionContext executionContext,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing new request for a valet key.");

            try
            {
                var userInfo = executionContext.GetRequiredAuthenticatedUser();
                
                _logger.LogInformation("Request from user: {userId}, Email: {email}", 
                    userInfo.UserId, userInfo.Email);

                var blobName = $"{userInfo.UserId}/{Guid.NewGuid()}";
                
                var sasToken = await GetSharedAccessReferenceForUploadAsync(blobContainerClient, blobName, cancellationToken);
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(sasToken);
                return response;
            }
            catch (Authentication.Library.Exceptions.AuthenticationException)
            {
                var response = req.CreateResponse(HttpStatusCode.Unauthorized);
                await response.WriteStringAsync("Authentication required");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing valet key request");
                var response = req.CreateResponse(HttpStatusCode.InternalServerError);
                await response.WriteStringAsync("Internal server error");
                return response;
            }
        }

        private async Task<StorageEntitySas> GetSharedAccessReferenceForUploadAsync(BlobContainerClient blobContainerClient, string blobName, CancellationToken cancellationToken)
        {
            var blobServiceClient = blobContainerClient.GetParentBlobServiceClient();
            var blobClient = blobContainerClient.GetBlockBlobClient(blobName);

            var userDelegationKey = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow.AddMinutes(-3),
                                                                                      DateTimeOffset.UtcNow.AddMinutes(3), cancellationToken);

            var blobSasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobContainerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-3),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(3),
                Protocol = SasProtocol.Https
            };
            blobSasBuilder.SetPermissions(BlobSasPermissions.Create);

            var sas = blobSasBuilder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName).ToString();

            _logger.LogInformation("Generated user delegated SaS token for {uri} that expires at {expiresOn}.", blobClient.Uri, blobSasBuilder.ExpiresOn);

            return new StorageEntitySas
            {
                BlobUri = blobClient.Uri,
                Signature = sas
            };
        }

        public class StorageEntitySas
        {
            public Uri? BlobUri { get; internal set; }
            public string? Signature { get; internal set; }
        }
    }
}