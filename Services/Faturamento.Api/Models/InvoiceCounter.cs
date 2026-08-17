namespace Faturamento.Api.Models
{
    // Linha única (Id = 1) usada para gerar a numeração sequencial de notas fiscais
    // via UPDATE atômico, evitando a corrida de um MAX(Number)+1.
    public class InvoiceCounter
    {
        public int Id { get; set; } = 1;
        public int NextNumber { get; set; } = 1;
    }
}
