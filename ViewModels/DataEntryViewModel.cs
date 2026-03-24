using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DataEntryApp.Models;
using DataEntryApp.Services;
using DataEntryApp.Messages;

namespace DataEntryApp.ViewModels;

public partial class DataEntryViewModel : ViewModelBase
{
    private readonly DatabaseService _dbService;

    [ObservableProperty] private int _currentStep = 1;
    [ObservableProperty] private double _progressPercent = 25.0;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsNotStep4 => CurrentStep < 4;

    // STEP 1: Patient Information
    [ObservableProperty] 
    [Required] 
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand))]
    private string _patientIdSuffix = string.Empty;
    [ObservableProperty] private DateTime _date = DateTime.Now;
    [ObservableProperty] private int _age;
    [ObservableProperty] private double _weight;
    [ObservableProperty] private double _height;

    // STEP 2: Clinical Information
    [ObservableProperty] private bool _isAcuteAbdomen;
    [ObservableProperty] private bool _isAbdominopelvicTrauma;
    [ObservableProperty] private bool _isAbdominopelvicMasses;
    [ObservableProperty] private string _specificHistory = string.Empty;
    [ObservableProperty] private string _additionalNotes = string.Empty;

    // STEP 3: Scan Parameters
    [ObservableProperty] private string _protocolName = string.Empty;
    [ObservableProperty] private double _kvValue = 120;
    [ObservableProperty] private double _masValue = 200;
    [ObservableProperty] private double _sliceThickness = 1.25;
    [ObservableProperty] private double _rotationTime = 0.6;
    [ObservableProperty] private double _pitch;
    [ObservableProperty] private double _beamWidth;
    [ObservableProperty] private double _scanningRange;

    // STEP 4: Settings & Quality
    [ObservableProperty] private double _ctdiVol;
    [ObservableProperty] private double _dlpValue;
    [ObservableProperty] private string _scanningMode = "Helical";
    [ObservableProperty] private bool _isAecUsed = true;
    [ObservableProperty] private string _referencePhantom = "Body (16 cm)";
    [ObservableProperty] private bool _isImageQualityAccepted = true;

    [ObservableProperty] private string? _statusMessage;
    [ObservableProperty] private bool _isBusy;

    public DataEntryViewModel(DatabaseService dbService)
    {
        _dbService = dbService;
        NextStepCommand = new RelayCommand(NextStep, CanNextStep);
        PreviousStepCommand = new RelayCommand(PreviousStep, CanPreviousStep);
        GoToStepCommand = new RelayCommand<string>(GoToStep);
        SubmitCommand = new AsyncRelayCommand(SubmitAsync, () => !IsBusy);
        SaveDraftCommand = new AsyncRelayCommand(SaveDraftAsync, () => !IsBusy);

        WeakReferenceMessenger.Default.Register<EditEntryMessage>(this, (r, m) => LoadEntry(m.Entry));
    }

    private string _editingEntryId = string.Empty;

    private void LoadEntry(Entry entry)
    {
        _editingEntryId = entry.Id;
        PatientIdSuffix = entry.PatientId.StartsWith("UMR-", StringComparison.OrdinalIgnoreCase) 
            ? entry.PatientId.Substring(4) 
            : (entry.PatientId.StartsWith("UMR", StringComparison.OrdinalIgnoreCase) 
                ? entry.PatientId.Substring(3) 
                : entry.PatientId);
        Date = entry.Date;
        Age = entry.Age;
        Weight = entry.Weight;
        Height = entry.Height;
        
        IsAcuteAbdomen = entry.IsAcuteAbdomen;
        IsAbdominopelvicTrauma = entry.IsAbdominopelvicTrauma;
        IsAbdominopelvicMasses = entry.IsAbdominopelvicMasses;
        SpecificHistory = entry.SpecificHistory;
        AdditionalNotes = entry.AdditionalNotes;
        
        ProtocolName = entry.ProtocolName;
        KvValue = entry.KV;
        MasValue = entry.MAS;
        SliceThickness = entry.SliceThickness;
        RotationTime = entry.RotationTime;
        Pitch = entry.Pitch;
        BeamWidth = entry.BeamWidth;
        ScanningRange = entry.ScanningRange;
        
        CtdiVol = entry.CTDIvol;
        DlpValue = entry.DLP;
        ScanningMode = entry.ScanningMode;
        IsAecUsed = entry.IsAecUsed;
        ReferencePhantom = entry.ReferencePhantom;
        IsImageQualityAccepted = entry.IsImageQualityAccepted;

        CurrentStep = 1;
        UpdateProgress();
        StatusMessage = "Editing Record...";
    }

    public IRelayCommand NextStepCommand { get; }
    public IRelayCommand PreviousStepCommand { get; }
    public IRelayCommand<string> GoToStepCommand { get; }
    public IAsyncRelayCommand SubmitCommand { get; }
    public IAsyncRelayCommand SaveDraftCommand { get; }

    private void GoToStep(string? stepStr)
    {
        if (int.TryParse(stepStr, out int step))
        {
            if (step > 1 && string.IsNullOrWhiteSpace(PatientIdSuffix))
            {
                StatusMessage = "Patient ID is mandatory to continue.";
                UpdateProgress(); // Resets visual UI toggles if they clicked ahead
                return;
            }
            CurrentStep = step;
            StatusMessage = string.Empty;
            UpdateProgress();
        }
    }

    private void NextStep() 
    { 
        if (CurrentStep == 1 && string.IsNullOrWhiteSpace(PatientIdSuffix))
        {
            StatusMessage = "Patient ID is mandatory to continue.";
            return;
        }
        if (CurrentStep < 4) { CurrentStep++; StatusMessage = string.Empty; UpdateProgress(); } 
    }
    
    private bool CanNextStep() 
    {
        if (CurrentStep == 1 && string.IsNullOrWhiteSpace(PatientIdSuffix)) return false;
        return CurrentStep < 4;
    }

    private void PreviousStep() { if (CurrentStep > 1) { CurrentStep--; StatusMessage = string.Empty; UpdateProgress(); } }
    private bool CanPreviousStep() => CurrentStep > 1;

    private void UpdateProgress()
    {
        ProgressPercent = (CurrentStep / 4.0) * 100.0;
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsNotStep4));
        
        NextStepCommand.NotifyCanExecuteChanged();
        PreviousStepCommand.NotifyCanExecuteChanged();
    }

    private async Task SaveDraftAsync() => await SaveAsync("Draft");
    private async Task SubmitAsync() => await SaveAsync("Complete");

    private async Task SaveAsync(string status)
    {
        if (string.IsNullOrWhiteSpace(PatientIdSuffix))
        {
            StatusMessage = "Cannot save: Patient ID is required.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Saving...";
        try
        {
            var entry = new Entry
            {
                Id = !string.IsNullOrEmpty(_editingEntryId) ? _editingEntryId : Guid.NewGuid().ToString(),
                PatientId = $"UMR{PatientIdSuffix}",
                Age = Age,
                Weight = Weight,
                Height = Height,
                Date = Date,
                IsAcuteAbdomen = IsAcuteAbdomen,
                IsAbdominopelvicTrauma = IsAbdominopelvicTrauma,
                IsAbdominopelvicMasses = IsAbdominopelvicMasses,
                SpecificHistory = SpecificHistory,
                AdditionalNotes = AdditionalNotes,
                ProtocolName = ProtocolName,
                KV = KvValue,
                MAS = MasValue,
                SliceThickness = SliceThickness,
                RotationTime = RotationTime,
                Pitch = Pitch,
                BeamWidth = BeamWidth,
                ScanningRange = ScanningRange,
                CTDIvol = CtdiVol,
                DLP = DlpValue,
                ScanningMode = ScanningMode,
                IsAecUsed = IsAecUsed,
                ReferencePhantom = ReferencePhantom,
                IsImageQualityAccepted = IsImageQualityAccepted,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            var success = await _dbService.SaveEntryAsync(entry);
            if (success)
            {
                StatusMessage = $"Record saved as {status}!";
                WeakReferenceMessenger.Default.Send(new EntryCreatedMessage(entry));
                if (status == "Complete") ResetWizard();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetWizard()
    {
        CurrentStep = 1;
        UpdateProgress();
        _editingEntryId = string.Empty;
        PatientIdSuffix = string.Empty;
        Age = 0;
        Weight = 0;
        Height = 0;
        Date = DateTime.Now;
        
        IsAcuteAbdomen = false;
        IsAbdominopelvicTrauma = false;
        IsAbdominopelvicMasses = false;
        SpecificHistory = string.Empty;
        AdditionalNotes = string.Empty;
        
        ProtocolName = string.Empty;
        KvValue = 120;
        MasValue = 200;
        SliceThickness = 1.25;
        RotationTime = 0.6;
        Pitch = 0;
        BeamWidth = 0;
        ScanningRange = 0;
        
        CtdiVol = 0;
        DlpValue = 0;
        ScanningMode = "Helical";
        IsAecUsed = true;
        ReferencePhantom = "Body (16 cm)";
        IsImageQualityAccepted = true;
        
        StatusMessage = string.Empty;
    }
}
