using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using NUI = Tizen.NUI;
using Wyoming.Net.Satellite.App.Tz.Pages;
using Wyoming.Net.Satellite.App.Tz.ViewModels;
using Wyoming.Net.Satellite.App.Tz.Components;
using System;


namespace Wyoming.Net.Satellite.App.Tz
{
    class Program : NUIApplication
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            FocusManager.Instance.FocusIndicator = null;

            var container = new View
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent,
                Focusable = true,
                Layout = new LinearLayout
                {
                    LinearOrientation = LinearLayout.Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,

                },
                BackgroundColor = TvStyle.MainBackgroundColor
            };

            var tabView = new TvTabView();

            var satelliteSettingsVm = SatelliteSettingsViewModel.Load();

            var main = new MainPage(tabView)
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent
            };

            SatelliteSettingsPage satelliteSettingsPage = new SatelliteSettingsPage(satelliteSettingsVm, tabView)
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent
            };

            var wakeSettingsPage = new WakeSettingsPage(satelliteSettingsVm.WakeSettings, tabView)
            {
                WidthResizePolicy = ResizePolicyType.FillToParent,
                HeightResizePolicy = ResizePolicyType.FillToParent
            };

            var assistantTab = tabView.AddTab("Assistant", main);
            var satelliteSettingsTab = tabView.AddTab("Satellite Settings", satelliteSettingsPage);
            var wakeSettingsTab = tabView.AddTab("Wake Settings", wakeSettingsPage);

            assistantTab.Leave += OnTabLeave;
            satelliteSettingsTab.Leave += OnTabLeave;
            wakeSettingsTab.Leave += OnTabLeave;

            container.Add(tabView);
            Window.Instance.Add(container);

            FocusManager.Instance.SetCurrentFocusView(tabView);

            return;

            void OnTabLeave(object? sender, EventArgs args)
            {
                satelliteSettingsVm.Save();
            }
        }

        public async void OnKeyEvent(object sender, NUI.Window.KeyEventArgs e)
        {
            if (e.Key.State == Key.StateType.Down && (e.Key.KeyPressedName == "XF86Back" || e.Key.KeyPressedName == "Escape"))
            {
                Exit();
            }
        }

        static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }
    }
}