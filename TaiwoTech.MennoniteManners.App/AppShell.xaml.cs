using System.Reflection;
using TaiwoTech.MennoniteManners.App.Pages;

namespace TaiwoTech.MennoniteManners.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        var pageTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => !type.IsInterface && typeof(IPage).IsAssignableFrom(type));

        foreach (var pageType in pageTypes)
        {
            Routing.RegisterRoute(pageType.Name, pageType);
        }
    }
}
