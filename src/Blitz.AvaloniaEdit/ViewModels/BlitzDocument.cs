using System;
using System.Threading.Tasks;
using ReactiveUI;

namespace Blitz.AvaloniaEdit.ViewModels;

/// <summary>
/// Tab View Model For Documents, Tab items only retain Dirty Text Information,  There is only one Avalonia document loaded this way.
/// </summary>
public class BlitzDocument : ViewModelBase
{
    private bool _isDirty = false;

    public enum DocumentType
    {
        Untitled,
        Preview,
        File
    }
    
    public DateTime LastModified { get; set; } = DateTime.MinValue;

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public string DirtyText { get; set; }

    public BlitzDocument(DocumentType documentType, string fileNameOrTitle)
    {
    }
    
}