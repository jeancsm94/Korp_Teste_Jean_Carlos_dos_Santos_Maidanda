using Faturamento.Api.Dtos;
using Faturamento.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Faturamento.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoices;

        public InvoicesController(IInvoiceService invoices)
        {
            _invoices = invoices;
        }

        [HttpPost(Name = "CreateInvoice")]
        public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request, CancellationToken ct)
        {
            var invoice = await _invoices.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        [HttpGet(Name = "GetInvoices")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAll([FromQuery] string? status, CancellationToken ct)
        {
            var invoices = await _invoices.ListAsync(status, ct);
            return Ok(invoices);
        }

        [HttpGet("{id:int}", Name = "GetInvoiceById")]
        public async Task<ActionResult<InvoiceDto>> GetById(int id, CancellationToken ct)
        {
            var invoice = await _invoices.GetByIdAsync(id, ct);
            return Ok(invoice);
        }

        [HttpPost("{id:int}/print", Name = "PrintInvoice")]
        public async Task<ActionResult<InvoiceDto>> Print(int id, CancellationToken ct)
        {
            var invoice = await _invoices.PrintAsync(id, ct);
            return Ok(invoice);
        }
    }
}
