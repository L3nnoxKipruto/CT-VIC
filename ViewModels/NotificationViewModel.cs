using System;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DataEntryApp.ViewModels;

public partial class NotificationViewModel : ViewModelBase
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private Messages.NotificationType _type;
    [ObservableProperty] private bool _isVisible;

    private readonly Timer _timer;

    public NotificationViewModel()
    {
        _timer = new Timer(5000); // 5 seconds
        _timer.Elapsed += (s, e) => IsVisible = false;
        _timer.AutoReset = false;
    }

    public void Show(string title, string message, Messages.NotificationType type)
    {
        Title = title;
        Message = message;
        Type = type;
        IsVisible = true;
        
        _timer.Stop();
        _timer.Start();
    }
}
