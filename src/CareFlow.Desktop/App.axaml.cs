
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.Desktop.ViewModels;
using CareFlow.Desktop.Views;
using CareFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CareFlow.Desktop;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            string? connectionString =
                Environment.GetEnvironmentVariable("CAREFLOW_CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                desktop.MainWindow = new ConfigurationErrorWindow();
            }
            else
            {
                var services = new ServiceCollection();

                // Register existing backend services.
                services.AddCareFlowInfrastructure(connectionString);

                // Register desktop ViewModels.
                services.AddSingleton<MainViewModel>();

                // Register desktop windows.
                services.AddSingleton<MainWindow>();

                // Build the dependency injection container.
                _serviceProvider = services.BuildServiceProvider();

                // Resolve MainWindow and its dependencies.
                desktop.MainWindow =
                    _serviceProvider.GetRequiredService<MainWindow>();

                // Dispose registered services when the application exits.
                desktop.Exit += (_, _) => _serviceProvider?.Dispose();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
