namespace MockApi.Api.Options;

public class ReviewsApiOptions
{
    public const string SectionName = "ReviewsApi";

    /// <summary>
    /// The Dapr app id of mock-reviews-api, as registered with --dapr-app-id.
    /// Requests are routed to it through this app's Dapr sidecar, not by URL/DNS.
    /// </summary>
    public required string AppId { get; set; }
}
