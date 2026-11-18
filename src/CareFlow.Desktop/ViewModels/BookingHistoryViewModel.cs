using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CareFlow.Application.Bookings;
using CareFlow.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.Desktop.ViewModels;

public partial class BookingHistoryViewModel : ViewModelBase
{
    private readonly IBookingQueryService _queries;
    public BookingHistoryViewModel(IBookingQueryService queries) => _queries = queries;

    public ObservableCollection<BookingDetails> Bookings { get; } = new();
    [ObservableProperty] private BookingDetails? selectedBooking;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsEmpty => !IsLoading && !HasError && Bookings.Count == 0;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var results = await _queries.GetBookingsAsync();
            Bookings.Clear();
            foreach (var result in results) Bookings.Add(result);
            SelectedBooking = null;
        }
        catch (Exception)
        {
            Bookings.Clear();
            ErrorMessage = "Unable to load booking history. Check your connection and try again.";
        }
        finally { IsLoading = false; OnPropertyChanged(nameof(IsEmpty)); }
    }
}
