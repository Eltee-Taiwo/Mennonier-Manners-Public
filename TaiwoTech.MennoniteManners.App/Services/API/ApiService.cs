using System.Text.Json;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Services.Dialog;

namespace TaiwoTech.MennoniteManners.App.Services.API
{
    public class ApiService : ISingletonService
    {
        private const string Ok = "Ok";

        private DialogService DialogService { get; }
        private HttpClient HttpClient { get; }

        public ApiService(DialogService dialogService, HttpClient httpClient)
        {
            DialogService = dialogService;
            HttpClient = httpClient;
            //todo: Omitted for public version
            //HttpClient.BaseAddress = new Uri("");
        }

        public async Task<MennoniteMannersOverview> GetSettings(CancellationToken cancellationToken = default)
        {
            const string apiQueryString = "settings";
            var apiOverview = await GetAsync<MennoniteMannersOverview>(apiQueryString, new Dictionary<string, string>(), cancellationToken);

            if (!string.IsNullOrWhiteSpace(apiOverview.MinimumAcceptableApiVersion))
            {
                return apiOverview;
            }

            await DialogService.DisplayAlertAsync("An error occurred",
                $"{ErrorCodes.ApiServerNoData}: An error occurred while getting data",
                Ok
            );
            return null;

        }

        private async Task<T> GetAsync<T>(string queryString, Dictionary<string, string> properties, CancellationToken cancellationToken = default)
        {
            try
            {
                properties.Add(nameof(queryString), queryString);
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await DialogService.DisplayAlertAsync("No internet connection",
                        "Please connect to the internet to Host a game",
                        Ok
                    );
                }

                properties.Add("Using Cache", "No");

                Analytics.TrackEvent("Getting Data from API", properties);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(queryString, UriKind.Relative));
                var response = await HttpClient.SendAsync(requestMessage, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"{response.StatusCode}: {response.ReasonPhrase}");
                }

                var result = await response.Content.ReadAsStringAsync(cancellationToken);
                var resultAsT = JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return resultAsT;
            }
            catch (Exception ex)
            {

                Crashes.TrackError(ex, properties);
                await DialogService.DisplayAlertAsync("An error occurred",
                    $"{ErrorCodes.ApiServerUnknown}: An error occurred while getting data",
                    Ok
                );
                return default;
            }
        }
    }
}
