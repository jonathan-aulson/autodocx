using api.Adapters.Mappers;
using api.Data;
using api.Models.Dto;
using api.Models.Vo;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using TownePark;

namespace api.Services.Impl
{
    public class BillingStatementService : IBillingStatementService
    {
        private readonly IBillingStatementRepository _billingStatementRepository;
        private readonly IConfigDataRepository _configDataRepository;
        private readonly IRoleService _roleService;

        private const string InvoiceEntityAlias = "invoice";

        public BillingStatementService(IBillingStatementRepository billingStatementRepository, IConfigDataRepository configDataRepository, IRoleService roleService) 
        {
            _billingStatementRepository = billingStatementRepository;
            _configDataRepository = configDataRepository;
            _roleService = roleService;
        }

        public IEnumerable<BillingStatementVo> GetCurrentBillingStatements(UserDto userAuth)
        {
            if (_roleService.IsSiteFilteredUser(userAuth))
            {
                var customerSiteIds = _roleService.GetSiteIdsForFilteredUser(userAuth.Email);
                if (!customerSiteIds.Any())
                {
                    return Enumerable.Empty<BillingStatementVo>();
                }
                var billingStatementIds = _billingStatementRepository.GetBillingStatementIdsByCustomerSite(customerSiteIds);
                var statements = _billingStatementRepository.GetBillingStatementsByIds(billingStatementIds);
                return BillingStatementMapper.BillingStatementModelsToVo(statements);
            }

            var billingStatements = _billingStatementRepository.GetCurrentBillingStatements();
            return BillingStatementMapper.BillingStatementModelsToVo(billingStatements);
        }

        public IEnumerable<BillingStatementVo> GetBillingStatements(Guid customerSiteId)
        {
            var billingStatements = _billingStatementRepository.GetBillingStatementsByCustomerSite(customerSiteId);
            return BillingStatementMapper.BillingStatementModelsToVo(billingStatements);
        }

        public void UpdateStatementStatus(Guid billingStatementId, UpdateStatementStatusVo status)
        {
            _billingStatementRepository.UpdateStatementStatus(billingStatementId, BillingStatementMapper.UpdateStatementStatusVoToModel(status));
        }

        public BillingStatementPdfVo GetStatementPdfData(Guid billingStatementId)
        {
            // Define the parsing logic as a lambda function or a separate method
            Action<bs_BillingStatement, IGrouping<Guid, Entity>> parseAttributeValues = (statement, group) =>
            {
                // Implement your attribute parsing logic here
                statement.bs_Invoice_BillingStatement = group.Select(entity => {
                    return new bs_Invoice
                    {
                        bs_InvoiceId = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceId)?.Value as Guid? ?? Guid.Empty,
                        bs_InvoiceNumber = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceNumber)?.Value as string,
                        bs_Amount = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Amount)?.Value as decimal? ?? 0,
                        bs_InvoiceDate = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceDate)?.Value as DateTime? ?? DateTime.MinValue,
                        bs_PaymentTerms = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_PaymentTerms)?.Value as string,
                        bs_Title = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Title)?.Value as string,
                        bs_Description = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_Description)?.Value as string,
                        bs_InvoiceData = entity.GetAttributeValue<AliasedValue>(InvoiceEntityAlias + "." + bs_Invoice.Fields.bs_InvoiceData)?.Value as string
                    };
                }).ToArray();
            };

            // Pass the lambda function when calling the repository method
            var statementPdfData = _billingStatementRepository.GetBillingStatementById(billingStatementId);

            var statementPdfVo = BillingStatementMapper.StatementPdfModelToVo(statementPdfData);

            var generalConfigData = _configDataRepository.GetInvoiceConfigData(bs_generalconfiggroupchoices.InvoiceHeaderFooter);
            var generalConfigVo = ConfigDataMapper.InvoiceConfigModelToVo(generalConfigData).ToList();

            statementPdfVo.GeneralConfig = generalConfigVo;

            return statementPdfVo;
        }
    }
}
