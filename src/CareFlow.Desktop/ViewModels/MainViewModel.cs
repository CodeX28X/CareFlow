using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public AvailabilityViewModel Availability { get; }
    public NewBookingViewModel NewBooking { get; }
    public BookingHistoryViewModel History { get; }

    [ObservableProperty] private ViewModelBase? currentPage;
    [ObservableProperty] private string currentPageTitle = "Service Availability";
    [ObservableProperty] private string currentPageDescription = "Review remaining capacity for a selected date.";
    [ObservableProperty] private string currentNavigation = "Availability";

    public MainViewModel(AvailabilityViewModel availability,
        NewBookingViewModel newBooking, BookingHistoryViewModel history)
    {
        Availability = availability;
        NewBooking = newBooking;
        History = history;
        CurrentPage = Availability;
    }

    public async Task InitializeAsync() => await Availability.RefreshAsync();

    [RelayCommand]
    private async Task ShowAvailabilityAsync()
    {
        CurrentPage = Availability;
        CurrentNavigation = "Availability";
        CurrentPageTitle = "Service Availability";
        CurrentPageDescription = "Review remaining capacity for a selected date.";
        await Availability.RefreshAsync();
    }

    [RelayCommand]
    private async Task ShowNewBookingAsync()
    {
        CurrentPage = NewBooking;
        CurrentNavigation = "New Booking";
        CurrentPageTitle = "New Booking";
        CurrentPageDescription = "Select a patient, date, and up to three diagnostic services.";
        await NewBooking.InitializeAsync();
    }

    [RelayCommand]
    private async Task ShowHistoryAsync()
    {
        CurrentPage = History;
        CurrentNavigation = "History";
        CurrentPageTitle = "Booking History";
        CurrentPageDescription = "Review confirmed bookings and their diagnostic services.";
        await History.RefreshAsync();
    }
}
