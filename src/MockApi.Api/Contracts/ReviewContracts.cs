namespace MockApi.Api.Contracts;

public record ReviewDto(int Id, int ProductId, string Author, int Rating, string? Comment, DateTime CreatedAtUtc);

public record ProductReviewsDto(int ProductId, double AverageRating, int Count, IReadOnlyList<ReviewDto> Reviews);

public record ProductWithReviewsDto(ProductDto Product, ProductReviewsDto Reviews);
