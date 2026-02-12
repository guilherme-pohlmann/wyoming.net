using Tizen.NUI.Components;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Wyoming.Net.Satellite.App.Tz.ViewModels;

namespace Wyoming.Net.Satellite.App.Tz.Pages;

public class SatelliteSettingsPage : ContentPage
{
	public SatelliteSettingsPage(SatelliteSettingsViewModel vm, View parent)
	{
		var nameLabel = TizenUI.CreateLabel("Name");
		var nameInput = TizenUI.CreateInput(vm, (it) => it.Name, (it, value) => it.Name = value);

		var areaLabel = TizenUI.CreateLabel("Area");
		var areaInput = TizenUI.CreateInput(vm, (it) => it.Area, (it, value) => it.Area = value);

		var descriptionLabel = TizenUI.CreateLabel("Description");
		var descriptionInput = TizenUI.CreateInput(vm, (it) => it.Description, (it, value) => it.Description = value);

		var portLabel = TizenUI.CreateLabel("Port");
		var portInput = TizenUI.CreateInput(vm, (it) => it.Port, (it, value) => it.Port = value.ToIntOrDefault());

		nameInput.UpFocusableView = parent;
		nameInput.DownFocusableView = areaInput;

		areaInput.UpFocusableView = nameInput;
		areaInput.DownFocusableView = descriptionInput;

		descriptionInput.UpFocusableView = areaInput;
		descriptionInput.DownFocusableView = portInput;

		portInput.UpFocusableView = descriptionInput;

        var view = new View
        {
            WidthResizePolicy = ResizePolicyType.FillToParent,
            HeightResizePolicy = ResizePolicyType.FillToParent,
            Padding = new Extents(200, 200, 0, 0),
            Margin = new Extents(50, 50, 50, 50),

            Layout = new LinearLayout()
            {
                LinearOrientation = LinearLayout.Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };

         view.Add(nameLabel);
		 view.Add(nameInput);
		 view.Add(areaLabel);
		 view.Add(areaInput);
		 view.Add(descriptionLabel);
		 view.Add(descriptionInput);
		 view.Add(portLabel);
		 view.Add(portInput);

		Content = view;
		Focusable = true;
		FocusGained += (s,args) => FocusManager.Instance.SetCurrentFocusView(nameInput);
	}
}