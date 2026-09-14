namespace MockApi.Api.Common;

public static class ApiRoutes
{
    public const string ProductsBase = "/api/products";
    public const string ProductById = "/api/products/{id:int}";
    public const string Health = "/health";
}

public static class ErrorMessages
{
    public const string ProductNotFound = "Product with the given id was not found.";
    public const string NameRequired = "Name is required.";
}
