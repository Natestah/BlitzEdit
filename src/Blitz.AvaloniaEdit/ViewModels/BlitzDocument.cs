using System;
using System.IO;
using ReactiveUI;

namespace Blitz.AvaloniaEdit.ViewModels;

/// <summary>
/// Tab View Model For Documents, Tab items only retain Dirty Text Information,  There is only one Avalonia document loaded this way.
/// </summary>
public class BlitzDocument : ViewModelBase
{
    private bool _isDirty = false;
    public string FileNameOrTitle { get; set; }
    
    
    
    //Todo: work out how we want to Scroll restore, it should prefer the actual scroll position
    //Scroll Position would get cleared when "Goto line/column" is initiated.
    //Also, this would go to a Model.. where we could disk store the states.
    public int AlignViewLine { get; set; }
    public int AlignViewColumn { get; set; }


    public string TabTitle
    {
        get
        {
            if (Type == DocumentType.File)
            {
                return Path.GetFileName(FileNameOrTitle);
            }
            
            //todo, multiple files with same name.. show more of the path to distinguish.

            return FileNameOrTitle;
        }
    }

    public DocumentType Type { get; set; }

    public enum DocumentType
    {
        Untitled,
        Preview,
        File
    }
    
    public string Extension => Type==DocumentType.File? Path.GetExtension(FileNameOrTitle): ".txt";
    
    public DateTime LastModified { get; set; } = DateTime.MinValue;

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public string DirtyText { get; set; }

    public BlitzDocument(DocumentType documentType, string fileNameOrTitle)
    {
        FileNameOrTitle = fileNameOrTitle;
        Type = documentType;
    }
    
}