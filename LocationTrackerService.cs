using LocationTracker.Models;

namespace LocationTracker.Services;

/// <summary>Foreground-only GPS polling. Tracking intentionally stops on app sleep.</summary>
public sealed class LocationTrackerService
{
    private readonly LocationDatabase _database;
    private CancellationTokenSource? _trackingCts;
    public bool IsTracking => _trackingCts is not null;
    public event Action<LocationRecord>? LocationSaved;
    public event Action<string>? TrackingError;

    public LocationTrackerService(LocationDatabase database) => _database = database;

    public async Task<bool> StartAsync()
    {
        if (IsTracking) return true;
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                TrackingError?.Invoke("Location permission was not granted.");
                return false;
            }
            if (!Geolocation.Default.IsEnabled)
            {
                TrackingError?.Invoke("Enable location services in Android settings.");
                return false;
            }
            _trackingCts = new CancellationTokenSource();
            _ = TrackLoopAsync(_trackingCts.Token);
            return true;
        }
        catch (Exception ex)
        {
            TrackingError?.Invoke($"Unable to start GPS: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        var cts = _trackingCts;
        _trackingCts = null;
        cts?.Cancel();
        // The loop disposes its CancellationTokenSource when it ends.
    }

    private async Task TrackLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var position = await Geolocation.Default.GetLocationAsync(
                        new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(12)), token);
                    if (position is null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            TrackingError?.Invoke("GPS returned no location"));
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            TrackingError?.Invoke(
                                $"GPS received: {position.Latitude:F5}, {position.Longitude:F5}"));
                    }
                    if (position is not null &&
                        double.IsFinite(position.Latitude) && double.IsFinite(position.Longitude) &&
                        position.Latitude is >= -90 and <= 90 && position.Longitude is >= -180 and <= 180)
                    {
                        var record = new LocationRecord
                        {
                            Latitude = position.Latitude,
                            Longitude = position.Longitude,
                            AccuracyMeters = position.Accuracy ?? 0,
                            TimestampUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                        await _database.AddAsync(record);
                        MainThread.BeginInvokeOnMainThread(() => LocationSaved?.Invoke(record));
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() => TrackingError?.Invoke(ex.Message));
                }
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
        catch (OperationCanceledException) { }
    }
}
