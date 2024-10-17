using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using ReactiveUI;
using MainWindowViewModel = BlitzEdit.ViewModels.MainWindowViewModel;

namespace BlitzEdit.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        OpenFileCommand = ReactiveCommand.Create( OpenFile );
        DataContext = new MainWindowViewModel();
        Loaded+=OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
    }
    

    ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

    public async void OpenFile()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open File",
            AllowMultiple = false,
        });

        if (files.Count < 1)
        {
            return;
        }

        foreach (var file in files)
        {
            OpenFile(file.Path.AbsolutePath, 1, 1);
        }
    }

    public void OpenFile(string path, int line, int column)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.EditorViewModel.GetOpenedOrCreateFile(path, false, line, column);
    }



}
