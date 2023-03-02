using System.Text.Json;
using CommunityToolkit.Maui.Views;
using Microsoft.AppCenter.Analytics;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Domain.RealTimeServer;
using TaiwoTech.MennoniteManners.App.Domain.User;
using TaiwoTech.MennoniteManners.App.Models;
using TaiwoTech.MennoniteManners.App.Services.DisplaySize;
using TaiwoTech.MennoniteManners.App.Services.RealTime;

namespace TaiwoTech.MennoniteManners.App.Pages.Popups;

public partial class GameOverPopUp : Popup
{
    public bool HasBeenOpened { get; set; }
    private DisplaySizeService DisplaySizeService { get; }
    private RealTimeServerService RealTimeServerService { get; }

    public GameOverPopUp(DisplaySizeService displaySizeService, RealTimeServerService realTimeServerService)
    {
        DisplaySizeService = displaySizeService;
        RealTimeServerService = realTimeServerService;
        InitializeComponent();
        BindingContext = this;
        Size = DisplaySizeService.GetSizeFromPercentages(60, 75);

        var showStatsMethodName = new MethodName("ShowStats");
        RealTimeServerService.RegisterAnswerHandler<string>(showStatsMethodName, HandleShowStats);
    }

    public void SetFailedValidation(int passMark)
    {
        WaitGrid.IsVisible = true;
        ResultsGrid.IsVisible = false;

        Outline.Stroke = new SolidColorBrush(Colors.Black);
        Prompt.Text = $"You score of {passMark} wasn't good enough";
        Details.Text = "Don't lose hope. Everyone else might fail as well!";
    }

    public void SetWinner(UserName winnerUserName, bool isMe, int passMark)
    {
        WaitGrid.IsVisible = true;
        ResultsGrid.IsVisible = false;

        var noWinner = passMark == -999;
        var colour = isMe && !noWinner ? Colors.Green : Colors.Red;


        Prompt.Text = $"You {(isMe && !noWinner ? "won" : "lost")} the game!";
        if (noWinner)
        {
            Details.Text = "Nobody won this game";
        }
        else
        {
            Details.Text = $"{(isMe ? "You" : winnerUserName)} won with a score of {passMark}!";
        }

        Details.Text += "\nAll other players' scores are being calculated...";

        Prompt.TextColor = colour;
        Outline.Stroke = new SolidColorBrush(colour);

        if (isMe)
        {
            Analytics.TrackEvent(TrackedEvents.GameWon);
        }
    }

    private void HandleShowStats(string json)
    {
        WaitGrid.IsVisible = false;
        ResultsGrid.IsVisible = true;
        Size = DisplaySizeService.GetSizeFromPercentages(60, 75);

        var results = JsonSerializer.Deserialize<List<GameResultDto>>(json)!.OrderByDescending(x => x.Score).ToList();
        ResultsCollectionView.ItemsSource = results;
    }

    private async void ReturnToLobby(object sender, EventArgs e)
    {
        await Dispatcher.DispatchAsync(() =>
        {
            Close();
        });
    }
}