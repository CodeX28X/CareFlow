
using CareFlow.Application.Availability;
using CareFlow.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CareFlow.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAvailabilityService _availabilityService;

    public MainViewModel(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;

        // Load today's availability when the dashboard opens.
        _ = RefreshAvailabilityAsync();
    }

    [ObservableProperty]
    private string currentPageTitle = "Service Availability";

    [ObservableProperty]
    private string currentPageDescription =
        "Monitor diagnostic service availability and remaining capacity.";

    [ObservableProperty]
    private string currentNavigation = "Availability";

    [ObservableProperty]
    private string connectionStatus = "Checking database...";

    [ObservableProperty]
    private DateTimeOffset? selectedDate = DateTimeOffset.Now;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    public ObservableCollection<ServiceAvailability> AvailabilityItems
    { get; } = new();

    public bool HasError =>
        !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool HasNoAvailability =>
        !IsLoading && !HasError && AvailabilityItems.Count == 0;

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        _ = RefreshAvailabilityAsync();
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasNoAvailability));
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(HasNoAvailability));
    }

    [RelayCommand]
    private async Task RefreshAvailabilityAsync()
    {
        if (SelectedDate is null)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            DateOnly bookingDate = DateOnly.FromDateTime(
                SelectedDate.Value.DateTime);

            var results =
                await _availabilityService.GetAvailabilityAsync(
                    bookingDate);

            AvailabilityItems.Clear();

            foreach (var item in results)
            {
                AvailabilityItems.Add(item);
            }

            ConnectionStatus = "Database connected";
        }
        catch (Exception ex)
        {
            AvailabilityItems.Clear();

            ConnectionStatus = "Database unavailable";

            ErrorMessage =
                $"Unable to load service availability: {ex.Message}";
        }
        finally
        {
            IsLoading = false;

            OnPropertyChanged(nameof(HasNoAvailability));
        }
    }

    [RelayCommand]
    private void ShowAvailability()
    {
        CurrentNavigation = "Availability";
        CurrentPageTitle = "Service Availability";
        CurrentPageDescription =
            "Monitor diagnostic service availability and remaining capacity.";
    }

    [RelayCommand]
    private void ShowNewBooking()
    {
        CurrentNavigation = "New Booking";
        CurrentPageTitle = "New Booking";
        CurrentPageDescription =
            "Create a diagnostic booking for an existing patient.";
    }

    [RelayCommand]
    private void ShowHistory()
    {
        CurrentNavigation = "History";
        CurrentPageTitle = "Booking History";
        CurrentPageDescription =
            "Review previously confirmed diagnostic bookings.";
    }
}
