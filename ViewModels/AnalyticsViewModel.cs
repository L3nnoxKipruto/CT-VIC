using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Models;
using DataEntryApp.Messages;
using DataEntryApp.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace DataEntryApp.ViewModels;

public partial class AnalyticsViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;

    [ObservableProperty] private double _averageCtdi;
    [ObservableProperty] private double _averageDlp;
    [ObservableProperty] private double _p75Ctdi;
    [ObservableProperty] private double _p75Dlp;
    
    [ObservableProperty] private ObservableCollection<MonthlyTrend> _trends = new();
    [ObservableProperty] private ObservableCollection<ProtocolStat> _topProtocols = new();
    [ObservableProperty] private ObservableCollection<WeightGroupStat> _weightStats = new();
    
    // LiveCharts Properties
    [ObservableProperty] private ISeries[] _doseSeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _categorySeries = Array.Empty<ISeries>();
    [ObservableProperty] private ISeries[] _qualitySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _xAxes = Array.Empty<Axis>();

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

        // Calculate 75th Percentile (DRL)
        P75Ctdi = CalculatePercentile(entries.Select(e => e.CTDIvol).ToList(), 0.75);
        P75Dlp = CalculatePercentile(entries.Select(e => e.DLP).ToList(), 0.75);

        // Weight Groups Stats
        var weightGroups = new List<WeightGroupStat>
        {
            CreateWeightStat(entries, "< 60kg", e => e.Weight < 60),
            CreateWeightStat(entries, "60 - 90kg", e => e.Weight >= 60 && e.Weight <= 90),
            CreateWeightStat(entries, "> 90kg", e => e.Weight > 90)
        };
        WeightStats = new ObservableCollection<WeightGroupStat>(weightGroups.Where(w => w.Count > 0));

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

        // 1. Dose Trends Chart (Line/Area)
        DoseSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = monthlyData.Select(m => m.AverageDose).ToArray(),
                Name = "Avg DLP",
                Fill = new SolidColorPaint(SKColor.Parse("#4F46E5").WithAlpha(40)),
                Stroke = new SolidColorPaint(SKColor.Parse("#4F46E5"), 3),
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#4F46E5"), 3)
            }
        };

        XAxes = new[]
        {
            new Axis
            {
                Labels = monthlyData.Select(m => m.MonthName).ToArray(),
                LabelsRotation = 0,
                Padding = new LiveChartsCore.Drawing.Padding(0, 15),
                TextSize = 12
            }
        };

        // 2. Category Distribution (Pie)
        var categoryCounts = new Dictionary<string, int>
        {
            { "Acute Abdomen", entries.Count(e => e.IsAcuteAbdomen) },
            { "Trauma", entries.Count(e => e.IsAbdominopelvicTrauma) },
            { "Masses", entries.Count(e => e.IsAbdominopelvicMasses) }
        };

        CategorySeries = categoryCounts
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => new PieSeries<int>
            {
                Values = new[] { kvp.Value },
                Name = kvp.Key
            })
            .ToArray();

        // 3. Quality Acceptance (Gauge)
        var total = entries.Count;
        var accepted = entries.Count(e => e.IsImageQualityAccepted);
        var qualityPercent = total > 0 ? (double)accepted / total * 100 : 0;
        
        QualitySeries = new ISeries[]
        {
            new PieSeries<double> { Values = new[] { qualityPercent }, Name = "Accepted", InnerRadius = 60 },
            new PieSeries<double> { Values = new[] { 100 - qualityPercent }, Name = "Rejected", InnerRadius = 60, Fill = new SolidColorPaint(SKColors.LightGray.WithAlpha(100)) }
        };

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

    private double CalculatePercentile(List<double> values, double percentile)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        int n = sorted.Count;
        double realIndex = percentile * (n - 1);
        int index = (int)realIndex;
        double fraction = realIndex - index;
        if (index + 1 < n)
            return sorted[index] + (fraction * (sorted[index + 1] - sorted[index]));
        return sorted[index];
    }

    private WeightGroupStat CreateWeightStat(System.Collections.Generic.IEnumerable<Entry> entries, string label, Func<Entry, bool> predicate)
    {
        var group = entries.Where(predicate).ToList();
        return new WeightGroupStat
        {
            Label = label,
            Count = group.Count,
            AverageCtdi = group.Any() ? group.Average(e => e.CTDIvol) : 0,
            AverageDlp = group.Any() ? group.Average(e => e.DLP) : 0
        };
    }
}

public class WeightGroupStat
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageCtdi { get; set; }
    public double AverageDlp { get; set; }
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
