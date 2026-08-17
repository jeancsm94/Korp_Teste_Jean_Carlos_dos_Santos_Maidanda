namespace Faturamento.Api.Clients
{
    public interface IEstoqueClient
    {
        Task<IReadOnlyList<EstoqueProductDto>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken ct);

        Task<DebitBatchResponse> DebitBatchAsync(string idempotencyKey, IEnumerable<DebitItemRequest> items, CancellationToken ct);
    }
}
