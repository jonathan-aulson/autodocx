using TownePark;

namespace api.Data
{
    public interface IUserRepository
    {
        bs_User GetUserRoles(string email);
    }
}
