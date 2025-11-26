using api.Models.Dto;

namespace api.Adapters
{
    public interface IUserServiceAdapter
    {
        UserDto GetUserRoles(string email);
    }
}
