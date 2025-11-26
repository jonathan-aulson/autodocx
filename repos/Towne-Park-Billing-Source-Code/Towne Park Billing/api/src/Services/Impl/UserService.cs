using api.Adapters.Mappers;
using api.Data;
using api.Models.Vo;

namespace api.Services.Impl
{
    class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public UserVo GetUserRoles(string email)
        {
            var user = _userRepository.GetUserRoles(email);
            return UserMapper.UserModelToVo(user);
        }
    }
}
