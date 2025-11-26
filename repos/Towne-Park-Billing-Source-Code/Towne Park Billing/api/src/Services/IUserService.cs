using api.Models.Vo;

namespace api.Services
{
    public interface IUserService
    {
        UserVo GetUserRoles(string email);
    }
}
