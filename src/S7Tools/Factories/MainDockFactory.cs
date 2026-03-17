using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Dock.Avalonia.Controls;
using System.ComponentModel;
using System.Linq;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.ViewModels.Layout;

namespace S7Tools.Factories;

/// <summary>
/// Central dock factory that creates and manages the docking layout.
/// Extends Dock.Model.Mvvm.Factory to provide IDE-style docking with
/// tabbed documents and tool windows.
/// </summary>
public class MainDockFactory : Factory
{
    private readonly object _context;

    /// <summary>Content ViewModel for the Log Viewer tool window.</summary>
    public object? LogViewerContent { get; set; }

    /// <summary>Content ViewModel for the Settings document.</summary>
    public object? SettingsContent { get; set; }

    /// <summary>Content ViewModel for the initial/welcome document.</summary>
    public object? WelcomeContent { get; set; }

    private IDocumentDock? _mainDocumentDock;
    private IToolDock? _bottomToolDock;
    private readonly Dictionary<string, IDocument> _openDocuments = new();
    private readonly Dictionary<string, ITool> _openTools = new();

    public MainDockFactory(object context)
    {
        _context = context;
    }

    /// <summary>
    /// Ensures the document dock has a valid VisibleDockables list.
    /// Dock.Avalonia may set it to null when the last document is closed.
    /// </summary>
    private bool EnsureDocumentDock()
    {
        if (_mainDocumentDock == null) return false;

        if (_mainDocumentDock.VisibleDockables == null)
        {
            _mainDocumentDock.VisibleDockables = CreateList<IDockable>();
        }

        return true;
    }

    /// <summary>
    /// Ensures the tool dock has a valid VisibleDockables list.
    /// </summary>
    private bool EnsureToolDock()
    {
        if (_bottomToolDock == null) return false;

        if (_bottomToolDock.VisibleDockables == null)
        {
            _bottomToolDock.VisibleDockables = CreateList<IDockable>();
        }

        return true;
    }

    /// <summary>
    /// Opens a ViewModel as a tabbed document in the main document dock.
    /// If a document with the same DockId already exists, it activates it instead.
    /// </summary>
    public void OpenDocument(IDockableViewModel vm)
    {
        if (!EnsureDocumentDock()) return;

        // Check for existing open document by DockId
        if (_openDocuments.TryGetValue(vm.DockId, out var existingDoc))
        {
            if (_mainDocumentDock!.VisibleDockables?.Contains(existingDoc) == true)
            {
                _mainDocumentDock.ActiveDockable = existingDoc;
                return;
            }
            else
            {
                _openDocuments.Remove(vm.DockId);
            }
        }

        var document = new Document
        {
            Id = vm.DockId,
            Title = vm.DockTitle,
            Context = vm,
            CanClose = vm.CanClose,
            CanFloat = vm.CanFloat
        };

        // Subscribe to title changes if the VM supports property change notifications
        if (vm is INotifyPropertyChanged notifiable)
        {
            notifiable.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IDockableViewModel.DockTitle))
                {
                    document.Title = vm.DockTitle;
                }
            };
        }

        AddDockable(_mainDocumentDock!, document);
        SetActiveDockable(document);
        SetFocusedDockable(_mainDocumentDock!, document);
        _openDocuments[vm.DockId] = document;
    }

    /// <summary>
    /// Opens a ViewModel as a tool window in the bottom tool dock.
    /// If a tool with the same DockId already exists, it activates it instead.
    /// </summary>
    public void OpenTool(IDockableViewModel vm)
    {
        if (!EnsureToolDock()) return;

        if (_openTools.TryGetValue(vm.DockId, out var existingTool))
        {
            if (_bottomToolDock!.VisibleDockables?.Contains(existingTool) == true)
            {
                _bottomToolDock.ActiveDockable = existingTool;
                return;
            }
            else
            {
                _openTools.Remove(vm.DockId);
            }
        }

        var tool = new Tool
        {
            Id = vm.DockId,
            Title = vm.DockTitle,
            Context = vm,
            CanClose = vm.CanClose,
            CanFloat = vm.CanFloat
        };

        AddDockable(_bottomToolDock!, tool);
        SetActiveDockable(tool);
        SetFocusedDockable(_bottomToolDock!, tool);
        _openTools[vm.DockId] = tool;
    }

    /// <summary>
    /// Closes a document by its DockId.
    /// </summary>
    public void CloseDocument(string dockId)
    {
        if (_openDocuments.TryGetValue(dockId, out var doc))
        {
            this.CloseDockable(doc);
        }
    }

    /// <summary>
    /// Closes a tool by its DockId.
    /// </summary>
    public void CloseTool(string dockId)
    {
        if (_openTools.TryGetValue(dockId, out var tool))
        {
            this.CloseDockable(tool);
        }
    }

    /// <summary>
    /// Opens the Settings view as a document tab (or activates it if already open).
    /// </summary>
    public void RestoreSettings()
    {
        if (!EnsureDocumentDock() || SettingsContent == null) return;

        var existingSettings = _mainDocumentDock!.VisibleDockables?
            .OfType<Document>()
            .FirstOrDefault(d => d.Id == "Settings");

        if (existingSettings != null)
        {
            SetActiveDockable(existingSettings);
            SetFocusedDockable(_mainDocumentDock!, existingSettings);
            return;
        }

        var settingsDocument = new Document
        {
            Id = "Settings",
            Title = "Settings",
            Context = SettingsContent,
            CanClose = true,
            CanFloat = false
        };

        AddDockable(_mainDocumentDock!, settingsDocument);
        SetActiveDockable(settingsDocument);
        SetFocusedDockable(_mainDocumentDock!, settingsDocument);
    }

    /// <inheritdoc/>
    public override void OnDockableRemoved(IDockable? dockable)
    {
        base.OnDockableRemoved(dockable);

        if (dockable is IDocument doc && doc.Id != null)
        {
            var toRemove = _openDocuments
                .Where(kvp => kvp.Value == doc)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _openDocuments.Remove(key);
            }

            // Dispose heavy ViewModels to avoid memory leaks
            if (doc.Context is IDisposable disposableVm)
            {
                disposableVm.Dispose();
            }
        }
        else if (dockable is ITool tool && tool.Id != null)
        {
            var toRemove = _openTools
                .Where(kvp => kvp.Value == tool)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toRemove)
            {
                _openTools.Remove(key);
            }

            if (tool.Context is IDisposable disposableTool)
            {
                disposableTool.Dispose();
            }
        }
    }

    /// <inheritdoc/>
    public override IRootDock CreateLayout()
    {
        var logTool = new Tool
        {
            Id = "LogViewer",
            Title = "Output",
            Context = LogViewerContent,
            CanClose = false,
            CanFloat = true
        };

        var welcomeDocument = new Document
        {
            Id = "Welcome",
            Title = "Welcome",
            Context = WelcomeContent,
            CanClose = true,
            CanFloat = true
        };

        _mainDocumentDock = new DocumentDock
        {
            Id = "Documents",
            Title = "Documents",
            Proportion = double.NaN,
            CanCreateDocument = false,
            IsCollapsable = false,
            ActiveDockable = welcomeDocument,
            VisibleDockables = CreateList<IDockable>(welcomeDocument)
        };

        _bottomToolDock = new ToolDock
        {
            Id = "BottomPane",
            Title = "BottomPane",
            Proportion = 0.3,
            VisibleDockables = CreateList<IDockable>(logTool),
            ActiveDockable = logTool
        };

        var verticalLayout = new ProportionalDock
        {
            Id = "VerticalLayout",
            Orientation = Orientation.Vertical,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(
                _mainDocumentDock,
                new ProportionalDockSplitter(),
                _bottomToolDock
            )
        };

        var mainLayout = new ProportionalDock
        {
            Id = "MainLayout",
            Orientation = Orientation.Horizontal,
            IsCollapsable = false,
            VisibleDockables = CreateList<IDockable>(verticalLayout)
        };

        var rootDock = new RootDock
        {
            Id = "Root",
            Title = "Root",
            ActiveDockable = mainLayout,
            VisibleDockables = CreateList<IDockable>(mainLayout)
        };

        return rootDock;
    }

    /// <inheritdoc/>
    public override void InitLayout(IDockable layout)
    {
        this.ContextLocator = new Dictionary<string, Func<object?>>
        {
            [layout.Id!] = () => _context
        };

        this.HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow()
        };

        base.InitLayout(layout);
    }
}
