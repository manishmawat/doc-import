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
using System.Text.Json;

namespace ValetKey.Web;

//function with event grid trigger to process uploaded document
public class ProcessUploadedDocument(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ProcessUploadedDocument>();

    [Function(nameof(ProcessUploadedDocument))]
    public async Task RunAsync(
        [EventGridTrigger] EventGridEvent eventGridEvent,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing uploaded document event: {eventId}", eventGridEvent.Id);

        try
        {
            // Extract blob information from the Event Grid event
            var eventData = JsonSerializer.Deserialize<JsonElement>(eventGridEvent.Data);
            var blobUrl = eventData.GetProperty("url").GetString();
            
            if (string.IsNullOrEmpty(blobUrl))
            {
                _logger.LogWarning("No blob URL found in event data");
                return;
            }

            // Create blob client from the URL
            var blobClient = new BlobClient(new Uri(blobUrl));
            
            // Get blob properties
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