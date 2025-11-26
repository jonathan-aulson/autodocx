using api.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Adapters
{
    public interface IBillingStatementServiceAdapter
    {
        IEnumerable<BillingStatementDto> GetBillingStatements(Guid customerSiteId);
        IEnumerable<BillingStatementDto> GetCurrentBillingStatements(UserDto userAuth);
        void UpdateStatementStatus(Guid billingStatementId, UpdateStatementStatusDto status);
        BillingStatementPdfDto GetStatementPdfData(Guid billingStatementId);
    }
}
