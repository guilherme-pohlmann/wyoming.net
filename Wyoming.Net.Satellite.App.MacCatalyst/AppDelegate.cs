using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Wyoming.Net.Satellite.App.MacCatalyst;

[Register(nameof(AppDelegate))]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
