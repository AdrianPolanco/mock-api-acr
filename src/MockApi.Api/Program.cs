using MockApi.Api.Common;
using MockApi.Api.Contracts;
using MockApi.Api.Data;
using MockApi.Api.Entities;
using MockApi.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<MockApiDbContext>(options => options.UseInMemoryDatabase("MockApiDb"));
builder.Services.AddScoped<IProductsService, ProductsService>();

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

app.MapGet(ApiRoutes.Health, () => Results.Ok(new { status = "Healthy" }));

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

app.Run();

// Exposed so WebApplicationFactory<Program> can boot this app from integration tests later.
public partial class Program;
