namespace UploaderDoc.Services;

public class ExternalEntraOptions
{
    public const string SectionName = "AzureAd";
    
    public string ClientId { get; set; } = string.Empty;
    public string Authority { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string TenantName { get; set; } = string.Empty;
    public bool ValidateAuthority { get; set; } = false;
}