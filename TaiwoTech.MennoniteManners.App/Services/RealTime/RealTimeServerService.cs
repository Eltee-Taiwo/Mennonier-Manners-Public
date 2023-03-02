using Microsoft.AspNetCore.SignalR.Client;
using System.Net.NetworkInformation;
using Microsoft.AppCenter.Crashes;
using Microsoft.AspNetCore.SignalR;
using TaiwoTech.MennoniteManners.App.Constants;
using TaiwoTech.MennoniteManners.App.Domain.RealTimeServer;
using TaiwoTech.MennoniteManners.App.Extensions;
using TaiwoTech.MennoniteManners.App.Services.Dialog;

namespace TaiwoTech.MennoniteManners.App.Services.RealTime
{
    public class RealTimeServerService : ISingletonService
    {
        private const string ConnectionError = "Connection Error";
        private const string ConnectionErrorTryAgain = "There was an error connecting to the server, please try again";
        private const string Ok = "Ok";


        private string HubConnectionUri { get; }
        private DialogService DialogService { get; }
        public HubConnection HubConnection { get; }

        public RealTimeServerService(DialogService dialogService)
        {
            DialogService = dialogService;
            //todo: Omitted for public version
            HubConnectionUri = "";
            HubConnection = new HubConnectionBuilder()
                .WithUrl(HubConnectionUri)
                .Build();

        }

        /// <summary>
        /// Send a message to the realtime server and cast the response type to a specified object
        /// </summary>
        /// <typeparam name="T">The type to cast the response from the server to</typeparam>
        /// <param name="methodName">The method we want to invoke on the real time server</param>
        /// <param name="parameters">A dictionary that contains the list of parameters we want to send to the real time server</param>
        /// <returns></returns>
        public async Task<T> InvokeAsync<T>(MethodName methodName, Dictionary<string, object> parameters)
        {
            var response = await InvokeCoreAsync<T>(methodName, parameters);
            return response;
        }

        /// <summary>
        /// Send a message to the realtime server without receiving a response
        /// </summary>
        /// <param name="methodName">The method we want to invoke on the real time server</param>
        /// <param name="parameters">A dictionary that contains the list of parameters we want to send to the real time server</param>
        /// <returns></returns>
        public async Task InvokeAsync(MethodName methodName, Dictionary<string, object> parameters)
        {
            await InvokeCoreAsync<object>(methodName, parameters, false);
        }

        /// <summary>
        /// Set up a handler for when a certain message is received from the realtime server
        /// </summary>
        /// <param name="methodName">The name of the method we are expecting from the real time server</param>
        /// <param name="handler">The function that will handle a message from the real time server</param>
        /// <returns></returns>
        public void RegisterAnswerHandler(MethodName methodName, Action handler)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                throw new NetworkInformationException();
            }
            HubConnection.Remove(methodName.Value);
            HubConnection.On(methodName.Value, handler);
        }

        /// <summary>
        /// Set up a handler for when a certain message is received from the realtime server
        /// </summary>
        /// <typeparam name="T">The data type we are expecting from the real time server</typeparam>
        /// <param name="methodName">The name of the method we are expecting from the real time server</param>
        /// <param name="handler">The function that will handle a message from the real time server</param>
        /// <returns></returns>
        public void RegisterAnswerHandler<T>(MethodName methodName, Action<T> handler)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                throw new NetworkInformationException();
            }
            HubConnection.Remove(methodName.Value);
            HubConnection.On(methodName.Value, handler);
        }

        /// <summary>
        /// Set up a handler for when a certain message is received from the realtime server
        /// </summary>
        /// <typeparam name="T1">The first data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T2">The second data type we are expecting from the real time server</typeparam>
        /// <param name="methodName">The name of the method we are expecting from the real time server</param>
        /// <param name="handler">The function that will handle a message from the real time server</param>
        /// <returns></returns>
        public void RegisterAnswerHandler<T1, T2>(MethodName methodName, Action<T1, T2> handler)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                throw new NetworkInformationException();
            }
            HubConnection.Remove(methodName.Value);
            HubConnection.On(methodName.Value, handler);
        }

        /// <summary>
        /// Set up a handler for when a certain message is received from the realtime server
        /// </summary>
        /// <typeparam name="T1">The first data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T2">The second data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T3">The third data type we are expecting from the real time server</typeparam>
        /// <param name="methodName">The name of the method we are expecting from the real time server</param>
        /// <param name="handler">The function that will handle a message from the real time server</param>
        /// <returns></returns>
        public void RegisterAnswerHandler<T1, T2, T3>(MethodName methodName, Action<T1, T2, T3> handler)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                throw new NetworkInformationException();
            }
            HubConnection.Remove(methodName.Value);
            HubConnection.On(methodName.Value, handler);
        }

        /// <summary>
        /// Set up a handler for when a certain message is received from the realtime server
        /// </summary>
        /// <typeparam name="T1">The first data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T2">The second data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T3">The third data type we are expecting from the real time server</typeparam>
        /// <typeparam name="T4">The fourth data we are expecting from the real time server</typeparam>
        /// <param name="methodName">The name of the method we are expecting from the real time server</param>
        /// <param name="handler">The function that will handle a message from the real time server</param>
        /// <returns></returns>
        public void RegisterAnswerHandler<T1, T2, T3, T4>(MethodName methodName, Action<T1, T2, T3, T4> handler)
        {
            if (HubConnection.State != HubConnectionState.Connected)
            {
                throw new NetworkInformationException();
            }
            HubConnection.Remove(methodName.Value);
            HubConnection.On(methodName.Value, handler);
        }

        private async Task<T> InvokeCoreAsync<T>(MethodName methodName, Dictionary<string, object> parameters, bool expectsData = true)
        {
            try
            {
                var cancellationTokenSource = new CancellationTokenSource();
                cancellationTokenSource.CancelAfter(30_000);

                await StartHubConnectionAsync(cancellationTokenSource.Token);

                if (HubConnection.State != HubConnectionState.Connected)
                {
                    throw new NetworkInformationException();
                }

                var response = await HubConnection.InvokeAsync<T>(
                    methodName.Value,
                    parameters,
                    cancellationTokenSource.Token
                );

                if (response == null && expectsData)
                {
                    await DialogService.DisplayAlertAsync(
                        ConnectionError,
                        $"{ErrorCodes.RealTimeServerNoData}: {ConnectionErrorTryAgain}",
                        Ok
                    );
                }

                return response;
            }
            catch (NetworkInformationException ex)
            {
                parameters.Add(nameof(MethodName), methodName);
                Crashes.TrackError(ex, parameters.ToAnalyticsProperties());
                await DialogService.DisplayAlertAsync(
                    ConnectionError,
                    $"{ErrorCodes.RealTimeServerTimeOut}: {ConnectionErrorTryAgain}",
                    Ok
                );

                return default;
            }
            catch (HubException ex)
            {
                parameters.Add(nameof(MethodName), methodName);
                Crashes.TrackError(ex, parameters.ToAnalyticsProperties());
                await DialogService.DisplayAlertAsync(
                    ConnectionError,
                    $"{ErrorCodes.RealTimeServerHubError}: {ConnectionErrorTryAgain}",
                    Ok
                );

                return default;
            }
            catch (Exception ex)
            {
                parameters.Add(nameof(MethodName), methodName);
                Crashes.TrackError(ex, parameters.ToAnalyticsProperties());
                await DialogService.DisplayAlertAsync(
                    ConnectionError,
                    $"{ErrorCodes.RealTimeServerUnknown}: {ConnectionErrorTryAgain}",
                    Ok
                );

                return default;
            }
        }

        /// <summary>
        /// Start the connection to the real time server if it is not started already
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task StartHubConnectionAsync(CancellationToken cancellationToken)
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await DialogService.DisplayAlertAsync("No internet connection",
                    "Please connect to the internet to Host a game",
                    "OK"
                );

                return;
            }

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync(cancellationToken);
            }
        }
    }
}
