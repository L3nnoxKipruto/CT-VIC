using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Models;
using DataEntryApp.Services;
using DataEntryApp.Messages;
using ClosedXML.Excel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Collections.Generic;

namespace DataEntryApp.ViewModels;

public partial class RecordsViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;
    private List<Entry> _allEntries = new();

    [ObservableProperty] private ObservableCollection<Entry> _entries = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _statusFilter = "All"; // All, Complete, Draft

    public RecordsViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        LoadEntriesCommand = new AsyncRelayCommand(LoadEntriesAsync);
        ExportToExcelCommand = new AsyncRelayCommand<object?>(p => ExportToExcelAsync(p));
        DeleteEntryCommand = new AsyncRelayCommand<Entry>(DeleteEntryAsync);
        EditEntryCommand = new RelayCommand<Entry>(EditEntry);

        WeakReferenceMessenger.Default.Register<EntryCreatedMessage>(this, (r, m) => { _ = LoadEntriesAsync(); });

        _ = LoadEntriesAsync();
    }

    public void EditEntry(Entry? entry)
    {
        if (entry != null)
        {
            WeakReferenceMessenger.Default.Send(new EditEntryMessage(entry));
        }
    }

    public IAsyncRelayCommand LoadEntriesCommand { get; }
    public IAsyncRelayCommand<object?> ExportToExcelCommand { get; }
    public IAsyncRelayCommand<Entry> DeleteEntryCommand { get; }
    public IRelayCommand<Entry> EditEntryCommand { get; }

    private async Task LoadEntriesAsync()
    {
        IsLoading = true;
        try
        {
            _allEntries = await _dbService.GetEntriesAsync();
            ApplyFilter();
        }
        finally { IsLoading = false; }
    }

    private void ApplyFilter()
    {
        var filtered = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(e => e.PatientId.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (StatusFilter != "All")
            filtered = filtered.Where(e => e.Status == StatusFilter);

        Entries = new ObservableCollection<Entry>(filtered);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();

    private async Task DeleteEntryAsync(Entry? entry)
    {
        if (entry == null) return;
        // In a real app, show confirmation dialog
        if (await _dbService.DeleteEntryAsync(entry.Id))
        {
            _allEntries.Remove(entry);
            ApplyFilter();
            WeakReferenceMessenger.Default.Send(new EntryCreatedMessage(entry)); // Trigger stats update
        }
    }

    public async Task<bool> ExportToExcelAsync(object? param = null)
    {
        if (!Entries.Any()) return false;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
        {
            var topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
            if (topLevel == null) return false;

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Clinical Registry Export",
                SuggestedFileName = $"RadiologyDMS_Export_{DateTime.Now:yyyyMMdd}.xlsx",
                DefaultExtension = "xlsx",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Excel Workbook")
                    {
                        Patterns = new[] { "*.xlsx" }
                    }
                }
            });

            if (file != null)
            {
                try
                {
                    using var workbook = new XLWorkbook();
                    var worksheet = workbook.Worksheets.Add("Clinical Registry");

                    // Professional Headers
                    string[] headers = { 
                        "Patient ID", "Age", "Weight (kg)", "Height (cm)", 
                        "Acute Abdomen", "Trauma", "Masses", "Clinical History", 
                        "Protocol", "KV", "MAS", "Slice Thickness", "Rotation Time", "Pitch", "Beam Width", "Scan Range",
                        "CTDIvol (mGy)", "DLP (mGy*cm)", "Scanning Mode", "AEC Used", "Reference Phantom", "IQ Accepted",
                        "Date", "Status" 
                    };
                    for (int h = 0; h < headers.Length; h++)
                    {
                        var cell = worksheet.Cell(1, h + 1);
                        cell.Value = headers[h];
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0D9488"); // Teal 600
                        cell.Style.Font.FontColor = XLColor.White;
                    }

                    // Data Rows
                    for (int i = 0; i < Entries.Count; i++)
                    {
                        var entry = Entries[i];
                        int row = i + 2;
                        worksheet.Cell(row, 1).Value = entry.PatientId;
                        worksheet.Cell(row, 2).Value = entry.Age;
                        worksheet.Cell(row, 3).Value = entry.Weight;
                        worksheet.Cell(row, 4).Value = entry.Height;
                        worksheet.Cell(row, 5).Value = entry.IsAcuteAbdomen ? "Yes" : "No";
                        worksheet.Cell(row, 6).Value = entry.IsAbdominopelvicTrauma ? "Yes" : "No";
                        worksheet.Cell(row, 7).Value = entry.IsAbdominopelvicMasses ? "Yes" : "No";
                        worksheet.Cell(row, 8).Value = entry.SpecificHistory;
                        worksheet.Cell(row, 9).Value = entry.ProtocolName;
                        worksheet.Cell(row, 10).Value = entry.KV;
                        worksheet.Cell(row, 11).Value = entry.MAS;
                        worksheet.Cell(row, 12).Value = entry.SliceThickness;
                        worksheet.Cell(row, 13).Value = entry.RotationTime;
                        worksheet.Cell(row, 14).Value = entry.Pitch;
                        worksheet.Cell(row, 15).Value = entry.BeamWidth;
                        worksheet.Cell(row, 16).Value = entry.ScanningRange;
                        worksheet.Cell(row, 17).Value = entry.CTDIvol;
                        worksheet.Cell(row, 18).Value = entry.DLP;
                        worksheet.Cell(row, 19).Value = entry.ScanningMode;
                        worksheet.Cell(row, 20).Value = entry.IsAecUsed ? "Yes" : "No";
                        worksheet.Cell(row, 21).Value = entry.ReferencePhantom;
                        worksheet.Cell(row, 22).Value = entry.IsImageQualityAccepted ? "Yes" : "No";
                        worksheet.Cell(row, 23).Value = entry.Date.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cell(row, 24).Value = entry.Status;
                    }

                    worksheet.Columns().AdjustToContents();

                    await using var stream = await file.OpenWriteAsync();
                    workbook.SaveAs(stream);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Export failed: {ex.Message}");
                    return false;
                }
            }
            return false;
        }
        return false;
    }
}
