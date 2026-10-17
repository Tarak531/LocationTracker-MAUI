using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using LocationTracker.Services;

namespace LocationTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiMaps();
#if DEBUG
        // Debug logging can be enabled after adding
        // the appropriate logging provider package.
#endif
        builder.Services.AddSingleton<LocationDatabase>();
        builder.Services.AddSingleton<LocationTrackerService>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
