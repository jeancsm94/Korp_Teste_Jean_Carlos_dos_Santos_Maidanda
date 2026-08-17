using System.ComponentModel.DataAnnotations;
using Estoque.Api.Dtos;
using Estoque.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Estoque.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _products;

        public ProductsController(IProductService products)
        {
            _products = products;
        }

        [HttpPost(Name = "CreateProduct")]
        public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken ct)
        {
            var product = await _products.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        [HttpGet(Name = "GetProducts")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
        {
            var products = await _products.ListAsync(search, ct);
            return Ok(products);
        }

        [HttpGet("batch", Name = "GetProductsBatch")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetBatch([FromQuery] string ids, CancellationToken ct)
        {
            var parsedIds = (ids ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(int.Parse)
                .ToList();

            var products = await _products.GetByIdsAsync(parsedIds, ct);
            return Ok(products);
        }

        [HttpGet("{id:int}", Name = "GetProductById")]
        public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
        {
            var product = await _products.GetByIdAsync(id, ct);
            return Ok(product);
        }

        [HttpPut("{id:int}", Name = "UpdateProduct")]
        public async Task<ActionResult<ProductDto>> Update(int id, UpdateProductRequest request, CancellationToken ct)
        {
            var product = await _products.UpdateAsync(id, request, ct);
            return Ok(product);
        }

        [HttpDelete("{id:int}", Name = "DeleteProduct")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            await _products.DeleteAsync(id, ct);
            return NoContent();
        }

        [HttpPost("debit-batch", Name = "DebitProductsBatch")]
        public async Task<ActionResult<DebitBatchResponse>> DebitBatch(
            [FromHeader(Name = "Idempotency-Key")] [Required] string idempotencyKey,
            DebitBatchRequest request,
            CancellationToken ct)
        {
            var items = request.Items
                .Select(i => new DebitItem(i.ProductId!.Value, i.Quantity!.Value))
                .ToList();

            var response = await _products.DebitBatchAsync(idempotencyKey, items, ct);
            return Ok(response);
        }
    }
}
