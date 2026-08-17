using System.ComponentModel.DataAnnotations;

namespace Faturamento.Api.Dtos
{
    public class CreateInvoiceItemRequest
    {
        [Required]
        public int? ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? Quantity { get; set; }
    }

    public class CreateInvoiceRequest
    {
        [Required]
        [MinLength(1)]
        public List<CreateInvoiceItemRequest> Items { get; set; } = [];
    }
}
