using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Vo
{
    public class BillingStatementPdfVo
    {
        public BillingStatementPdfVo()
        {
            Invoices = new List<InvoiceDetailVo>();
        }
        public Guid? Id { get; set; }
        public string? CreatedMonth { get; set; }
        public DateOnly? ServicePeriodStart { get; set; }
        public DateOnly? ServicePeriodEnd { get; set; }
        public decimal? TotalAmount { get; set; }
        public StatementStatus? Status { get; set; }
        public string? AmNotes { get; set; }
        public string? ForecastData { get; set; }
        public IEnumerable<InvoiceDetailVo> Invoices { get; set; }


        public CustomerDetailVo? CustomerSiteData { get; set; }
        public List<InvoiceConfigVo>? GeneralConfig { get; set; }

        public string? PurchaseOrder { get; set; }
    }

    //public class CustomerSiteVo
    //{
    //    public string? AccountManager { get; set; }
    //    public string? AccountManagerId { get; set; }
    //    public string? Address { get; set; }
    //    public string? BillingContactEmail { get; set; }
    //    public DateOnly? CloseDate { get; set; }
    //    public DateOnly? StartDate { get; set; }
    //    public string? District { get; set; }
    //    public string? GlString { get; set; }
    //    public string? InvoiceRecipient { get; set; }
    //}

    //public class GeneralConfigVo
    //{
    //    public string? Key { get; set; }
    //    public string? Value { get; set; }
    //}
}