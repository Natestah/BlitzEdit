using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using Blitz.AvaloniaEdit.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using TextMateSharp.Grammars;

namespace Blitz.AvaloniaEdit.Views;

public partial class BlitzFileView : UserControl
{
    private CancellationTokenSource? _currentToken = null;
    private BlitzDocument? _currentDocument = null;

    public BlitzFileView()
    {
        InitializeComponent();
        Loaded+=OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AvaloniaTextEditor.Options.HighlightCurrentLine = true;
        if (DataContext is not BlitzEditorViewModel editorViewModel)
        {
            return;
        }

        editorViewModel.TextMateInstaller = InstallTextMate;
        editorViewModel.PopulateThemeModels();
        editorViewModel.UpdateRegistryOptions();
        editorViewModel.PropertyChanged+=EditorViewModelOnPropertyChanged;
        editorViewModel.SelectedFiles.CollectionChanged += (o, args) => UpdateViewToSelection();
    }

    private async void UpdateViewToSelection()
    {
        if (DataContext is not BlitzEditorViewModel editorViewModel)
        {
            return;
        }
        if (editorViewModel.SelectedFiles.FirstOrDefault() is BlitzDocument blitzDocument)
        {
           await AddFileToView(blitzDocument);
           
           //Todo: Scroll to specific offset, instead of caret position line
        
           await ScrollToPosition(blitzDocument.AlignViewLine, blitzDocument.AlignViewColumn);
        }
        
    }

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlitzEditorViewModel.SelectedFiles))
        {
            UpdateViewToSelection();
        }
    }

    private async Task AddFileToView(BlitzDocument file)
    {
        if (DataContext is not BlitzEditorViewModel editorViewModel) return;
        

        //It's already the active in view.
        if (_currentDocument == file) return;
        if (_currentToken != null)
        {
            await _currentToken.CancelAsync();
        }
        _currentToken = new CancellationTokenSource();
        _currentDocument = file;
        string? filePreviewText = null;

        if (file.Type == BlitzDocument.DocumentType.File)
        {
            DateTime startTime = DateTime.Now;
            do
            {
                try
                {
                    filePreviewText = await File.ReadAllTextAsync(file.FileNameOrTitle, _currentToken.Token);

                }
                catch (IOException e)
                {
                    if (_currentToken.IsCancellationRequested) return;
                    await Task.Delay(50,_currentToken.Token);
                    continue;
                }

                if (_currentToken.IsCancellationRequested) return;
                break;
            } while (DateTime.Now - startTime < TimeSpan.FromSeconds(1));
            
        }
        else
        {
            filePreviewText = "";
        }
        var language =  editorViewModel.TextMateRegistryOptions.GetLanguageByExtension(file.Extension) 
                        ?? editorViewModel.TextMateRegistryOptions.GetAvailableLanguages().FirstOrDefault();
        if (language == null)
        {
            throw new NullReferenceException();
        }
        editorViewModel.TextMateInstallation?.SetGrammar(editorViewModel.TextMateRegistryOptions.GetScopeByLanguageId(language.Id));


        AvaloniaTextEditor.Document = new TextDocument(filePreviewText) ;
    }
    private TextMate.Installation InstallTextMate(RegistryOptions options) => AvaloniaTextEditor.InstallTextMate(options);


    private async Task ScrollToPosition( int lineNumer, int column)
    {
        //Todo: I don't like this, Maybe we can work it into AvaloniaEdit itself "Load a document and center it on this line when things are finished"
        for (int i = 0; i < 2; i++)
        {
            AvaloniaTextEditor.ScrollTo(lineNumer, 1);

            try
            {
                var line = AvaloniaTextEditor.Document.GetLineByNumber(lineNumer);
                AvaloniaTextEditor.CaretOffset = line.Offset + column;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e);
                break;
            }

            try
            {
                await Task.Delay(50,_currentToken.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (_currentToken.IsCancellationRequested)
            {
                break;
            }
        }
        AvaloniaTextEditor.TextArea.TextView.Redraw();
    }
}