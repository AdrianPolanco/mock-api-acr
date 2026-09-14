using MockApi.Api.Common;
using MockApi.Api.Contracts;

namespace MockApi.Api.Services;

public interface IProductsService
{
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<Result<ProductDto>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);

    Task<Result<ProductDto>> UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken);
}
