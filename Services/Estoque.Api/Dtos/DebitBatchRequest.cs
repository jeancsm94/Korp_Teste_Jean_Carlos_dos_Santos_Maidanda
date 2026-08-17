using System.ComponentModel.DataAnnotations;

namespace Estoque.Api.Dtos
{
    public class DebitItemRequest
    {
        [Required]
        public int? ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int? Quantity { get; set; }
    }

    public class DebitBatchRequest
    {
        [Required]
        [MinLength(1)]
        public List<DebitItemRequest> Items { get; set; } = [];
    }
}
