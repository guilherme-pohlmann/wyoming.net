namespace Wyoming.Net.Satellite.App.Maui
{
    public partial class MainApp : Application
    {
        public MainApp()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
