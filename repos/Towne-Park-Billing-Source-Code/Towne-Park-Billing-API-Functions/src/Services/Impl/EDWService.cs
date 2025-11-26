using TownePark.Billing.Api.Config;
using TownePark.Billing.Api.Data;

namespace TownePark.Billing.Api.Services.Impl
{
    public class EDWService : IEDWService
    {
        private readonly IEDWRepository _edwRepository;

        public EDWService(IEDWRepository edwRepository)
        {
            _edwRepository = edwRepository;
        }

        public async Task<object> GetEDWDataAsync(
            int storedProcedureId,
            Dictionary<string, object> parameters)
        {
            if (!EDWQueryRegistry.Definitions.TryGetValue(storedProcedureId, out var queryDef))
                throw new ArgumentException("Invalid stored procedure ID.");

            var rawResults = await _edwRepository.ExecuteQueryAsync(
                queryDef.NameOrSql,
                parameters,
                queryDef.CommandType);

            if (queryDef.Mapper != null)
                return queryDef.Mapper(rawResults);

            return rawResults;
        }
    }
}
