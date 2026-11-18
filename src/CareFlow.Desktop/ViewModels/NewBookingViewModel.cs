using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CareFlow.Application.Availability;
using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using CareFlow.Application.Patients;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.Desktop.ViewModels;

public partial class BookingServiceChoice : ObservableObject
{
    public int ServiceId { get; }
    public string Name { get; }
    public int Remaining { get; }
    public bool CanBook => Remaining > 0;
    [ObservableProperty] private bool isSelected;
    public BookingServiceChoice(ServiceAvailability availability)
    {
        ServiceId = availability.ServiceId;
        Name = availability.ServiceName;
        Remaining = availability.RemainingCapacity;
    }
}

public partial class NewBookingViewModel : ViewModelBase
{
    private readonly IPatientQueryService _patients;
    private readonly IAvailabilityService _availability;
    private readonly IBookingService _bookings;
    private readonly TimeZoneInfo _manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
    private int _availabilityVersion;
    private bool _started;

    public NewBookingViewModel(
        IPatientQueryService patients,
        IAvailabilityService availability,
        IBookingService bookings)
    {
        _patients = patients;
        _availability = availability;
        _bookings = bookings;
        var local = TimeZoneInfo.ConvertTime(TimeProvider.System.GetUtcNow(), _manila);
        bookingDate = new DateTimeOffset(local.Date, local.Offset);
    }

    public ObservableCollection<PatientSummary> Patients { get; } = new();
    public ObservableCollection<BookingServiceChoice> Services { get; } = new();

    [ObservableProperty] private PatientSummary? selectedPatient;
    [ObservableProperty] private DateTimeOffset? bookingDate;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string message = string.Empty;
    [ObservableProperty] private bool isSuccess;

    partial void OnBookingDateChanged(DateTimeOffset? value) => _ = LoadServicesAsync();

    public async Task InitializeAsync()
    {
        if (_started) return;
        _started = true;
        try
        {
            var patients = await _patients.GetPatientsAsync();
            Patients.Clear();
            foreach (var patient in patients) Patients.Add(patient);
            await LoadServicesAsync();
        }
        catch (Exception)
        {
            _started = false;
            Message = "Unable to load patients or services. Check your connection and reopen New Booking.";
        }
    }

    [RelayCommand]
    private async Task LoadServicesAsync()
    {
        int version = ++_availabilityVersion;
        if (BookingDate is null) { Services.Clear(); return; }
        if (IsBusy) return;
        try
        {
            var date = DateOnly.FromDateTime(BookingDate.Value.DateTime);
            var items = await _availability.GetAvailabilityAsync(date);
            if (version != _availabilityVersion) return;
            Services.Clear();
            foreach (var item in items) Services.Add(new BookingServiceChoice(item));
        }
        catch (Exception)
        {
            if (version == _availabilityVersion)
                Message = "Unable to load services. Check your connection and refresh.";
        }
    }

    [RelayCommand]
    private async Task ConfirmAsync()
    {
        if (IsBusy) return;
        IsSuccess = false;
        Message = string.Empty;
        if (SelectedPatient is null) { Message = "Select a patient."; return; }
        if (BookingDate is null) { Message = "Select a booking date."; return; }
        var selectedIds = Services.Where(s => s.IsSelected).Select(s => s.ServiceId).ToArray();
        if (selectedIds.Length is < 1 or > 3)
        {
            Message = "Select between one and three different services.";
            return;
        }
        IsBusy = true;
        try
        {
            var request = new CreateBookingRequest(
                SelectedPatient.PatientId,
                DateOnly.FromDateTime(BookingDate.Value.DateTime), selectedIds);
            var result = await _bookings.ConfirmBookingAsync(request);
            IsSuccess = result.IsSuccess;
            Message = result.Message;
            if (result.IsSuccess)
            {
                foreach (var service in Services) service.IsSelected = false;
            }
        }
        catch (Exception)
        {
            Message = "Booking could not be confirmed. Check the database connection and booking history before trying again.";
        }
        finally { IsBusy = false; }
        // Refresh regardless of outcome: another user may have consumed capacity.
        await LoadServicesAsync();
    }
}
