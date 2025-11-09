namespace UploaderDoc.Services;
public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";
    
    public string SasApiEndpoint { get; set; } = string.Empty;
}