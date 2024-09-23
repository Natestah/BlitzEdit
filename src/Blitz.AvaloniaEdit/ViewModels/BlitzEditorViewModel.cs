using System.Collections.ObjectModel;
using ReactiveUI;

namespace Blitz.AvaloniaEdit.ViewModels;

public class BlitzEditorViewModel : ViewModelBase
{
    private BlitzDocument? _selectedFile;
    public ObservableCollection<BlitzDocument> OpenedFiles { get; set; } = [];

    public BlitzDocument? SelectedFile
    {
        get => _selectedFile;
        set => this.RaiseAndSetIfChanged(ref _selectedFile, value);
    }
}