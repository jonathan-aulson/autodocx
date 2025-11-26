using api.Models.Dto;
using api.Models.Vo;
using Newtonsoft.Json;
using Riok.Mapperly.Abstractions;
using TownePark;

namespace api.Adapters.Mappers
{
    [Mapper]
    public static partial class UserMapper
    {
        [MapProperty(nameof(bs_User.bs_UserId), nameof(UserVo.Id))]
        [MapProperty(nameof(bs_User.bs_SystemUserID), nameof(UserVo.SystemUserId))]
        [MapProperty(nameof(bs_User.bs_Name), nameof(UserVo.Name))]
        [MapProperty(nameof(bs_User.bs_FirstName), nameof(UserVo.FirstName))]
        [MapProperty(nameof(bs_User.bs_LastName), nameof(UserVo.LastName))]
        [MapProperty(nameof(bs_User.bs_Email), nameof(UserVo.Email))]
        [MapProperty(nameof(bs_User.bs_Roles), nameof(UserVo.Roles))]
        public static partial UserVo MapUserModelToVo(bs_User model);

        public static UserVo UserModelToVo(bs_User model)
        {
            UserVo vo = MapUserModelToVo(model);

            vo.Roles = JsonConvert.DeserializeObject<string[]>(model.bs_Roles);

            return vo;
        }
        public static partial UserDto UserVoToDto(UserVo vo);
    }
}
