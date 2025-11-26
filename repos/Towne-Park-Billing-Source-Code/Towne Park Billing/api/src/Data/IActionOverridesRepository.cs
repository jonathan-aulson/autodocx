using TownePark;

namespace api.Data
{
    public interface IActionOverridesRepository
    {
        bs_ActionOverrides GetActionOverrideValueByName(string name);
    }
}
