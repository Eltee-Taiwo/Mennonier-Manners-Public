using CommunityToolkit.Maui.Views;
using Microsoft.AppCenter.Crashes;

namespace TaiwoTech.MennoniteManners.App.Services.Dialog;

public class DialogService : ISingletonService
{
    private Page MainPage => Application.Current!.MainPage;

    /// <summary>
    /// Display a popup to the user.
    /// </summary>
    /// <param name="popup">The popup to display</param>
    /// <returns>An object of the specified type</returns>
    public void DisplayPopUp(Popup popup)
    {

        try
        {
            MainPage!.ShowPopup(popup);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(
                ex,
                new Dictionary<string, string>
                {
                    {"Method", nameof(DisplayPopUp)},
                    {nameof(popup), popup.GetType().FullName}
                }
            );
            throw;
        }
    }

    /// <summary>
    /// Display a popup to the user and wait for it to close.
    /// </summary>
    /// <typeparam name="TResponse">The data type we are expecting from the popup</typeparam>
    /// <param name="popup">The popup to display</param>
    /// <returns>An object of the specified type</returns>
    public async Task<TResponse> DisplayPopUpAndWait<TResponse>(Popup popup)
    {
        try
        {
            return (TResponse)await MainPage!.ShowPopupAsync(popup);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(
                ex,
                new Dictionary<string, string>
                {
                    {"Method", nameof(DisplayPopUpAndWait)},
                    {nameof(popup), popup.GetType().FullName}
                }
            );
            throw;
        }
    }

    /// <summary>
    /// Display a popup to the user and wait for it to close.
    /// </summary>
    /// <param name="popup"></param>
    /// <returns></returns>
    public async Task DisplayPopUpAndWait(Popup popup)
    {
        await DisplayPopUpAndWait<object>(popup);
    }

    /// <summary>
    /// Display an alert to the user with a single button to dismiss the prompt
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="cancellationText"></param>
    /// <returns></returns>
    public async Task DisplayAlertAsync(string title, string message, string cancellationText)
    {
        try
        {
            await MainPage!.DisplayAlert(title, message, cancellationText);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(
                ex,
                new Dictionary<string, string>
                {
                    {"Method", nameof(DisplayAlertAsync)},
                    {nameof(title), title},
                    {nameof(message), message},
                    {nameof(cancellationText), cancellationText}
                }
            );
            throw;
        }
    }

    /// <summary>
    /// Display an alert to the user with a single button to dismiss the prompt
    /// </summary>
    /// <param name="title"></param>
    /// <param name="message"></param>
    /// <param name="affirmativeText"></param>
    /// <param name="negativeText"></param>
    /// <returns></returns>
    public Task<bool> DisplayAlertAsync(string title, string message, string affirmativeText, string negativeText)
    {
        try
        {
            return MainPage!.DisplayAlert(title, message, affirmativeText, negativeText);
        }
        catch (Exception ex)
        {
            Crashes.TrackError(
                ex,
                new Dictionary<string, string>
                {
                    {"Method", nameof(DisplayAlertAsync)},
                    {nameof(title), title},
                    {nameof(message), message},
                    {nameof(affirmativeText), affirmativeText},
                    {nameof(negativeText), negativeText},
                }
            );
            throw;
        }
    }
}
