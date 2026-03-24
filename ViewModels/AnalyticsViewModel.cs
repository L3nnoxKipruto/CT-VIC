using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Messages;
using DataEntryApp.Services;

namespace DataEntryApp.ViewModels;

public partial class AnalyticsViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;

    [ObservableProperty] private double _averageCtdi;
    [ObservableProperty] private double _averageDlp;
    
    [ObservableProperty] private ObservableCollection<MonthlyTrend> _trends = new();
    [ObservableProperty] private ObservableCollection<ProtocolStat> _topProtocols = new();

    public AnalyticsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;

        WeakReferenceMessenger.Default.Register<EntryCreatedMessage>(this, (r, m) =>
        {
            _ = LoadAnalyticsAsync();
        });

        _ = LoadAnalyticsAsync();
    }

    public async Task LoadAnalyticsAsync()
    {
        var entries = await _dbService.GetEntriesAsync();
        if (!entries.Any()) return;

        AverageCtdi = entries.Average(e => e.CTDIvol);
        AverageDlp = entries.Average(e => e.DLP);

        // Group by month
        var monthlyData = entries
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyTrend
            {
                MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                AverageDose = g.Average(e => e.DLP),
                Count = g.Count()
            })
            .Skip(Math.Max(0, entries.GroupBy(e => new { e.Date.Year, e.Date.Month }).Count() - 6)) // last 6 months
            .ToList();

        // If no data or just 1 month, provide some visual structure
        if (monthlyData.Count < 6)
        {
            // Pad with empty months for visual consistency
            var current = DateTime.Now;
            for (int i = 5; i >= 0; i--)
            {
                var targetMonth = current.AddMonths(-i);
                if (!monthlyData.Any(m => m.MonthName == targetMonth.ToString("MMM")))
                {
                    monthlyData.Add(new MonthlyTrend { MonthName = targetMonth.ToString("MMM"), AverageDose = 0, Count = 0 });
                }
            }
            monthlyData = monthlyData.OrderBy(m => DateTime.ParseExact(m.MonthName, "MMM", null).Month).ToList();
        }

        Trends = new ObservableCollection<MonthlyTrend>(monthlyData);

        // Top Protocols
        TopProtocols = new ObservableCollection<ProtocolStat>(
            entries.GroupBy(e => e.ProtocolName)
                  .Select(g => new ProtocolStat 
                  { 
                      Name = string.IsNullOrWhiteSpace(g.Key) ? "Unknown" : g.Key, 
                      Count = g.Count(),
                      AverageDlp = g.Average(e => e.DLP)
                  })
                  .OrderByDescending(p => p.Count)
                  .Take(5)
                  .ToList()
        );
    }
}

public class MonthlyTrend
{
    public string MonthName { get; set; } = string.Empty;
    public double AverageDose { get; set; }
    public int Count { get; set; }

    // Normalize height for the UI bar chart (max 200px)
    public double BarHeight => Math.Min(200, Math.Max(5, AverageDose / 5.0)); 
}

public class ProtocolStat
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageDlp { get; set; }
}
