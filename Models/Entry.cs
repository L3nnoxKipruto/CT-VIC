using System;
using System.ComponentModel.DataAnnotations;

namespace DataEntryApp.Models;

public class Entry
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string PatientId { get; set; } = string.Empty;

    public int Age { get; set; }
    public double Weight { get; set; }
    public double Height { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    // Clinical Info
    public string AdditionalNotes { get; set; } = string.Empty;
    public bool IsAcuteAbdomen { get; set; }
    public bool IsAbdominopelvicTrauma { get; set; }
    public bool IsAbdominopelvicMasses { get; set; }
    public string SpecificHistory { get; set; } = string.Empty;

    // Scan Parameters
    public string ProtocolName { get; set; } = string.Empty;
    public double KV { get; set; }
    public double MAS { get; set; }
    public double SliceThickness { get; set; }
    public double RotationTime { get; set; }
    public double Pitch { get; set; }
    public double BeamWidth { get; set; }
    public double ScanningRange { get; set; }
    public double CTDIvol { get; set; }
    public double DLP { get; set; }

    // New Fields for RadiologyDMS
    public string Status { get; set; } = "Draft"; // Draft, Complete
    public string ScanningMode { get; set; } = "Helical"; // Axial, Helical
    public bool IsAecUsed { get; set; } = true;
    public string ReferencePhantom { get; set; } = "Body (16 cm)";
    public bool IsImageQualityAccepted { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool IsHighDose { get; set; }

    public bool IsComplete => !string.IsNullOrEmpty(PatientId) && 
                              !string.IsNullOrEmpty(ProtocolName) && 
                              CTDIvol > 0 && 
                              DLP > 0;
}
