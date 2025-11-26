using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace api.Models.Vo
{
    public class DeviationVo
    {
        public Guid? ContractId { get; set; }
        public decimal? DeviationAmount { get; set; }
        public decimal? DeviationPercentage { get; set; }
        public Guid? CustomerSiteId { get; set; }
        public string? SiteName { get; set; }
        public string? SiteNumber { get; set; }
    }
}
