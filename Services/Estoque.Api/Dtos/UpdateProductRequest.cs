using System.ComponentModel.DataAnnotations;

namespace Estoque.Api.Dtos
{
    public class UpdateProductRequest
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int? Balance { get; set; }
    }
}
