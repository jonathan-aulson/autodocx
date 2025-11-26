using System.Data;

namespace TownePark.Billing.Api.Models.Common
{
    public class EDWQueryDefinition
    {
        public int Id { get; set; }
        public string NameOrSql { get; set; } = string.Empty;
        public CommandType CommandType { get; set; }
        public Func<List<Dictionary<string, object>>, object>? Mapper { get; set; }
    }
}