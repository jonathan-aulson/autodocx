using api.Models.Dto;
using TownePark;

namespace api.Services
{
    public interface IRoleService
    {
        bool IsAccountManager(UserDto userDto);
        bool IsSiteFilteredUser(UserDto userDto);
        IEnumerable<Guid> GetSiteIdsForFilteredUser(string email);
        IEnumerable<Guid> GetSitesByAccountManager(string email);
        IEnumerable<Guid> GetSiteIdsByAccountManager(string email);
    }
}
