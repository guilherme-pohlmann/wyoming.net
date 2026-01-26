using Wyoming.Net.Satellite.App.Maui.ViewModels;

namespace Wyoming.Net.Satellite.App.Maui;

public partial class SatelliteSettingsPage : ContentPage
{
    public SatelliteSettingsPage(SatelliteSettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        ((SatelliteSettingsViewModel)BindingContext).Save();
        base.OnNavigatedFrom(args);
    }
}