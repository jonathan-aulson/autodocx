using System;

namespace TownePark.Billing.Api.Models.Dto
{
    public class PayrollStatisticsDto
    {
        public string JobCode { get; set; }
        public decimal Hours { get; set; }
        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
    }
}
