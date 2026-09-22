using System.Net.Http.Json;
using MockApi.Api.Common;
using MockApi.Api.Contracts;

namespace MockApi.Api.Clients;

/// <summary>
/// Typed client for the internal-ingress mock-reviews-api service. Network and
/// deserialization failures are translated to Result.Failure at this boundary
/// instead of propagating exceptions into the caller.
/// </summary>
public class ReviewsClient(HttpClient httpClient) : IReviewsClient
{
    public async Task<Result<ProductReviewsDto>> GetProductReviewsAsync(int productId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/products/{productId}/reviews", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable);
            }

            var reviews = await response.Content.ReadFromJsonAsync<ProductReviewsDto>(cancellationToken);

            return reviews is null
                ? Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable)
                : Result<ProductReviewsDto>.Success(reviews);
        }
        catch (HttpRequestException)
        {
            return Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<ProductReviewsDto>.Failure(ErrorMessages.ReviewsServiceUnavailable);
        }
    }
}
