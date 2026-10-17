# Location Tracker — Android (.NET 10 MAUI)

An academic foreground-only location tracking app with GPS, local SQLite persistence, route pins and a density-circle overlay.

## Prerequisites
1. Install **Visual Studio 2022 17.12+** (or a newer compatible Visual Studio) and select **.NET Multi-platform App UI development** workload including Android SDK/emulator.
2. Verify `dotnet --version` and `dotnet workload list` in a terminal. Use .NET 10 SDK and MAUI workloads.
3. In Google Cloud Console enable **Maps SDK for Android**, configure a billing account if required, create an **Android-restricted** API key using the application ID `edu.example.locationtracker` and your signing certificate SHA-1. Insert key into `Platforms/Android/AndroidManifest.xml` replacing `REPLACE_WITH_YOUR_GOOGLE_MAPS_API_KEY`. Debug and release signing can need separate restrictions.
4. Open `LocationTracker.csproj` in Visual Studio, restore NuGet packages, choose an Android emulator with Google Play services or a physical Android phone, then Run.

## How to test
1. Confirm map tiles render after key configuration; if blank, check Android logcat for key/billing/restrictions issues.
2. Enable device location; tap **Start** and grant *while using app* precise location permission.
3. Move with device or change emulator position using **Extended controls > Location**. Wait ~5–15 seconds per fix. Check saved count increases and markers appear.
4. Turn **Density overlay** off/on. Warm circles indicate denser sampling; pins show individual fixes.
5. Tap **Stop**. Relaunch app and confirm saved positions remain (SQLite).
6. Press **Clear** and confirm count resets to zero. Canceling should retain history.
7. Deny permission and verify appropriate error without app crash.
8. Take a real screenshot from emulator/device and place it into your assignment Word document. Do not claim a screenshot exists until actually tested.

## Design and limitations
- GPS is collected every ~5 seconds **only while the page is active**; foreground tracking stops when it disappears. No background tracking permissions or service.
- Repeated stationary samples create stronger density, which is intentional; the overlay is **grid-aggregated circles**, not a continuous raster heat-map shader. Red indicates relatively high local density, orange medium, yellow low.
- Location readings are retained only on-device in `FileSystem.AppDataDirectory/locationtracker.db3`. Clear deletes local records. There is no location upload or remote server.
- Map API key is necessary to show map tiles. Do not commit a production unrestricted key to public GitHub. Store credentials appropriately for real projects.
- Rendering is capped to 300 most recent pins and 150 density circles for UI performance, but SQLite retains all samples until cleared.
- Actual successful build and on-device runtime verification must happen on a machine with the .NET 10 MAUI Android toolchain. This ZIP has not been compiled in the creation environment.

## GitHub and submission
Create your own repository, upload these files, remove/replace any real secrets before committing, and copy the **real** repository URL into your Word submission. Include actual screen capture of the app and a short test-results table.

## Official references
- https://learn.microsoft.com/dotnet/maui/get-started/installation?view=net-maui-10.0
- https://learn.microsoft.com/dotnet/maui/user-interface/controls/map?view=net-maui-10.0
- https://learn.microsoft.com/dotnet/maui/platform-integration/device/geolocation?view=net-maui-10.0
- https://learn.microsoft.com/dotnet/maui/data-cloud/database-sqlite?view=net-maui-10.0
