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
        await db.Database.EnsureCreatedAsync();
    }

    public async Task<List<Entry>> GetEntriesAsync()
    {
        using var db = new AppDbContext();
        return await db.Entries.OrderByDescending(e => e.CreatedAt).ToListAsync();
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
