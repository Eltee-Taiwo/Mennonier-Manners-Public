using TaiwoTech.MennoniteManners.App.Pages.Game;
using TaiwoTech.MennoniteManners.App.Pages.PreGame;

namespace TaiwoTech.MennoniteManners.App.Extensions
{
    public static class NavigationExtensions
    {
        public static async Task GoBack()
        {
            await Shell.Current.GoToAsync("..", true);
        }

        public static async Task GoToHomePage()
        {
            await Shell.Current.GoToAsync("//MainPage");
        }

        public static async Task GoToPreGamePage(IDictionary<string, object> parameters)
        {
            await GoTo(nameof(PreGamePage), parameters);
        }

        public static async Task GoToGamePage(IDictionary<string, object> parameters)
        {
            await GoTo(nameof(GamePage), parameters);
        }

        private static async Task GoTo(string pageName, IDictionary<string, object> parameters = null)
        {
            await Shell.Current.GoToAsync(pageName, true, parameters);
        }
    }
}
