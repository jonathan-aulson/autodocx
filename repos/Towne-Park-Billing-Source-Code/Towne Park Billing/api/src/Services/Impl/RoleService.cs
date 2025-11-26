using api.Data;
using api.Models.Dto;
using TownePark;

namespace api.Services.Impl
{
    public class RoleService : IRoleService
    {
        private readonly ICustomerRepository _customerRepository;

        public RoleService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public bool IsAccountManager(UserDto userDto)
        {
            return userDto.Roles?.Any(role => NormalizeRoleName(role) == "accountmanager") ?? false;
        }

        public bool IsSiteFilteredUser(UserDto userDto)
        {
            var filterRoles = new[] { "accountmanager", "districtmanager" };
            return userDto.Roles?.Any(role => filterRoles.Contains(NormalizeRoleName(role))) ?? false;
        }

        public IEnumerable<Guid> GetSiteIdsForFilteredUser(string email)
        {
            return _customerRepository.GetSiteIdsByUser(email);
        }

        public IEnumerable<Guid> GetSitesByAccountManager(string email)
        {
            return _customerRepository.GetSiteIdsByUser(email);
        }

        public IEnumerable<Guid> GetSiteIdsByAccountManager(string email)
        {
            return _customerRepository.GetSiteIdsByUser(email);
        }

        private string NormalizeRoleName(string roleName)
        {
            return roleName?.Trim()
                .Replace("[", "")
                .Replace("]", "")
                .Replace("\"", "")
                .ToLowerInvariant();
        }
    }
}
