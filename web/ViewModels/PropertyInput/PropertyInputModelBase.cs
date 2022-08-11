using BricksAndHearts.Database;

namespace BricksAndHearts.ViewModels.PropertyInput;

public abstract class PropertyInputModelBase
{
    public abstract void PropertyInputModelStepInitialiser(PropertyDbModel property);
}