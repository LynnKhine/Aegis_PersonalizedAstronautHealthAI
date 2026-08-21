namespace Aegis.Infrastructure.Watsonx;

public sealed class WatsonxOptions
{
    public const string SectionName = "Watsonx";

    /// <summary>IBM Cloud IAM API key (Bearer token source).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>watsonx.ai project ID from the IBM Cloud console.</summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Base URL for the watsonx.ai REST API, e.g. https://us-south.ml.cloud.ibm.com</summary>
    public string Endpoint { get; set; } = "https://us-south.ml.cloud.ibm.com";
}
