using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Models;
using DataEntryApp.Services;
using DataEntryApp.Messages;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;

namespace DataEntryApp.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;

    [ObservableProperty] private int _totalEntries;
    [ObservableProperty] private int _completeCount;
    [ObservableProperty] private int _draftCount;
    [ObservableProperty] private double _completionRate;
    [ObservableProperty] private double _averageDlp;

    [ObservableProperty]
    private ObservableCollection<Entry> _latestEntries = new();

    [ObservableProperty]
    private ISeries[] _activitySeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _distributionSeries = Array.Empty<ISeries>();

    public DashboardViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        
        WeakReferenceMessenger.Default.Register<EntryCreatedMessage>(this, (r, m) =>
        {
            _ = LoadStatsAsync();
        });

        _ = LoadStatsAsync();
    }

    public async Task LoadStatsAsync()
    {
        var stats = await _dbService.GetDashboardStatsAsync();
        TotalEntries = stats.TotalRecords;
        CompleteCount = stats.CompleteCount;
        DraftCount = stats.DraftCount;
        CompletionRate = stats.CompletionRate;
        AverageDlp = stats.AverageDlp;

        var entries = await _dbService.GetEntriesAsync();
        LatestEntries = new ObservableCollection<Entry>(entries.Take(5));

        DistributionSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { CompleteCount }, Name = "Complete", Fill = new SolidColorPaint(SKColor.Parse("#2D8B7E")), InnerRadius = 60 },
            new PieSeries<int> { Values = new[] { DraftCount }, Name = "Draft", Fill = new SolidColorPaint(SKColor.Parse("#F1F5F9")), InnerRadius = 60 }
        };

        var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
        var dailyCounts = last7Days.Select(day => entries.Count(e => e.CreatedAt.Date == day)).ToArray();

        ActivitySeries = new ISeries[]
        {
            new ColumnSeries<int>
            {
                Values = dailyCounts,
                Fill = new SolidColorPaint(SKColor.Parse("#2D8B7E")),
                MaxBarWidth = 40,
                Rx = 4, Ry = 4
            }
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = last7Days.Select(d => d.ToString("ddd")).ToArray(),
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                TextSize = 12
            }
        };
    }
}
