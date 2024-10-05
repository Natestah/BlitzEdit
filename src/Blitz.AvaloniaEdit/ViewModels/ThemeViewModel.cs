using System;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace Blitz.AvaloniaEdit.ViewModels;

public class ThemeViewModel : ViewModelBase
{
     private readonly BlitzTheme _blitzTheme;

    public BlitzTheme Theme => _blitzTheme;
    TextMateSharp.Grammars.RegistryOptions? _options;
    Registry? _registry;
    private BlitzEditorViewModel _BlitzEditorViewModel;
    public ThemeViewModel(BlitzEditorViewModel blitzEditorViewModel, BlitzTheme blitzTheme)
    {
        _blitzTheme = blitzTheme;
        _BlitzEditorViewModel = blitzEditorViewModel;
        string themeString = Theme.ThemeName;
        if (!Enum.TryParse(themeString, out TextMateSharp.Grammars.ThemeName themeName))
        {
            themeName = ThemeName.DarkPlus;
        }
        _options = new TextMateSharp.Grammars.RegistryOptions(themeName);
        _registry = new Registry(_options);
        var theme = _registry.GetTheme();
        if (theme.GetGuiColorDictionary().TryGetValue("editor.background", out var colorHexString))
        {
            BackGroundBrush = new SolidColorBrush(Color.Parse(colorHexString));
        }
        else
        {
            BackGroundBrush = Brushes.Transparent;
        }
    }

    public IBrush BackGroundBrush { get; }

    public ThemeName ThemeName => Enum.TryParse(_blitzTheme.ThemeName, out ThemeName parsedName ) ? parsedName : ThemeName.DarkPlus;

}