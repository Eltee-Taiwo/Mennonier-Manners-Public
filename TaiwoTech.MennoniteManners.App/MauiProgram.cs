using CommunityToolkit.Maui;
using System.Reflection;
using CommunityToolkit.Maui.Views;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Pages;
using TaiwoTech.MennoniteManners.App.Pages.Popups;
using TaiwoTech.MennoniteManners.App.Services;

namespace TaiwoTech.MennoniteManners.App;

public static class MauiProgram
{
    private static Assembly ExecutingAssembly => Assembly.GetExecutingAssembly();

    public static MauiApp CreateMauiApp()
    {
#if IOS || MACCATALYST
        Microsoft.Maui.Handlers.GraphicsViewHandler.Mapper.AppendToMapping("MakeTransparent", (handler, view) =>
        {
            handler.PlatformView.Opaque = false;
            handler.PlatformView.BackgroundColor = null;
        });
#endif

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("mafw.ttf", "MFW");
                fonts.AddFont("OPTIRipple-Bold.otf", "OPTI");
                fonts.AddFont("materialdesignicons-webfont.ttf", alias: "IconFont");
                fonts.AddFont("GloriaHallelujah-Regular.ttf", alias: "GLORY");
            })
            .Services
            .AddServices()
            .AddPagesAndViewModels()
            .AddPopups()
            .AddSingleton<HttpClient>();

        //todo: Omitted for public version
        //AppCenter.Start("", typeof(Analytics), typeof(Crashes));

        Analytics.EnableManualSessionTracker();
        Analytics.StartSession();
        Analytics.TrackEvent(TrackedEvents.AppLaunched);

        return builder.Build();
    }

    /// <summary>
    /// Register all popups that inherit from <see cref="Popup"/> as well as the Popup factory.
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    private static IServiceCollection AddPopups(this IServiceCollection self)
    {
        var popupTypes = ExecutingAssembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(Popup)));

        foreach (var type in popupTypes)
        {
            self.AddTransient(type);
        }

        self.AddSingleton<PopupFactory>();

        return self;
    }

    /// <summary>
    /// Register all pages (<see cref="IPage" />) and their respective view models (<see cref="BaseViewModel" />)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    private static IServiceCollection AddPagesAndViewModels(this IServiceCollection self)
    {
        var pageTypes = ExecutingAssembly
            .GetTypes()
            .Where(type => !type.IsInterface && typeof(IPage).IsAssignableFrom(type));

        var viewModelTypes = ExecutingAssembly
            .GetTypes()
            .Where(type => !type.IsInterface && typeof(BaseViewModel).IsAssignableFrom(type));

        foreach (var type in pageTypes.Concat(viewModelTypes))
        {
            self.AddTransient(type);
        }

        return self;
    }

    /// <summary>
    /// Register all my created services (<see cref="ISingletonService"/>)
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    private static IServiceCollection AddServices(this IServiceCollection self)
    {
        var singletonServiceTypes = ExecutingAssembly
            .GetTypes()
            .Where(type => !type.IsInterface && typeof(ISingletonService).IsAssignableFrom(type));

        foreach (var service in singletonServiceTypes)
        {
            self.AddSingleton(service);
        }

        return self;
    }
}
