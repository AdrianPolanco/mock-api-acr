using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MockApi.Api.Clients;
using MockApi.Api.Contracts;
using Xunit;

namespace MockApi.Tests;

public class ReviewsClientTests
{
    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused.");
    }

    private static ReviewsClient CreateSut(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://reviews.internal") };
        return new ReviewsClient(httpClient);
    }

    [Fact]
    public async Task GetProductReviewsAsync_SuccessResponse_ReturnsSuccessResult()
    {
        var payload = new ProductReviewsDto(1, 4.5, 2, [new ReviewDto(1, 1, "Alice", 5, "Great!", DateTime.UtcNow)]);
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(payload),
        });
        var sut = CreateSut(handler);

        var result = await sut.GetProductReviewsAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ProductId.Should().Be(1);
        result.Value.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetProductReviewsAsync_ServerError_ReturnsFailure()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(handler);

        var result = await sut.GetProductReviewsAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProductReviewsAsync_ConnectionFails_ReturnsFailure()
    {
        var sut = CreateSut(new ThrowingHttpMessageHandler());

        var result = await sut.GetProductReviewsAsync(1, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
