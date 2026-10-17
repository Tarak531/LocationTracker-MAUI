using LocationTracker.Models;
using LocationTracker.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace LocationTracker;

public partial class MainPage : ContentPage
{
    private readonly LocationDatabase _database;
    private readonly LocationTrackerService _tracker;
    private List<LocationRecord> _records = new();
    private bool _loaded;

    public MainPage(LocationDatabase database, LocationTrackerService tracker)
    {
        InitializeComponent();
        _database = database;
        _tracker = tracker;
        _tracker.LocationSaved += OnLocationSaved;
        _tracker.TrackingError += OnTrackingError;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        try
        {
            _records = await _database.GetAllAsync();
            RenderMap(centerOnLast: true);
        }
        catch (Exception ex) { StatusLabel.Text = $"Database error: {ex.Message}"; }
    }

    protected override void OnDisappearing()
    {
        _tracker.Stop(); // Explicit foreground-only behavior.
        UpdateButtons();
        base.OnDisappearing();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        var ok = await _tracker.StartAsync();
        StatusLabel.Text = ok ? "Tracking in foreground" : "Unable to start tracking";
        UpdateButtons();
    }

    private void OnStopClicked(object sender, EventArgs e)
    {
        _tracker.Stop();
        StatusLabel.Text = "Tracking stopped";
        UpdateButtons();
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        if (_tracker.IsTracking)
        {
            StatusLabel.Text = "Stop tracking before clearing history";
            return;
        }
        bool confirmed = await DisplayAlertAsync(
        "Confirm",
        "Clear saved location history?",
        "Yes",
        "No");
        if (!confirmed) return;
        try
        {
            await _database.ClearAsync();
            _records.Clear();
            RenderMap(centerOnLast: false);
            StatusLabel.Text = "History cleared";
        }
        catch (Exception ex) { StatusLabel.Text = $"Could not clear: {ex.Message}"; }
    }

    private void OnHeatToggled(object sender, ToggledEventArgs e) => RenderMap(centerOnLast: false);

    private void OnLocationSaved(LocationRecord record)
    {
        _records.Add(record);
        StatusLabel.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
        RenderMap(centerOnLast: true);
    }

    private void OnTrackingError(string message) => StatusLabel.Text = message;

    private void UpdateButtons()
    {
        StartButton.IsEnabled = !_tracker.IsTracking;
        StopButton.IsEnabled = _tracker.IsTracking;
    }

    private void RenderMap(bool centerOnLast)
    {
        // Repaint modest history volumes. For large datasets use clustering/tiling.
        TrackingMap.Pins.Clear();
        TrackingMap.MapElements.Clear();
        CountLabel.Text = $"{_records.Count} saved locations";
        if (_records.Count == 0) return;

        // Dots mimic the assignment example. Cap count for map responsiveness.
        foreach (var item in _records.TakeLast(300))
        {
            TrackingMap.Pins.Add(new Pin
            {
                Label = "GPS reading",
                Address = DateTimeOffset.FromUnixTimeMilliseconds(item.TimestampUtcMs).ToLocalTime().ToString("g"),
                Type = PinType.Place,
                Location = new Location(item.Latitude, item.Longitude)
            });
        }

        if (HeatSwitch.IsToggled)
            DrawDensityOverlay(_records);

        if (centerOnLast)
        {
            var last = _records[^1];
            TrackingMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(last.Latitude, last.Longitude), Distance.FromKilometers(1.5)));
        }
    }

    /// <summary>
    /// Grid aggregation (~75 m latitude cells); larger counts yield warmer, more opaque
    /// circles. This is a lightweight density visualization rather than a raster SDK heatmap.
    /// </summary>
    private void DrawDensityOverlay(IEnumerable<LocationRecord> records)
    {
        const double cellMeters = 75;
        var groups = records.GroupBy(p =>
        {
            double latStep = cellMeters / 111_320.0;
            double lonStep = cellMeters / (111_320.0 * Math.Max(0.1, Math.Cos(p.Latitude * Math.PI / 180)));
            return (Lat: (int)Math.Floor(p.Latitude / latStep), Lon: (int)Math.Floor(p.Longitude / lonStep));
        }).OrderByDescending(g => g.Count()).Take(150).ToList();
        if (groups.Count == 0) return;
        int max = groups.Max(g => g.Count());
        foreach (var group in groups)
        {
            double intensity = (double)group.Count() / max;
            var color = intensity >= 0.7 ? Color.FromRgba(255, 53, 53, 125)
                      : intensity >= 0.35 ? Color.FromRgba(255, 156, 31, 105)
                      : Color.FromRgba(255, 223, 51, 80);
            TrackingMap.MapElements.Add(new Circle
            {
                Center = new Location(group.Average(x => x.Latitude), group.Average(x => x.Longitude)),
                Radius = Distance.FromMeters(60),
                FillColor = color,
                StrokeColor = Colors.Transparent,
                StrokeWidth = 0
            });
        }
    }
}
