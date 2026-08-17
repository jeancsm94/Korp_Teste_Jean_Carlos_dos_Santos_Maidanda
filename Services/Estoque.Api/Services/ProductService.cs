using System.Text.Json;
using Estoque.Api.Data;
using Estoque.Api.Dtos;
using Estoque.Api.Models;
using Korp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly EstoqueDbContext _db;
        private readonly ILogger<ProductService> _logger;

        public ProductService(EstoqueDbContext db, ILogger<ProductService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken ct)
        {
            var codeInUse = await _db.Products.AnyAsync(p => p.Code == request.Code.Trim(), ct);
            if (codeInUse)
            {
                throw new ConflictException($"Já existe um produto com o código '{request.Code}'.");
            }

            var product = new Product
            {
                Code = request.Code,
                Description = request.Description,
                Balance = request.InitialBalance!.Value
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);

            return ToDto(product);
        }

        public async Task<IReadOnlyList<ProductDto>> ListAsync(string? search, CancellationToken ct)
        {
            var query = _db.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Code.Contains(search) || p.Description.Contains(search));
            }

            var products = await query.OrderBy(p => p.Code).ToListAsync(ct);
            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto> GetByIdAsync(int id, CancellationToken ct)
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new NotFoundException($"Produto {id} não encontrado.");

            return ToDto(product);
        }

        public async Task<IReadOnlyList<ProductDto>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct)
        {
            var products = await _db.Products.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .ToListAsync(ct);

            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto> UpdateAsync(int id, UpdateProductRequest request, CancellationToken ct)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new NotFoundException($"Produto {id} não encontrado.");

            var codeInUse = await _db.Products.AnyAsync(p => p.Id != id && p.Code == request.Code, ct);
            if (codeInUse)
            {
                throw new ConflictException($"Já existe um produto com o código '{request.Code}'.");
            }

            product.Code = request.Code;
            product.Description = request.Description;
            product.Balance = request.Balance!.Value;
            product.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            return ToDto(product);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
                ?? throw new NotFoundException($"Produto {id} não encontrado.");

            _db.Products.Remove(product);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<DebitBatchResponse> DebitBatchAsync(string idempotencyKey, IReadOnlyList<DebitItem> items, CancellationToken ct)
        {
            var cachedBody = await _db.ProcessedRequests
                .Where(r => r.IdempotencyKey == idempotencyKey)
                .Select(r => r.ResponseBody)
                .FirstOrDefaultAsync(ct);

            if (cachedBody is not null)
            {
                _logger.LogInformation("Idempotency-Key {Key} já processada — devolvendo resultado em cache.", idempotencyKey);
                return JsonSerializer.Deserialize<DebitBatchResponse>(cachedBody)!;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            var results = new List<DebitResultItem>();
            var insufficient = new List<InsufficientStockItem>();

            foreach (var item in items)
            {
                var product = await _db.Products.AsNoTracking()
                    .Where(p => p.Id == item.ProductId)
                    .Select(p => new { p.Id, p.Balance })
                    .FirstOrDefaultAsync(ct)
                    ?? throw new NotFoundException($"Produto {item.ProductId} não encontrado.");

                var affected = await _db.Products
                    .Where(p => p.Id == item.ProductId && p.Balance >= item.Quantity)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Balance, p => p.Balance - item.Quantity)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);

                if (affected == 0)
                {
                    insufficient.Add(new InsufficientStockItem(item.ProductId, item.Quantity, product.Balance));
                }
                else
                {
                    results.Add(new DebitResultItem(item.ProductId, product.Balance, product.Balance - item.Quantity));
                }
            }

            if (insufficient.Count > 0)
            {
                await transaction.RollbackAsync(ct);
                throw new InsufficientStockException(insufficient);
            }

            var response = new DebitBatchResponse(idempotencyKey, results);

            _db.ProcessedRequests.Add(new ProcessedRequest
            {
                IdempotencyKey = idempotencyKey,
                ResponseBody = JsonSerializer.Serialize(response)
            });

            try
            {
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Duas requisições concorrentes com a mesma chave: a outra já comitou primeiro.
                await transaction.RollbackAsync(ct);

                var raced = await _db.ProcessedRequests.AsNoTracking()
                    .Where(r => r.IdempotencyKey == idempotencyKey)
                    .Select(r => r.ResponseBody)
                    .FirstAsync(ct);

                return JsonSerializer.Deserialize<DebitBatchResponse>(raced)!;
            }

            return response;
        }

        private static ProductDto ToDto(Product product) => new()
        {
            Id = product.Id,
            Code = product.Code,
            Description = product.Description,
            Balance = product.Balance,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
