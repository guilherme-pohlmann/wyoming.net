using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace Wyoming.Net.Satellite.App.iOS
{
    [Register(nameof(AppDelegate))]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
