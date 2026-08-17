namespace Estoque.Api.Dtos
{
    public record DebitResultItem(int ProductId, int PreviousBalance, int NewBalance);

    public record DebitBatchResponse(string IdempotencyKey, List<DebitResultItem> Results);
}
