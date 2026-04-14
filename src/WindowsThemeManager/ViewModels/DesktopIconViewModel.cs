using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WindowsThemeManager.Core.Interfaces;
using WindowsThemeManager.Core.Models;

namespace WindowsThemeManager.ViewModels;

/// <summary>
/// ViewModel for the desktop icon backup/restore feature.
/// </summary>
public partial class DesktopIconViewModel : ObservableObject
{
    private readonly IDesktopIconService _desktopIconService;
    private readonly ILogger<DesktopIconViewModel> _logger;
    private readonly string _layoutsDirectory;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private DesktopIconLayout? _currentLayout;

    [ObservableProperty]
    private DesktopIconLayout? _savedLayout;

    [ObservableProperty]
    private string _selectedFilePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _savedLayouts = new();

    [ObservableProperty]
    private string _selectedLayoutName = string.Empty;

    public DesktopIconViewModel(
        IDesktopIconService desktopIconService,
        ILogger<DesktopIconViewModel> logger)
    {
        _desktopIconService = desktopIconService;
        _logger = logger;
        _layoutsDirectory = desktopIconService.LayoutsDirectory;

        LoadSavedLayoutsList();
    }

    /// <summary>
    /// Captures the current desktop icon layout.
    /// </summary>
    [RelayCommand]
    private async Task CaptureLayoutAsync()
    {
        IsProcessing = true;
        StatusMessage = "Capturing icon positions...";

        try
        {
            CurrentLayout = await _desktopIconService.CaptureLayoutAsync();
            StatusMessage = $"Captured {CurrentLayout.Icons.Count} icon positions";
            _logger.LogInformation("Captured layout with {Count} icons", CurrentLayout.Icons.Count);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to capture desktop icon layout");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Restores the captured layout.
    /// </summary>
    [RelayCommand]
    private async Task RestoreCapturedLayoutAsync()
    {
        if (CurrentLayout == null)
        {
            StatusMessage = "No layout captured. Please capture first.";
            return;
        }

        await RestoreLayoutAsync(CurrentLayout);
    }

    /// <summary>
    /// Saves the captured layout to a file with a Save As dialog.
    /// </summary>
    [RelayCommand]
    private async Task SaveCapturedLayoutAsync()
    {
        if (CurrentLayout == null)
        {
            StatusMessage = "No layout captured. Please capture first.";
            return;
        }

        // Show Save As dialog
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Save Icon Layout Backup",
            FileName = $"DesktopIcons_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            DefaultExt = "json",
            AddExtension = true
        };

        if (dialog.ShowDialog() == true)
        {
            IsProcessing = true;
            StatusMessage = "Saving layout...";

            try
            {
                var filePath = await _desktopIconService.SaveLayoutToFileAsync(CurrentLayout, dialog.FileName);
                StatusMessage = $"✅ Backup saved to: {filePath}";
                _logger.LogInformation("Backup saved to {Path}", filePath);
                
                // Also save a copy to the internal layouts directory for the list
                var internalPath = Path.Combine(_layoutsDirectory, $"{CurrentLayout.LayoutName}.json");
                if (!File.Exists(internalPath))
                {
                    await _desktopIconService.SaveLayoutToFileAsync(CurrentLayout, internalPath);
                    LoadSavedLayoutsList();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Failed to save layout");
            }
            finally
            {
                IsProcessing = false;
            }
        }
        else
        {
            StatusMessage = "Save cancelled.";
        }
    }

    /// <summary>
    /// Opens the folder containing saved layout backups.
    /// </summary>
    [RelayCommand]
    private void OpenBackupsFolder()
    {
        try
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
            StatusMessage = "Opened Desktop folder - backups are saved there by default";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error opening folder: {ex.Message}";
            _logger.LogError(ex, "Failed to open backups folder");
        }
    }

    /// <summary>
    /// Loads a layout from file.
    /// </summary>
    [RelayCommand]
    private async Task LoadLayoutFromFileAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = "Select Icon Layout File",
            InitialDirectory = _desktopIconService.LayoutsDirectory
        };

        if (dialog.ShowDialog() == true)
        {
            IsProcessing = true;
            StatusMessage = "Loading layout...";

            try
            {
                SavedLayout = await _desktopIconService.LoadLayoutFromFileAsync(dialog.FileName);
                SelectedFilePath = dialog.FileName;
                StatusMessage = $"Loaded {SavedLayout.Icons.Count} icons from {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                _logger.LogError(ex, "Failed to load layout from file");
            }
            finally
            {
                IsProcessing = false;
            }
        }
    }

    /// <summary>
    /// Restores the loaded layout from file.
    /// </summary>
    [RelayCommand]
    private async Task RestoreSavedLayoutAsync()
    {
        if (SavedLayout == null)
        {
            StatusMessage = "No layout loaded. Please load a file first.";
            return;
        }

        await RestoreLayoutAsync(SavedLayout);
    }

    /// <summary>
    /// Restores a layout from the saved layouts list.
    /// </summary>
    [RelayCommand]
    private async Task RestoreSelectedLayoutAsync()
    {
        if (string.IsNullOrEmpty(SelectedLayoutName))
        {
            StatusMessage = "Please select a layout from the list.";
            return;
        }

        var filePath = Path.Combine(_desktopIconService.LayoutsDirectory, $"{SelectedLayoutName}.json");
        if (!File.Exists(filePath))
        {
            StatusMessage = "Selected file not found.";
            return;
        }

        IsProcessing = true;
        StatusMessage = $"Loading {SelectedLayoutName}...";

        try
        {
            var layout = await _desktopIconService.LoadLayoutFromFileAsync(filePath);
            await RestoreLayoutAsync(layout);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to restore selected layout");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    /// <summary>
    /// Deletes a saved layout file.
    /// </summary>
    [RelayCommand]
    private void DeleteSelectedLayout()
    {
        if (string.IsNullOrEmpty(SelectedLayoutName))
        {
            StatusMessage = "Please select a layout to delete.";
            return;
        }

        var filePath = Path.Combine(_desktopIconService.LayoutsDirectory, $"{SelectedLayoutName}.json");
        if (!File.Exists(filePath))
        {
            StatusMessage = "File not found.";
            return;
        }

        try
        {
            File.Delete(filePath);
            StatusMessage = $"Deleted {SelectedLayoutName}";
            LoadSavedLayoutsList();
            SelectedLayoutName = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to delete layout");
        }
    }

    /// <summary>
    /// Refreshes the list of saved layouts.
    /// </summary>
    private void LoadSavedLayoutsList()
    {
        SavedLayouts.Clear();

        try
        {
            if (Directory.Exists(_desktopIconService.LayoutsDirectory))
            {
                var files = Directory.GetFiles(_desktopIconService.LayoutsDirectory, "*.json");
                foreach (var file in files)
                {
                    SavedLayouts.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved layouts list");
        }
    }

    /// <summary>
    /// Core method to restore a layout with user confirmation.
    /// </summary>
    private async Task RestoreLayoutAsync(DesktopIconLayout layout)
    {
        var result = MessageBox.Show(
            $"Restore {layout.Icons.Count} icon positions from '{layout.LayoutName}'?\n\n" +
            "This will move all desktop icons to their saved positions.",
            "Confirm Icon Restoration",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            StatusMessage = "Restoration cancelled.";
            return;
        }

        IsProcessing = true;
        StatusMessage = "Restoring icon positions...";

        try
        {
            var success = await _desktopIconService.RestoreLayoutAsync(layout);
            StatusMessage = success ? "Icons restored successfully!" : "Icon restoration failed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logger.LogError(ex, "Failed to restore layout");
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
