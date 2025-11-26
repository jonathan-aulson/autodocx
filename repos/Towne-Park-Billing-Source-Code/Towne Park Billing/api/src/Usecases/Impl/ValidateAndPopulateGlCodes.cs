using api.Models.Vo;
using api.Models.Vo.Enum;
using api.Services;

namespace api.Usecases.Impl;

public class ValidateAndPopulateGlCodes : IValidateAndPopulateGlCodes
{
    private readonly IConfigDataService _configDataService;

    public ValidateAndPopulateGlCodes(IConfigDataService configDataService)
    {
        _configDataService = configDataService;
    }

    public void Apply(ContractDetailVo updateContractVo)
    {
        var codeTypes = new List<string>
        {
            nameof(GlCodeType.Service),
            nameof(GlCodeType.SalariedJob),
            nameof(GlCodeType.NonSalariedJob),
            nameof(GlCodeType.PerOccupiedRoom),
            nameof(GlCodeType.RevenueShare),
            nameof(GlCodeType.ManagementAgreement),
            nameof(GlCodeType.BellServiceFee)
        };
        var glCodesEnumerable = _configDataService.GetGlCodes(codeTypes).GlCodes ?? Enumerable.Empty<GlCodeVo>();
        var glCodes = glCodesEnumerable.ToList();
        // Services validation
        var serviceCodeMap = glCodes.Where(glCode => glCode.Type == GlCodeType.Service)
            .GroupBy(glCode => glCode.Code ?? "")
            .ToDictionary(group => group.Key, group => group.First());
        var invalidService = updateContractVo.FixedFee.ServiceRates
            .Find(service => service.Code == null || !serviceCodeMap.ContainsKey(service.Code));
        if (invalidService != null)
            throw new ArgumentException($"Invalid fixed fee service GL Code: {invalidService.Code}");

        // Populate Jobs
        // Always populate this code as it is never present in the arguments.
        var jobCodeMap = glCodes
            .Where(glCode => glCode.Type is GlCodeType.SalariedJob or GlCodeType.NonSalariedJob)
            .GroupBy(glCode => glCode.Name ?? "")
            .ToDictionary(group => group.Key, group => group.First().Code);
        foreach (var job in updateContractVo.PerLaborHour.JobRates)
        {
            var validJob = jobCodeMap.TryGetValue(job.Name ?? "", out var jobCode);
            if (!validJob) throw new ArgumentException($"Invalid per labor hour job: {job.Name}");
            job.Code = jobCode;
        }

        // Populate Occupied room
        // Always populate this code as it is never present in the arguments.
        var occupiedRoom = glCodes.Find(glCode => glCode.Type == GlCodeType.PerOccupiedRoom);
        if (occupiedRoom != null) updateContractVo.PerOccupiedRoom.Code = occupiedRoom.Code;
    }
}