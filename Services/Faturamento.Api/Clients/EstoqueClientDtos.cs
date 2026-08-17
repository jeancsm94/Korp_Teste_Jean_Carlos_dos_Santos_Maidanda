namespace Faturamento.Api.Clients
{
    public record EstoqueProductDto(int Id, string Code, string Description, int Balance);

    public record DebitItemRequest(int ProductId, int Quantity);

    public record DebitBatchRequestBody(List<DebitItemRequest> Items);

    public record DebitResultItem(int ProductId, int PreviousBalance, int NewBalance);

    public record DebitBatchResponse(string IdempotencyKey, List<DebitResultItem> Results);
}
