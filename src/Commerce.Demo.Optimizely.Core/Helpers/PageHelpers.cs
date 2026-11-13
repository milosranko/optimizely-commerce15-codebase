using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Commerce.Demo.Optimizely.Core.Helpers;

public static class PageHelpers
{
    public static bool IsInEditMode()
    {
        var mode = ServiceLocator.Current.GetInstance<IContextModeResolver>().CurrentMode;
        return mode is ContextMode.Edit or ContextMode.Preview;
    }
}
