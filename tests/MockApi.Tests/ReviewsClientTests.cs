using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MockApi.Api.Clients;
using MockApi.Api.Contracts;
using Xunit;

namespace MockApi.Tests;

/// <summary>
/// ReviewsClient talks to mock-reviews-api through the Dapr sidecar's gRPC
/// channel (DaprClient), which isn't something an in-process test can fake
/// without a running sidecar. These tests instead cover MapResponseAsync,
/// the part of ReviewsClient that owns the actual response-handling logic
/// (success parsing, non-success status -> Result.Failure).
/// </summary>
public class ReviewsClientTests
{
    [Fact]
    public async Task MapResponseAsync_SuccessResponse_ReturnsSuccessResult()
    {
        var payload = new ProductReviewsDto(1, 4.5, 2, [new ReviewDto(1, 1, "Alice", 5, "Great!", DateTime.UtcNow)]);
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(payload) };

        var result = await ReviewsClient.MapResponseAsync(response, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(1);
        result.Value.Count.Should().Be(2);
    }

    [Fact]
    public async Task MapResponseAsync_ServerError_ReturnsFailure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var result = await ReviewsClient.MapResponseAsync(response, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MapResponseAsync_NotFound_ReturnsFailure()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);

        var result = await ReviewsClient.MapResponseAsync(response, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
