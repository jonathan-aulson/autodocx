using api.Adapters.Mappers;
using api.Functions;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl;

public class CustomerServiceAdapter : ICustomerServiceAdapter
{
    
    private readonly ICustomerService _customerService;

    public CustomerServiceAdapter(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    /**
     * Transforms CustomerSummary value object into a serializable DTO.
     */
    public IEnumerable<CustomerSummaryDto> GetCustomersSummary(UserDto userAuth, bool isForecast)
    {
        return Mappers.CustomerSummaryMapper.CustomersVoToDto(_customerService.GetCustomersSummary(userAuth, isForecast));
    }

    public CustomerDetailDto GetCustomerDetail(Guid customerId)
    {
        return Mappers.CustomerDetailMapper.CustomerDetailVoToDto(_customerService.GetCustomerDetail(customerId));
    }

    public void UpdateCustomerDetail(Guid customerId, CustomerDetailDto updateCustomer)
    {
        _customerService.UpdateCustomerDetail(customerId, CustomerDetailMapper.CustomerDetailDtoToVo(updateCustomer));
    }

    public Guid AddCustomer(string siteNumber)
    {
        return _customerService.AddCustomer(siteNumber);
    }
}
