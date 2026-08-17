namespace Faturamento.Api.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Aberta;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public List<InvoiceItem> Items { get; set; } = [];
    }
}
