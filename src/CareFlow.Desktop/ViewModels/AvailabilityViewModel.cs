using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CareFlow.Application.Availability;
using CareFlow.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CareFlow.Desktop.ViewModels;

public partial class AvailabilityViewModel : ViewModelBase
{
    private readonly IAvailabilityService _service;
    private readonly TimeZoneInfo _manila = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
    private CancellationTokenSource? _request;
    private int _requestVersion;

    public AvailabilityViewModel(IAvailabilityService service)
    {
        _service = service;
        var localNow = TimeZoneInfo.ConvertTime(TimeProvider.System.GetUtcNow(), _manila);
        selectedDate = new DateTimeOffset(localNow.Date, localNow.Offset);
    }

    [ObservableProperty] private DateTimeOffset? selectedDate;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    public ObservableCollection<ServiceAvailability> Items { get; } = new();
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool IsEmpty => !IsLoading && !HasError && Items.Count == 0;

    partial void OnSelectedDateChanged(DateTimeOffset? value) => _ = RefreshAsync();
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        _request?.Cancel();
        _request?.Dispose();
        _request = new CancellationTokenSource();
        var token = _request.Token;
        var version = ++_requestVersion;
        if (SelectedDate is null) { Items.Clear(); OnPropertyChanged(nameof(IsEmpty)); return; }
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var date = DateOnly.FromDateTime(SelectedDate.Value.DateTime);
            var results = await _service.GetAvailabilityAsync(date, token);
            if (token.IsCancellationRequested || version != _requestVersion) return;
            Items.Clear();
            foreach (var item in results) Items.Add(item);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception)
        {
            if (version != _requestVersion) return;
            Items.Clear();
            ErrorMessage = "Unable to load availability. Check the database connection and try again.";
        }
        finally
        {
            if (version == _requestVersion)
            {
                IsLoading = false;
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }
}
