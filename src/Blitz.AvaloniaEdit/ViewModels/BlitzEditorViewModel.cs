using System;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia.Dialogs.Internal;
using AvaloniaEdit.TextMate;
using Avalonia.Media;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using ReactiveUI;

namespace Blitz.AvaloniaEdit.ViewModels;


/// <summary>
/// View Model For hosted editors, hold list of opened files ( file tabs ) and a selection list (Typically one)
/// </summary>
public class BlitzEditorViewModel : ViewModelBase
{
    private TextMate.Installation? _textMateInstallation;
    private IBrush? _statusBarForeground;
    private IBrush? _statusBarBackground;
    private IBrush? _titleBarBackground = Brushes.Sienna;
    private IBrush? _textForeground;
    private string? _searchThisPreviewText;

    public InstallationInstallerDelegate? TextMateInstaller { get; set; }
    public delegate TextMate.Installation InstallationInstallerDelegate(RegistryOptions options);
    public Action<TextMate.Installation>? BackGroundForeGroundUpdate;

    private ThemeViewModel _blitzThemeViewModel;
    public ThemeViewModel? ThemeViewModel
    {
        get => _blitzThemeViewModel;
        set
        {
            if (value == null)
                return;
            
            //Todo: Configuration
            //Configuration.Instance.CurrentTheme = value.Theme;
            
            _blitzThemeViewModel = value;
            UpdateTheme();
        }
    }
    private ObservableCollection<object> _selectedFiles = [];

    /// <summary>
    /// All the files that are opened ( file tabs )
    /// </summary>
    public ObservableCollection<BlitzDocument> OpenedFiles { get; set; } = [];

    /// <summary>
    /// Selected file(s)
    /// </summary>
    public ObservableCollection<object> SelectedFiles
    {
        get => _selectedFiles;
        set => this.RaiseAndSetIfChanged(ref _selectedFiles, value);
    }

    /// <summary>
    /// Gets the current opened file, if one isn't in the collection a new one 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="openInPreview"></param>
    /// <param name="lineNumber"></param>
    /// <param name="columnNumber"></param>
    /// <returns></returns>
    public BlitzDocument GetOpenedOrCreateFile(string fileName, bool openInPreview = false, int lineNumber = 1, int columnNumber = 1)
    {
        int afterSelectedIndex = 0;
        if (SelectedFiles.FirstOrDefault() is BlitzDocument selected)
        {
            afterSelectedIndex = OpenedFiles.IndexOf(selected) + 1;
        }

        foreach (var item in OpenedFiles.OfType<BlitzDocument>())
        {
            var isRequestedFile = item.FileNameOrTitle == fileName;
            if (openInPreview && item.IsPreviewing && !isRequestedFile)
            {
                var index = OpenedFiles.IndexOf(item);
                OpenedFiles.Remove(item);
                var updatedPreview = new BlitzDocument(BlitzDocument.DocumentType.File, fileName){AlignViewLine = lineNumber, AlignViewColumn = columnNumber, IsPreviewing = openInPreview};
                OpenedFiles.Insert(index, updatedPreview);
                SelectDocument(updatedPreview);
                return updatedPreview;
            }
            
            if (item.Type != BlitzDocument.DocumentType.File
                || !isRequestedFile)
            {
                continue;
            }
            item.AlignViewLine = lineNumber;
            item.AlignViewColumn = columnNumber;
            SelectDocument(item);
            return item;
        }

        
        var returnDocument = new BlitzDocument(BlitzDocument.DocumentType.File, fileName){AlignViewLine = lineNumber, AlignViewColumn = columnNumber, IsPreviewing = openInPreview};
        OpenedFiles.Insert(afterSelectedIndex, returnDocument);
        SelectDocument(returnDocument);
        return returnDocument;
    }

    private void SelectDocument(BlitzDocument document)
    {
        SelectedFiles.Clear();
        SelectedFiles.Add(document);
    }
    
    
    public string? SearchThisPreviewText
    {
        get => _searchThisPreviewText;
        set => this.RaiseAndSetIfChanged(ref _searchThisPreviewText,value); 
    }


    /// <summary>
    /// Request Line when setting selection, this will goto, once the file is loaded.
    /// </summary>
    public int RequestedGotoLine { get; set; } = -1;
    
    /// <summary>
    /// Request Line when setting selection. This will goto, once the file is loaded.
    /// </summary>
    public int RequestedGotoColumn { get; set; } = -1;
    
    
    public ObservableCollection<ThemeViewModel> AllThemeViewModels { get; } = [];

    public void PopulateThemeModels()
    {
        foreach (ThemeName themeName in Enum.GetValuesAsUnderlyingType(typeof(TextMateSharp.Grammars.ThemeName)))
        {
            var newBlitzTHeme = themeName.ToString().ToLower().Contains("light") ? FromBase(BlitzTheme.Light, themeName) : FromBase(BlitzTheme.Dark, themeName);
            AllThemeViewModels.Add( new ThemeViewModel(this, newBlitzTHeme));
        }

        // Todo: Configuration..
        // foreach (var themeViewModel in AllThemeViewModels)
        // {
        //     if ( themeViewModel.Theme.ThemeName == Configuration.Instance.SelectedThemePremium)
        //     {
        //         this.ThemeViewModel = themeViewModel;
        //         return;
        //     }
        // }

        this.ThemeViewModel = AllThemeViewModels.FirstOrDefault(model => model.ThemeName == ThemeName.Monokai);
    }
    
    public TextMate.Installation? TextMateInstallation
    {
        get => _textMateInstallation;
        set => this.RaiseAndSetIfChanged(ref _textMateInstallation, value);
    }

    private RegistryOptions? _textMateRegistryOptions;
    private FontFamily _selectedFontFamily;

    public ThemeName ConfiguredThemeName { get; set; } = ThemeName.DarkPlus;
    public RegistryOptions TextMateRegistryOptions
    {
        get
        {
            if (_textMateRegistryOptions != null) return _textMateRegistryOptions;
            var options =  new RegistryOptions(ConfiguredThemeName);
            return options;
           
        }
        set
        {
            _textMateRegistryOptions = value;
            if (TextMateInstallation != null)
            {
                TextMateInstallation.AppliedTheme -= TextMateInstallationOnAppliedTheme;
                TextMateInstallation.Dispose();
            }

            if (TextMateInstaller != null)
            {
                var newInstallation = TextMateInstaller.Invoke(value);
                newInstallation.AppliedTheme += TextMateInstallationOnAppliedTheme;
                TextMateInstallation = newInstallation;
            
                this.RaiseAndSetIfChanged(ref _textMateRegistryOptions, value);
                TextMateInstallationOnAppliedTheme(this, newInstallation);
            }
        }
    }
    
    
    
    bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
    {
        if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
            return false;

        if (!Color.TryParse(colorString, out Color color))
            return false;

        var colorBrush = new SolidColorBrush(color);
        applyColorAction(colorBrush);
        return true;
    }


    public IBrush? StatusBarForeground
    {
        get => _statusBarForeground;
        set => this.RaiseAndSetIfChanged(ref _statusBarForeground, value);
    }

    public IBrush? StatusBarBackground
    {
        get => _statusBarBackground;
        set => this.RaiseAndSetIfChanged(ref _statusBarBackground, value);
    }

    public IBrush? TitleBarBackground
    {
        get => _titleBarBackground;
        set => this.RaiseAndSetIfChanged(ref _titleBarBackground, value);
    }

    public IBrush? TextForeground
    {
        get => _textForeground;
        set => this.RaiseAndSetIfChanged(ref _textForeground, value);
    }

    public FontFamily SelectedFontFamily
    {
        get => _selectedFontFamily ?? FontFamily.Default;
        set => this.RaiseAndSetIfChanged(ref _selectedFontFamily, value);
    }

    private void TextMateInstallationOnAppliedTheme(object? sender, TextMate.Installation e)
    {
        if (!ApplyBrushAction(e,"statusBar.background", brush => StatusBarBackground = brush))
        {
            StatusBarBackground = Brushes.Transparent;
        }

        if (!ApplyBrushAction(e, "titleBar.activeBackground", brush => TitleBarBackground = brush))
        {
            TitleBarBackground = Brushes.Transparent;
        }

        if (!ApplyBrushAction(e,"statusBar.foreground", brush => StatusBarForeground = brush))
        {
            ApplyBrushAction(e,"editor.foreground", brush => StatusBarForeground = brush);
        }

        ApplyBrushAction(e,"editor.foreground", brush => TextForeground = brush);
        
        //Applying the Editor background to the whole window for demo sake.
        BackGroundForeGroundUpdate?.Invoke(e);
    }


    private BlitzTheme FromBase(BlitzTheme baseTheme,ThemeName themeName)
    {
        return new BlitzTheme
        {
            TextForeground = baseTheme.TextForeground,
            WindowBackground = baseTheme.WindowBackground,
            PassiveIcon = baseTheme.PassiveIcon,
            ContentHighlightBackground = baseTheme.ContentHighlightBackground,
            ContentHighlightBorder = baseTheme.ContentHighlightBorder,
            ContentHighlightReplaceBackground = baseTheme.ContentHighlightReplaceBackground,
            ContentHighlightReplaceBorder = baseTheme.ContentHighlightReplaceBorder,
            SelectedItemBackground =baseTheme.SelectedItemBackground,
            AvaloniaThemeVariant = baseTheme.AvaloniaThemeVariant,
            ThemeName = themeName.ToString()
        };
    }
    
    public void UpdateRegistryOptions() => this.RaisePropertyChanged(nameof(TextMateRegistryOptions));

    public void UpdateTheme()
    {
        TextMateRegistryOptions = new RegistryOptions(_blitzThemeViewModel!.ThemeName);
        this.RaisePropertyChanged(nameof(ThemeViewModel));
    }
}