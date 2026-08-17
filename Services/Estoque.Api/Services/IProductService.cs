using Estoque.Api.Dtos;

namespace Estoque.Api.Services
{
    public record DebitItem(int ProductId, int Quantity);

    public interface IProductService
    {
        Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct);
        Task<IReadOnlyList<ProductDto>> ListAsync(string? search, CancellationToken ct);
        Task<ProductDto> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct);
        Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
        Task<DebitBatchResponse> DebitBatchAsync(string idempotencyKey, IReadOnlyList<DebitItem> items, CancellationToken ct);
    }
}
