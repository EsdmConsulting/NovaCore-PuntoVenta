using System;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using NovaCoreESDM.Views;

namespace NovaCoreESDM;

public partial class App
    : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(
            this);
    }


    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splashWindow =
                new SplashWindow();


            desktop.MainWindow =
                splashWindow;


            splashWindow.Show();


            DispatcherTimer.RunOnce(
                () =>
                {
                    var loginWindow =
                        new LoginWindow();


                    desktop.MainWindow =
                        loginWindow;


                    loginWindow.Show();


                    splashWindow.Close();
                },
                TimeSpan.FromSeconds(
                    2));
        }


        base.OnFrameworkInitializationCompleted();
    }
}