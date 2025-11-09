using System.Net.Http.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Options;

namespace UploaderDoc.Services
{
    public class DocumentUploadService : IDocumentUploadService
    {
        private readonly HttpClient httpClient;
        private readonly BlobStorageOptions blobStorageOptions;
        public DocumentUploadService(HttpClient httpClient, IOptions<BlobStorageOptions> blobStorageOptions
        )
        {
            this.httpClient = httpClient;
            this.blobStorageOptions = blobStorageOptions.Value;
        }
        public async Task UploadDocumentAsync(Stream fileStream)
        {
            Console.WriteLine("Uploading document...");
            // Implement your document upload logic here
            var sasInfo = await GetBlobSAS();

            var sasUri = new UriBuilder(sasInfo!.BlobUri!)
            {
                Query = sasInfo.Signature
            };

            var blob = new BlobClient(sasUri.Uri);
            await blob.UploadAsync(fileStream);
            
        }
        private async Task<StorageEntitySas?> GetBlobSAS()
        {
            Console.WriteLine("Getting SAS URL from API...");
            // Call your API to get the SAS URL
            var response = await httpClient.GetAsync(blobStorageOptions.SasApiEndpoint);
            response.EnsureSuccessStatusCode();

            var sasInfo = await response.Content.ReadFromJsonAsync<StorageEntitySas>();
            if(sasInfo == null || sasInfo.BlobUri == null || string.IsNullOrEmpty(sasInfo.Signature))
            {
                throw new Exception("Failed to get SAS info from API.");
            }
            return sasInfo;
        }

        public class StorageEntitySas
        {
            public Uri? BlobUri { get; set; }
            public string? Signature { get; set; }
        }
    }
}