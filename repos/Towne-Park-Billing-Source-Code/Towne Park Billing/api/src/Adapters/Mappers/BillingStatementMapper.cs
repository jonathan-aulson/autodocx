using api.Models.Dto;
using api.Models.Vo;
using api.Models.Vo.Enum;
using Riok.Mapperly.Abstractions;
using System.Globalization;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class BillingStatementMapper
    {
        private static readonly Dictionary<StatementStatus, string> StatementStatusDisplayStrings =
            new Dictionary<StatementStatus, string>
            {
                { StatementStatus.Generating, "Generating" },
                { StatementStatus.NeedsReview, "Needs Review" },
                { StatementStatus.Approved, "Approved" },
                { StatementStatus.Sent, "Sent" },
                { StatementStatus.ArReview, "AR Review" },
                { StatementStatus.ApprovalTeam, "Approval Team" },
                { StatementStatus.ReadyToSend, "Ready To Send" },
                { StatementStatus.Failed, "Failed" }
            };

        private static partial BillingStatementDto MapBillingStatementVoToDto(BillingStatementVo vo);

        public static IEnumerable<BillingStatementDto> BillingStatementVoToDto(IEnumerable<BillingStatementVo> statements)
        {
            return statements.Select(vo =>
            {
                var dto = MapBillingStatementVoToDto(vo);
                dto.ServicePeriod = $"{vo.ServicePeriodStart?.ToString("MMMM d", CultureInfo.InvariantCulture)} - {vo.ServicePeriodEnd?.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)}";
                return dto;
            });
        }

        public static partial IEnumerable<BillingStatementVo> BillingStatementModelsToVo(IEnumerable<bs_BillingStatement> model);

        private static BillingStatementVo BillingStatementModelToVo(bs_BillingStatement model)
        {
            var statement = MapBillingStatementModelToVo(model);
            statement.TotalAmount = statement.Invoices.Aggregate(0m,
                (sum, next) => next.Amount.HasValue ? sum + next.Amount.Value : sum);
            statement.CreatedMonth = MapDateTimeToYearMonth(model.CreatedOn);

            var readyForInvoice = model.bs_BillingStatement_cr9e8_readyforinvoice?.First();
            MapReadyForInvoiceModelToVo(readyForInvoice, statement);

            return statement;
        }

        public static string MapDateTimeToYearMonth(DateTime? dateTime)
        {
            if (dateTime.HasValue)
            {
                return dateTime.Value.ToString("yyyy-MM");
            }

            return null;
        }

        [MapProperty(nameof(bs_BillingStatement.bs_BillingStatementId), nameof(BillingStatementVo.Id))]
        [MapProperty(nameof(bs_BillingStatement.bs_StatementStatus), nameof(BillingStatementVo.Status))]
        [MapProperty(nameof(bs_BillingStatement.bs_ServicePeriodStart), nameof(BillingStatementVo.ServicePeriodStart))]
        [MapProperty(nameof(bs_BillingStatement.bs_ServicePeriodEnd), nameof(BillingStatementVo.ServicePeriodEnd))]
        [MapProperty($"{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite)}.{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite.bs_CustomerSiteId)}", nameof(BillingStatementVo.CustomerSiteId))]
        [MapProperty($"{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite)}.{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite.bs_SiteNumber)}", nameof(BillingStatementVo.SiteNumber))]
        [MapProperty($"{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite)}.{nameof(bs_BillingStatement.bs_BillingStatement_CustomerSite.bs_SiteName)}", nameof(BillingStatementVo.SiteName))]
        [MapProperty(nameof(bs_BillingStatement.bs_Invoice_BillingStatement), nameof(BillingStatementVo.Invoices))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_ForecastData), nameof(BillingStatementVo.ForecastData))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_PurchaseOrder), nameof(BillingStatementVo.PurchaseOrder))]
        public static partial BillingStatementVo MapBillingStatementModelToVo(bs_BillingStatement model);

        [MapProperty(nameof(bs_Invoice.bs_InvoiceId), nameof(InvoiceSummaryVo.Id))]
        [MapProperty(nameof(bs_Invoice.bs_InvoiceNumber), nameof(InvoiceSummaryVo.InvoiceNumber))]
        [MapProperty(nameof(bs_Invoice.bs_Amount), nameof(InvoiceSummaryVo.Amount))]
        private static partial InvoiceSummaryVo MapInvoiceModelToVo(bs_Invoice? model);

        private static string MapStatementStatusToString(StatementStatus status)
        {
            if (StatementStatusDisplayStrings.TryGetValue(status, out var displayString))
            {
                return displayString;
            }

            throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        private static void MapReadyForInvoiceModelToVo(cr9e8_readyforinvoice model, BillingStatementVo target)
        {
            target.AmNotes = model?.cr9e8_comments;
        }

        public static partial UpdateStatementStatusVo UpdateStatementStatusDtoToVo(UpdateStatementStatusDto dto);

        [MapProperty(nameof(UpdateStatementStatusVo.Status), nameof(bs_BillingStatement.Fields.bs_StatementStatus))]
        public static partial bs_BillingStatement UpdateStatementStatusVoToModel(UpdateStatementStatusVo vo);

        // pdf mapper
        public static partial BillingStatementPdfDto StatementPdfVoToDto(BillingStatementPdfVo vo);

        [MapProperty(nameof(bs_BillingStatement.Fields.bs_BillingStatementId), nameof(BillingStatementPdfVo.Id))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_StatementStatus), nameof(BillingStatementPdfVo.Status))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_ServicePeriodStart), nameof(BillingStatementPdfVo.ServicePeriodStart))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_ServicePeriodEnd), nameof(BillingStatementPdfVo.ServicePeriodEnd))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_ForecastData), nameof(BillingStatementPdfVo.ForecastData))]
        [MapProperty(nameof(bs_BillingStatement.Fields.bs_PurchaseOrder), nameof(BillingStatementPdfVo.PurchaseOrder))]
        private static partial BillingStatementPdfVo MapStatementPdfModelToVo(bs_BillingStatement model);

        public static BillingStatementPdfVo StatementPdfModelToVo(bs_BillingStatement model)
        {
            var vo = MapStatementPdfModelToVo(model);
            vo.CustomerSiteData = CustomerDetailMapper.CustomerDetailModelToVo(model.bs_BillingStatement_CustomerSite);

            vo.TotalAmount = model.bs_Invoice_BillingStatement
                .Select(invoice => invoice.bs_Amount)
                .Sum();

            vo.CreatedMonth = MapDateTimeToYearMonth(model.CreatedOn);

            vo.Invoices = model.bs_Invoice_BillingStatement
                .Select(invoice => InvoiceDetailMapper.InvoiceDetailModelToVo(invoice))
                .ToList();

            //vo.PurchaseOrder = model.bs_BillingStatement_CustomerSite.bs_Contract_CustomerSite?
            //    .Select(contract => contract.bs_PurchaseOrder)
            //    .FirstOrDefault();

            return vo;
        }
    }
}
