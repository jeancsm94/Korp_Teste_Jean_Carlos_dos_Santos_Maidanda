using Faturamento.Api.Clients;
using Faturamento.Api.Data;
using Faturamento.Api.Dtos;
using Faturamento.Api.Models;
using Korp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.Api.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly FaturamentoDbContext _db;
        private readonly IEstoqueClient _estoqueClient;
        private readonly ILogger<InvoiceService> _logger;

        public InvoiceService(FaturamentoDbContext db, IEstoqueClient estoqueClient, ILogger<InvoiceService> logger)
        {
            _db = db;
            _estoqueClient = estoqueClient;
            _logger = logger;
        }

        public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct)
        {
            var requestedIds = request.Items
                .Select(i => i.ProductId!.Value)
                .Distinct()
                .ToList();

            var products = await _estoqueClient.GetProductsByIdsAsync(requestedIds, ct);
            var productsById = products.ToDictionary(p => p.Id);

            var missingIds = requestedIds.Where(id => !productsById.ContainsKey(id)).ToList();
            if (missingIds.Count > 0)
            {
                throw new NotFoundException($"Produto(s) não encontrado(s): {string.Join(", ", missingIds)}.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var number = await NextInvoiceNumberAsync(ct);

            var invoice = new Invoice
            {
                Number = number,
                Status = InvoiceStatus.Aberta,
                Items = request.Items.Select(i =>
                {
                    var product = productsById[i.ProductId!.Value];
                    return new InvoiceItem
                    {
                        ProductId = product.Id,
                        ProductCode = product.Code,
                        ProductDescription = product.Description,
                        Quantity = i.Quantity!.Value
                    };
                }).ToList()
            };

            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ToDto(invoice);
        }

        public async Task<IReadOnlyList<InvoiceDto>> ListAsync(string? status, CancellationToken ct)
        {
            var query = _db.Invoices.AsNoTracking()
                .Include(i => i.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(i => i.Status == parsedStatus);
            }

            var invoices = await query.OrderBy(i => i.Number).ToListAsync(ct);
            return invoices.Select(ToDto).ToList();
        }

        public async Task<InvoiceDto> GetByIdAsync(int id, CancellationToken ct)
        {
            var invoice = await _db.Invoices.AsNoTracking()
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id, ct)
                ?? throw new NotFoundException($"Nota fiscal {id} não encontrada.");

            return ToDto(invoice);
        }

        public async Task<InvoiceDto> PrintAsync(int id, CancellationToken ct)
        {
            var invoice = await _db.Invoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == id, ct)
                ?? throw new NotFoundException($"Nota fiscal {id} não encontrada.");

            if (invoice.Status != InvoiceStatus.Aberta)
            {
                throw new InvalidInvoiceStateException(
                    $"A nota fiscal {invoice.Number} não pode ser impressa pois seu status atual é '{invoice.Status}'.");
            }

            var idempotencyKey = $"invoice-{invoice.Id}";
            var debitItems = invoice.Items.Select(i => new DebitItemRequest(i.ProductId, i.Quantity));

            // Só chega na baixa de status abaixo se o débito no Estoque for confirmado —
            // se a chamada lançar (saldo insuficiente, serviço fora do ar, timeout), a nota
            // permanece Aberta e nenhuma alteração é persistida aqui.
            await _estoqueClient.DebitBatchAsync(idempotencyKey, debitItems, ct);

            invoice.Status = InvoiceStatus.Fechada;
            invoice.ClosedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Nota fiscal {Number} impressa e fechada com sucesso.", invoice.Number);

            return ToDto(invoice);
        }

        private async Task<int> NextInvoiceNumberAsync(CancellationToken ct)
        {
            await _db.InvoiceCounters
                .Where(c => c.Id == 1)
                .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.NextNumber, c => c.NextNumber + 1), ct);

            var next = await _db.InvoiceCounters.AsNoTracking()
                .Where(c => c.Id == 1)
                .Select(c => c.NextNumber)
                .FirstAsync(ct);

            return next - 1;
        }

        private static InvoiceDto ToDto(Invoice invoice) => new()
        {
            Id = invoice.Id,
            Number = invoice.Number,
            Status = invoice.Status.ToString(),
            CreatedAt = invoice.CreatedAt,
            ClosedAt = invoice.ClosedAt,
            Items = invoice.Items.Select(item => new InvoiceItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductDescription = item.ProductDescription,
                Quantity = item.Quantity
            }).ToList()
        };
    }
}
