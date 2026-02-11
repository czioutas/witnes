using Api.Application.Authentication;
using Api.Product.Billing.Models;
using Api.Product.Billing.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Product.Billing;

[ApiController]
[Route("v1/[controller]")]
[Authorize(Roles = nameof(AccountRoles.AdminUserRole))]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceModel>>> GetInvoices()
    {
        var invoices = await _invoiceService.GetInvoicesForTenantAsync();
        return Ok(invoices);
    }

    [HttpGet("{invoiceId:long}")]
    public async Task<ActionResult<InvoiceModel>> GetInvoice(long invoiceId)
    {
        var invoice = await _invoiceService.GetInvoiceAsync(invoiceId);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }
}
