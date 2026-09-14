namespace MockApi.Api.Contracts;

public record ProductDto(int Id, string Name, decimal Price, int Stock, DateTime CreatedAtUtc);

public record CreateProductRequest(string Name, decimal Price, int Stock);

public record UpdateProductRequest(string Name, decimal Price, int Stock);
