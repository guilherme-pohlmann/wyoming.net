using Wyoming.Net.Satellite.App.Maui.ViewModels;

namespace Wyoming.Net.Satellite.App.Maui;

public partial class WakeSettingsPage : ContentPage
{
    private readonly SatelliteSettingsViewModel viewModel;

    public WakeSettingsPage(SatelliteSettingsViewModel vm)
	{
		InitializeComponent();
        viewModel = vm;
		BindingContext = vm.WakeSettings;
	}

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        viewModel.Save();
        base.OnNavigatedFrom(args);
    }
}