using MockApi.Api.Common;
using MockApi.Api.Contracts;

namespace MockApi.Api.Clients;

public interface IReviewsClient
{
    Task<Result<ProductReviewsDto>> GetProductReviewsAsync(int productId, CancellationToken cancellationToken);
}
