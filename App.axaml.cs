using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using fhnw_compgr.labs;

namespace fhnw_compgr;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LabOne();
        }
        base.OnFrameworkInitializationCompleted();
    }
}