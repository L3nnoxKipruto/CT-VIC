using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Services;
using DataEntryApp.Messages;
using System.Threading.Tasks;

namespace DataEntryApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private MenuItemViewModel? _selectedMenuItem;

    public AvaloniaList<MenuItemViewModel> MenuItems { get; } = new();

    public MainViewModel()
    {
        _dbService = new DatabaseService();
        
        // Initialize Database
        _ = InitializeAsync();

        DashboardPage = new DashboardViewModel(_dbService);
        DataEntryPage = new DataEntryViewModel(_dbService);
        RecordsPage = new RecordsViewModel(_dbService);
        AnalyticsPage = new AnalyticsViewModel(_dbService);
        ExportPage = new ExportViewModel(RecordsPage);

        MenuItems.Add(new MenuItemViewModel("Dashboard", "Home", DashboardPage));
        MenuItems.Add(new MenuItemViewModel("Add Patient Data", "Edit", DataEntryPage));
        MenuItems.Add(new MenuItemViewModel("Clinical Registry", "List", RecordsPage));
        MenuItems.Add(new MenuItemViewModel("Analytics", "DataArea", AnalyticsPage));
        MenuItems.Add(new MenuItemViewModel("Export Report", "Document", ExportPage));

        SelectedMenuItem = MenuItems[1];
        CurrentPage = SelectedMenuItem.Page;

        Notification = new NotificationViewModel();

        WeakReferenceMessenger.Default.Register<EditEntryMessage>(this, (r, m) =>
        {
            CurrentPage = DataEntryPage;
            SelectedMenuItem = MenuItems[1];
        });

        WeakReferenceMessenger.Default.Register<NotificationMessage>(this, (r, m) =>
        {
            Notification.Show(m.Title, m.Message, m.Type);
        });
    }

    public NotificationViewModel Notification { get; }

    [ObservableProperty]
    private bool _isInitializing = true;

    private async Task InitializeAsync()
    {
        try 
        {
            await _dbService.InitializeAsync();
            // Artificial delay for smooth splash screen transition
            await Task.Delay(2000);
        }
        finally
        {
            IsInitializing = false;
        }
    }

    public DashboardViewModel DashboardPage { get; }
    public DataEntryViewModel DataEntryPage { get; }
    public RecordsViewModel RecordsPage { get; }
    public AnalyticsViewModel AnalyticsPage { get; }
    public ExportViewModel ExportPage { get; }

    partial void OnSelectedMenuItemChanged(MenuItemViewModel? value)
    {
        if (value != null)
        {
            CurrentPage = value.Page;
        }
    }
}

public class MenuItemViewModel
{
    public string Header { get; }
    public string Icon { get; }
    public ViewModelBase Page { get; }

    public MenuItemViewModel(string header, string icon, ViewModelBase page)
    {
        Header = header;
        Icon = icon;
        Page = page;
    }
}
