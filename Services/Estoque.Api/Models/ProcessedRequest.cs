namespace Estoque.Api.Models
{
    public class ProcessedRequest
    {
        public int Id { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string ResponseBody { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
