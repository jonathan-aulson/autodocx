using Riok.Mapperly.Abstractions;
using api.Models.Vo;
using api.Models.Dto;
using System.Collections.Generic;

namespace api.Adapters.Mappers
{
    [Mapper]
    public partial class JobGroupMapper
    {
        public partial JobGroupDto Map(JobGroupVo vo);

        public partial List<JobGroupDto> Map(List<JobGroupVo> vos);

        public partial JobCodeDto Map(JobCodeVo vo);

        public partial List<JobCodeDto> Map(List<JobCodeVo> vos);
    }
}
