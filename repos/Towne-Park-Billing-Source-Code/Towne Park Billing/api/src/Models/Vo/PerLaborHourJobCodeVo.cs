using System.Text.Json.Serialization;

namespace TownePark.Models.Vo
{
    /// <summary>
    /// Represents a per-labor-hour job code configuration within a management agreement
    /// </summary>
    public class PerLaborHourJobCodeVo
    {
        /// <summary>
        /// The job code identifier (e.g., "CASH", "VAL", etc.)
        /// </summary>
        [JsonPropertyName("Code")]
        public string Code { get; set; }

        /// <summary>
        /// Description of the job code
        /// </summary>
        [JsonPropertyName("Description")]
        public string Description { get; set; }

        /// <summary>
        /// Standard hourly rate for this job code
        /// </summary>
        [JsonPropertyName("StandardRate")]
        public decimal StandardRate { get; set; }

        /// <summary>
        /// Overtime hourly rate for this job code
        /// </summary>
        [JsonPropertyName("OvertimeRate")]
        public decimal OvertimeRate { get; set; }

        /// <summary>
        /// Escalator value for standard rate
        /// </summary>
        [JsonPropertyName("StandardRateEscalatorValue")]
        public decimal StandardRateEscalatorValue { get; set; }

        /// <summary>
        /// Escalator value for overtime rate
        /// </summary>
        [JsonPropertyName("OvertimeRateEscalatorValue")]
        public decimal OvertimeRateEscalatorValue { get; set; }
    }
}