using api.Adapters.Mappers;
using api.Models.Dto;
using api.Services;

namespace api.Adapters.Impl
{
    public class UserServiceAdapter : IUserServiceAdapter
    {
        private readonly IUserService _userService;

        public UserServiceAdapter(IUserService userService)
        {
            _userService = userService;
        }

        public UserDto GetUserRoles(string email)
        {
            var user = _userService.GetUserRoles(email);
            return UserMapper.UserVoToDto(user);
        }
    }
}
