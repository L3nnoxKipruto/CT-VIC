using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DataEntryApp.Data;
using DataEntryApp.Models;

namespace DataEntryApp.Services;

public class DatabaseService
{
    public async Task InitializeAsync()
    {
        using var db = new AppDbContext();
        await db.Database.MigrateAsync();

        // Self-Healing: Handle legacy column mismatches (sqlite)
        var migrationSteps = new[] { 
            "ScanningMode TEXT NOT NULL DEFAULT 'Helical'",
            "IsAecUsed INTEGER NOT NULL DEFAULT 1",
            "ReferencePhantom TEXT NOT NULL DEFAULT 'Body (16 cm)'",
            "IsImageQualityAccepted INTEGER NOT NULL DEFAULT 1",
            "Height REAL NOT NULL DEFAULT 0",
            "CreatedAt TEXT NOT NULL DEFAULT '2026-01-01'",
            "Status TEXT NOT NULL DEFAULT 'Incomplete'",
            "IsAbdominopelvicTrauma INTEGER NOT NULL DEFAULT 0",
            "IsAbdominopelvicMasses INTEGER NOT NULL DEFAULT 0"
        };

        foreach (var col in migrationSteps)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE Entries ADD COLUMN {col}");
            }
            catch { /* Ignore if exists */ }
        }

        // Fix NOT NULL constraint for legacy columns by adding defaults
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Entries ADD COLUMN IsTrauma INTEGER DEFAULT 0"); } catch { }
        try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE Entries ADD COLUMN IsMasses INTEGER DEFAULT 0"); } catch { }
    }

    public async Task<List<Entry>> GetEntriesAsync()
    {
        using var db = new AppDbContext();
        return await db.Entries.OrderByDescending(e => e.CreatedAt).ToListAsync();
    }

    public async Task<Entry?> GetLatestEntryByPatientIdAsync(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId)) return null;
        using var db = new AppDbContext();
        return await db.Entries
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Entry>> GetEntriesByCategoryAsync(string category)
    {
        using var db = new AppDbContext();
        return await db.Entries
            .Where(e => (category == "Acute Abdomen" && e.IsAcuteAbdomen) ||
                        (category == "Trauma" && e.IsAbdominopelvicTrauma) ||
                        (category == "Masses" && e.IsAbdominopelvicMasses))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> SaveEntryAsync(Entry entry)
    {
        using var db = new AppDbContext();
        var existing = await db.Entries.FindAsync(entry.Id);
        
        if (existing == null)
        {
            db.Entries.Add(entry);
        }
        else
        {
            db.Entry(existing).CurrentValues.SetValues(entry);
        }
        
        return await db.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteEntryAsync(string id)
    {
        using var db = new AppDbContext();
        var entry = await db.Entries.FindAsync(id);
        if (entry != null)
        {
            db.Entries.Remove(entry);
            return await db.SaveChangesAsync() > 0;
        }
        return false;
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        using var db = new AppDbContext();
        var all = await db.Entries.ToListAsync();
        
        int total = all.Count;
        int complete = all.Count(e => e.Status == "Complete");
        int draft = total - complete;
        double rate = total > 0 ? (double)complete / total * 100 : 0;
        
        return new DashboardStats
        {
            TotalRecords = total,
            CompleteCount = complete,
            DraftCount = draft,
            CompletionRate = rate,
            AverageDlp = all.Any() ? all.Average(e => e.DLP) : 0
        };
    }
}

public class DashboardStats
{
    public int TotalRecords { get; set; }
    public int CompleteCount { get; set; }
    public int DraftCount { get; set; }
    public double CompletionRate { get; set; }
    public double AverageDlp { get; set; }
}
