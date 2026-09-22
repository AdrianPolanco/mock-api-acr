using System.Net.Http.Json;
using Dapr.Client;
using MockApi.Api.Common;
using MockApi.Api.Contracts;
using MockApi.Api.Options;
using Microsoft.Extensions.Options;

namespace MockApi.Api.Clients;

/// <summary>
/// Calls mock-reviews-api through Dapr service invocation: DaprClient.CreateInvokableHttpClient
/// gives an HttpClient whose requests are routed through this app's Dapr sidecar to the target
/// app's sidecar by Dapr app id (see ReviewsApiOptions.AppId), not by a direct URL/DNS lookup.
/// Sidecar-level failures (HttpRequestException) and non-success responses are both translated
/// to Result.Failure at this boundary.
/// </summary>
public class ReviewsClient(DaprClient daprClient, IOptionsMonitor<ReviewsApiOptions> options) : IReviewsClient
{
    public async Task<Result<ProductReviewsDto>> GetProductReviewsAsync(int productId, CancellationToken cancellationToken)
    {
        var appId = options.CurrentValue.AppId;
        using var httpClient = daprClient.CreateInvokableHttpClient(appId);

        try
        {
            var response = await httpClient.GetAsync($"/api/products/{productId}/reviews", cancellationToken);
            return await MapResponseAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable);
        }
    }

    public static async Task<Result<ProductReviewsDto>> MapResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable);
        }

        var reviews = await response.Content.ReadFromJsonAsync<ProductReviewsDto>(cancellationToken);

        return reviews is null
            ? Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable)
            : Result<ProductReviewsDto>.Success(reviews);
    }
}
