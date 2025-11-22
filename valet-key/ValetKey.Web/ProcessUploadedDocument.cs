using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Authentication.Library.Middleware;
using Azure.Messaging.EventGrid;
using System.Diagnostics.Tracing;

namespace ValetKey.Web;

//function with event grid trigger to process uploaded document
public class ProcessUploadedDocument(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessUploadedDocument>();

    [Function(nameof(ProcessUploadedDocument))]
    public async Task RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        [BlobInput("uploads/{data.url}", Connection = "UploadStorage")] BlobClient blobClient,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing uploaded document event: {eventId}", eventGridEvent.Id);

        try
        {
            // Example processing logic: log blob name and size
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Uploaded blob name: {blobName}, Size: {blobSize} bytes", 
                blobClient.Name, properties.Value.ContentLength);

            // Additional processing logic can be added here
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing uploaded document for event: {eventId}", eventGridEvent.Id);
            throw;
        }
    }
}