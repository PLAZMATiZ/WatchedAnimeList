using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public static class NetworkHelper
{
    private static readonly Uri CheckUri = new Uri("http://clients3.google.com/generate_204");
    private static readonly HttpClient httpClient = new HttpClient();

    private static readonly object sync = new object();
    private static TaskCompletionSource<bool>? internetAvailableTcs;
    private static CancellationTokenSource cancellationTokenSource = new();
    private static CancellationToken cancellationToken { get => cancellationTokenSource.Token; }

    public static void Inithialize()
    {
        _ = MonitorInternetAsync();
    }
    /// <summary>
    /// Waiting until internet connection is restored
    /// </summary>
    public static Task WaitForInternetAsync()
    {
        lock (sync)
        {
            if (internetAvailableTcs == null || internetAvailableTcs.Task.IsCompleted)
                internetAvailableTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            cancellationToken.Register(() => internetAvailableTcs.TrySetCanceled(cancellationToken));

            return internetAvailableTcs.Task;
        }
    }

    public static async Task MonitorInternetAsync()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            bool hasInternet = await CheckInternetOnceAsync(TimeSpan.FromSeconds(3));

            if (hasInternet)
            {
                lock (sync)
                {
                    internetAvailableTcs?.TrySetResult(true);
                    internetAvailableTcs = null;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    public static void StopMonitorInternet()
    {
        cancellationTokenSource.Cancel();
    }

    private static async Task<bool> CheckInternetOnceAsync(TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            HttpResponseMessage resp = await httpClient.GetAsync(CheckUri, cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
