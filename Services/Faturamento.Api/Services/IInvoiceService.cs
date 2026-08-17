using Faturamento.Api.Dtos;

namespace Faturamento.Api.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct);
        Task<IReadOnlyList<InvoiceDto>> ListAsync(string? status, CancellationToken ct);
        Task<InvoiceDto> GetByIdAsync(int id, CancellationToken ct);
        Task<InvoiceDto> PrintAsync(int id, CancellationToken ct);
    }
}
