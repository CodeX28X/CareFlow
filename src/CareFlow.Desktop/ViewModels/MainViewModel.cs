
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string currentPageTitle = "Service Availability";

    [ObservableProperty]
    private string currentPageDescription =
        "Monitor diagnostic service availability and remaining capacity.";

    [ObservableProperty]
    private string currentNavigation = "Availability";

    [ObservableProperty]
    private string connectionStatus = "Database configured";

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
