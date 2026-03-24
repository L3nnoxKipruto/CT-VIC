using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataEntryApp.Services;

namespace DataEntryApp.ViewModels;

public partial class ExportViewModel : ViewModelBase
{
    private readonly RecordsViewModel _recordsViewModel;

    public ExportViewModel(RecordsViewModel recordsViewModel)
    {
        _recordsViewModel = recordsViewModel;
        ExportDataCommand = new AsyncRelayCommand(ExportDataAsync);
    }

    public IAsyncRelayCommand ExportDataCommand { get; }

    [ObservableProperty] private string _exportStatusLine = "Ready to export securely.";

    private async Task ExportDataAsync()
    {
        ExportStatusLine = "Opening file picker...";
        
        var success = await _recordsViewModel.ExportToExcelAsync();
        
        if (success)
        {
            ExportStatusLine = "✅ Report exported successfully!";
        }
        else
        {
            ExportStatusLine = "❌ Export cancelled or failed.";
        }
        
        // Clear status after 5 seconds
        _ = Task.Delay(5000).ContinueWith(_ => ExportStatusLine = "Ready to export securely.");
    }
}
