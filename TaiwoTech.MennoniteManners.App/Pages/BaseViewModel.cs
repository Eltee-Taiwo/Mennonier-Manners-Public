using CommunityToolkit.Mvvm.ComponentModel;

namespace TaiwoTech.MennoniteManners.App.Pages
{
    /// <summary>
    /// Used to identify a user generated view model to allow for easy DI registration
    /// </summary>
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] 
        public bool _isBusy;

    }
}
