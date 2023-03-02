using CommunityToolkit.Maui.Views;

namespace TaiwoTech.MennoniteManners.App.Pages.Popups
{
    /// <summary>
    /// Used to create new instances of popups. This exists rather than injecting the popups directly because while the pages are singletons,
    /// we need new instances of the popups everytime we want to display them.
    /// </summary>
    public class PopupFactory
    {
        private IServiceProvider ServiceProvider { get; }

        public PopupFactory(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Create a new instance of a given popup
        /// </summary>
        /// <typeparam name="T">The type of popup we want to instantiate</typeparam>
        /// <returns></returns>
        public T CreatePopUp<T>() where T : Popup
        {
            return ServiceProvider.GetRequiredService<T>();
        }
    }
}
