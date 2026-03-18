using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace S7Tools.ViewModels.Pages;

/// <summary>
/// Represents a file or folder in the tree view for memory dump exploration.
/// </summary>
public partial class FileTreeItemViewModel : ViewModelBase
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private string _fullPath = string.Empty;
    public string FullPath
    {
        get => _fullPath;
        set => this.RaiseAndSetIfChanged(ref _fullPath, value);
    }

    private bool _isDirectory;
    public bool IsDirectory
    {
        get => _isDirectory;
        set => this.RaiseAndSetIfChanged(ref _isDirectory, value);
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                this.RaiseAndSetIfChanged(ref _isExpanded, value);
                if (value && IsDirectory && Children.Count == 1 && Children[0].Name == "Loading...")
                {
                    LoadChildren();
                }
            }
        }
    }

    public ObservableCollection<FileTreeItemViewModel> Children { get; } = new();

    public bool IsDummyNode { get; private set; }

    public FileTreeItemViewModel(string path, bool isDirectory)
    {
        FullPath = path;
        Name = Path.GetFileName(path);
        
        if (string.IsNullOrEmpty(Name))
        {
            Name = path; // Root drives might not have a file name
        }

        IsDirectory = isDirectory;

        if (IsDirectory)
        {
            // Add a dummy node so the expander arrow shows up
            Children.Add(CreateDummyNode("Loading..."));
        }
    }

    private FileTreeItemViewModel(string dummyName)
    {
        Name = dummyName;
        IsDirectory = false;
        IsDummyNode = true;
    }

    public static FileTreeItemViewModel CreateDummyNode(string dummyName)
    {
        return new FileTreeItemViewModel(dummyName);
    }

    private void LoadChildren()
    {
        Children.Clear();

        try
        {
            var directories = Directory.GetDirectories(FullPath)
                .Select(d => new FileTreeItemViewModel(d, true));

            var files = Directory.GetFiles(FullPath, "*.*")
                .Where(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) || 
                            f.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileTreeItemViewModel(f, false));

            foreach (var dir in directories.OrderBy(d => d.Name))
            {
                Children.Add(dir);
            }

            foreach (var file in files.OrderBy(f => f.Name))
            {
                Children.Add(file);
            }
        }
        catch (UnauthorizedAccessException)
        {
            Children.Add(CreateDummyNode("Access Denied"));
        }
        catch (Exception ex)
        {
            Children.Add(CreateDummyNode($"Error: {ex.Message}"));
        }
    }
}
