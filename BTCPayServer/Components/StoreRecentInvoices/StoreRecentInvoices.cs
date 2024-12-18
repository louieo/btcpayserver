using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Data;
using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Rates;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BTCPayServer.Components.StoreRecentInvoices;

public class StoreRecentInvoices : ViewComponent
{
    private readonly StoreRepository _storeRepo;
    private readonly InvoiceRepository _invoiceRepo;
    private readonly CurrencyNameTable _currencyNameTable;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContextFactory _dbContextFactory;

    public StoreRecentInvoices(
        StoreRepository storeRepo,
        InvoiceRepository invoiceRepo,
        CurrencyNameTable currencyNameTable,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContextFactory dbContextFactory)
    {
        _storeRepo = storeRepo;
        _invoiceRepo = invoiceRepo;
        _userManager = userManager;
        _currencyNameTable = currencyNameTable;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IViewComponentResult> InvokeAsync(StoreData store, string cryptoCode, bool initialRendering)
    {
        var vm = new StoreRecentInvoicesViewModel
        {
            StoreId = store.Id,
            CryptoCode = cryptoCode,
            InitialRendering = initialRendering
        };

        if (vm.InitialRendering)
            return View(vm);

        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        var invoiceEntities = await _invoiceRepo.GetInvoices(new InvoiceQuery
        {
            UserId = userId,
            StoreId = [store.Id],
            IncludeArchived = false,
            IncludeRefunds = true,
            Take = 5
        });

        vm.Invoices = (from invoice in invoiceEntities
            let state = invoice.GetInvoiceState()
            select new StoreRecentInvoiceViewModel
            {
                Date = invoice.InvoiceTime, 
                Status = state, 
                HasRefund = invoice.Refunds.Any(), 
                InvoiceId = invoice.Id,
                OrderId = invoice.Metadata.OrderId ?? string.Empty, 
                Amount = invoice.Price,
                Currency = invoice.Currency,
                Details = new InvoiceDetailsModel
                {
                    Archived = invoice.Archived,
                    Payments = invoice.GetPayments(false)
                }
           }).ToList();

        return View(vm);
    }
}
