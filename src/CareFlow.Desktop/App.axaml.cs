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

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

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
                services.AddCareFlowInfrastructure(connectionString);
                services.AddSingleton<AvailabilityViewModel>();
                services.AddSingleton<NewBookingViewModel>();
                services.AddSingleton<BookingHistoryViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();
                _serviceProvider = services.BuildServiceProvider();
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                desktop.Exit += (_, _) => _serviceProvider?.Dispose();
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
