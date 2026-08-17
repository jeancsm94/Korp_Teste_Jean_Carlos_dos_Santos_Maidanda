using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Korp.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.Api.Clients
{
    public sealed class EstoqueClient : IEstoqueClient
    {
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;

        public EstoqueClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<IReadOnlyList<EstoqueProductDto>> GetProductsByIdsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var csv = string.Join(",", ids);
            var products = await _http.GetFromJsonAsync<List<EstoqueProductDto>>($"/products/batch?ids={csv}", CaseInsensitiveOptions, ct);
            return products ?? [];
        }

        public async Task<DebitBatchResponse> DebitBatchAsync(string idempotencyKey, IEnumerable<DebitItemRequest> items, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/products/debit-batch")
            {
                Content = JsonContent.Create(new DebitBatchRequestBody(items.ToList()))
            };
            request.Headers.Add("Idempotency-Key", idempotencyKey);

            using var response = await _http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CaseInsensitiveOptions, ct);
                var items2 = ExtractInsufficientStockItems(problem);
                throw items2 is { Count: > 0 }
                    ? new InsufficientStockException(items2)
                    : new InsufficientStockException(problem?.Detail ?? "Saldo insuficiente.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(CaseInsensitiveOptions, ct);
                throw new NotFoundException(problem?.Detail ?? "Produto não encontrado.");
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DebitBatchResponse>(CaseInsensitiveOptions, ct);
            return result ?? throw new InvalidOperationException("Resposta vazia do serviço de estoque.");
        }

        private static List<InsufficientStockItem>? ExtractInsufficientStockItems(ProblemDetails? problem)
        {
            if (problem is null || !problem.Extensions.TryGetValue("items", out var raw) || raw is not JsonElement element)
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<InsufficientStockItem>>(element.GetRawText(), CaseInsensitiveOptions);
        }
    }
}
