namespace LocationTracker;

public partial class App : Application
{
    private readonly MainPage _page;
    public App(MainPage page)
    {
        InitializeComponent();
        _page = page;
    }
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new NavigationPage(_page));
}
