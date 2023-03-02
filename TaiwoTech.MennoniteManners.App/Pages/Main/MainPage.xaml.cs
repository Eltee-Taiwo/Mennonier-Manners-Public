namespace TaiwoTech.MennoniteManners.App.Pages.Main;

public partial class MainPage : ContentPage, IPage
{
    public MainPage(
        MainPageViewModel viewModel
    )
    {
        InitializeComponent();
        DeviceDisplay.Current.KeepScreenOn = true;
        BindingContext = viewModel;
    }
}

