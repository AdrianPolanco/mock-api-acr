using Dapr.Client;
using MockApi.Api.Clients;
using MockApi.Api.Common;
using MockApi.Api.Contracts;
using MockApi.Api.Data;
using MockApi.Api.Entities;
using MockApi.Api.Options;
using MockApi.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<MockApiDbContext>(options => options.UseInMemoryDatabase("MockApiDb"));
builder.Services.AddScoped<IProductsService, ProductsService>();

builder.Services
    .AddOptions<ReviewsApiOptions>()
    .BindConfiguration(ReviewsApiOptions.SectionName)
    .ValidateOnStart();

// DaprClient talks to this app's Dapr sidecar (localhost, DAPR_HTTP_PORT/DAPR_GRPC_PORT
// env vars set by the Dapr runtime); the sidecar resolves mock-reviews-api by app id.
builder.Services.AddDaprClient();
builder.Services.AddScoped<IReviewsClient, ReviewsClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Seed a couple of products so the API returns meaningful data right after
// container start-up, without a manual step.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MockApiDbContext>();
    dbContext.Products.AddRange(
        new Product { Name = "Sample Widget", Price = 9.99m, Stock = 100, CreatedAtUtc = DateTime.UtcNow },
        new Product { Name = "Sample Gadget", Price = 19.99m, Stock = 50, CreatedAtUtc = DateTime.UtcNow });
    dbContext.SaveChanges();
}

app.MapGet(ApiRoutes.Health, () => Results.Ok(new { status = "Healthy", version = AppInfo.Version }));

app.MapGet(ApiRoutes.ProductsBase, async (IProductsService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetAllAsync(cancellationToken)))
    .WithName("GetProducts");

// Just another change
app.MapGet(ApiRoutes.ProductById, async (int id, IProductsService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetByIdAsync(id, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
})
    .WithName("GetProductById");


// Random change
app.MapPost(ApiRoutes.ProductsBase, async ([FromBody] CreateProductRequest request, IProductsService service, CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(request, cancellationToken);
    return result.IsSuccess
        ? Results.Created($"{ApiRoutes.ProductsBase}/{result.Value!.Id}", result.Value)
        : Results.BadRequest(new { error = result.Error });
})
    .WithName("CreateProduct");

app.MapPut(ApiRoutes.ProductById, async (int id, [FromBody] UpdateProductRequest request, IProductsService service, CancellationToken cancellationToken) =>
{
    var result = await service.UpdateAsync(id, request, cancellationToken);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
})
    .WithName("UpdateProduct");

app.MapDelete(ApiRoutes.ProductById, async (int id, IProductsService service, CancellationToken cancellationToken) =>
{
    var result = await service.DeleteAsync(id, cancellationToken);
    return result.IsSuccess ? Results.NoContent() : Results.NotFound(new { error = result.Error });
})
    .WithName("DeleteProduct");

// Calls the internal-ingress mock-reviews-api service to demonstrate
// service-to-service communication within the same Container Apps environment.
app.MapGet(ApiRoutes.ProductReviews, async (int id, IProductsService productsService, IReviewsClient reviewsClient, CancellationToken cancellationToken) =>
{
    var productResult = await productsService.GetByIdAsync(id, cancellationToken);
    if (!productResult.IsSuccess)
    {
        return Results.NotFound(new { error = productResult.Error });
    }

    var reviewsResult = await reviewsClient.GetProductReviewsAsync(id, cancellationToken);
    if (!reviewsResult.IsSuccess)
    {
        return Results.Json(new { error = reviewsResult.Error }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new ProductWithReviewsDto(productResult.Value!, reviewsResult.Value!));
})
    .WithName("GetProductWithReviews");

app.Run();

// Exposed so WebApplicationFactory<Program> can boot this app from integration tests later.
public partial class Program;
