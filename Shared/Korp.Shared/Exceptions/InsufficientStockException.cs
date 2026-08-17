namespace Korp.Shared.Exceptions
{
    public sealed record InsufficientStockItem(int ProductId, int Requested, int Available);

    public sealed class InsufficientStockException : ConflictException
    {
        public InsufficientStockException(string message) : base(message)
        {
        }

        public InsufficientStockException(IReadOnlyList<InsufficientStockItem> items)
            : base("Saldo insuficiente para um ou mais itens.")
        {
            Items = items;
        }

        public IReadOnlyList<InsufficientStockItem>? Items { get; }

        public override string Title => "Saldo insuficiente";
    }
}
