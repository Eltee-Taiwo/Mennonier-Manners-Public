using TaiwoTech.MennoniteManners.App.Extensions;
using TaiwoTech.MennoniteManners.App.Services.Dialog;

namespace TaiwoTech.MennoniteManners.App.Pages.Game;

public partial class GamePage : ContentPage, IPage, IQueryAttributable
{
    private DialogService DialogService { get; }

    public GamePage(DialogService dialogService,GamePageViewModel viewModel)
	{
        DialogService = dialogService;
        InitializeComponent();
        BindingContext = viewModel;

        HintsGrid.InputTransparent = DeviceInfo.Current.Platform != DevicePlatform.Android;
        RotateText();
    }

    protected override void OnDisappearing()
    {
        var vm = (GamePageViewModel)BindingContext;
        vm.PostGameCleanUp();
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        DialogService.DisplayAlertAsync("Leave Game?", "Are you sure you want to leave the game?", "Yes", "No")
            .ContinueWith(async answer =>
                {
                    if (answer.Result)
                    {
                        await (BindingContext as GamePageViewModel)!.LeaveGame();
                    }
                });
        
        return true;
    }

    private void RotateText()
    {
        var labelsToRotate = new List<Label> { NextLabel, PencilLabel, FinishLabel };

        foreach (var label in labelsToRotate)
        {
            var text = label.Text;
            var length = text.Length;
            var newText = "";

            for (var i = 0; i < length; i++)
            {
                newText += text[i] + "\n";
            }

            label.Text = newText;
        }
    }


    public void ApplyQueryAttributes(IDictionary<string, object> queryAttributes)
    {
        ((GamePageViewModel)BindingContext).InitialisePropertiesAsync(queryAttributes, GetImageFromDrawingView, ClearDrawingView);
    }

    private async Task<byte[]> GetImageFromDrawingView()
    {
        var imageStream = await DrawView.GetImageStream(DrawView.Width, DrawView.Height);
        ClearDrawingView();
        return imageStream.ToByteArray();
    }

    private void ClearDrawingView() => DrawView.Clear();
}