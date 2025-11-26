using api.Functions;
using api.Models.Dto;

namespace api.Adapters;

    public interface ICustomerServiceAdapter
    {
        IEnumerable<CustomerSummaryDto> GetCustomersSummary(UserDto userAuth, bool isForecast);
        CustomerDetailDto GetCustomerDetail(Guid customerId);
        void UpdateCustomerDetail(Guid customerId, CustomerDetailDto updateCustomer);
        Guid AddCustomer(string siteNumber);
    }
