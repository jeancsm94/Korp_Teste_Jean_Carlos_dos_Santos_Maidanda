namespace Faturamento.Api.Dtos
{
    public class InvoiceItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = [];
    }
}
