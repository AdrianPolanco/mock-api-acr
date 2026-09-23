namespace MockApi.Api.Common;

public static class ApiRoutes
{
    public const string ProductsBase = "/api/products";
    public const string ProductById = "/api/products/{id:int}";
    public const string ProductReviews = "/api/products/{id:int}/reviews";
    public const string Health = "/health";
}

public static class ErrorMessages
{
    public const string ProductNotFound = "Product with the given id was not found.";
    public const string NameRequired = "Name is required.";
    public const string ReviewsServiceUnavailable = "The reviews service is currently unavailable.";
}

public static class AppInfo
{
    public const string Version = "v2";
}
