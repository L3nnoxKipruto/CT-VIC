using Microsoft.EntityFrameworkCore;
using DataEntryApp.Models;
using System.IO;
using System;

namespace DataEntryApp.Data;

public class AppDbContext : DbContext
{
    public DbSet<Entry> Entries { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadiologyDMS");
        if (!Directory.Exists(dbPath)) Directory.CreateDirectory(dbPath);
        
        optionsBuilder.UseSqlite($"Data Source={Path.Combine(dbPath, "radiology.db")}");
    }
}
