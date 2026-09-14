using MockApi.Api.Common;
using MockApi.Api.Contracts;
using MockApi.Api.Data;
using MockApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace MockApi.Api.Services;

public class ProductsService(MockApiDbContext dbContext) : IProductsService
{
    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .OrderBy(p => p.Id)
            .Select(p => ToDto(p))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);

        return product is null
            ? Result<ProductDto>.Failure(ErrorMessages.ProductNotFound)
            : Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ProductDto>.Failure(ErrorMessages.NameRequired);
        }

        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);

        if (product is null)
        {
            return Result<ProductDto>.Failure(ErrorMessages.ProductNotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ProductDto>.Failure(ErrorMessages.NameRequired);
        }

        product.Name = request.Name;
        product.Price = request.Price;
        product.Stock = request.Stock;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ProductDto>.Success(ToDto(product));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FindAsync([id], cancellationToken);

        if (product is null)
        {
            return Result.Failure(ErrorMessages.ProductNotFound);
        }

        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Name, product.Price, product.Stock, product.CreatedAtUtc);
}
