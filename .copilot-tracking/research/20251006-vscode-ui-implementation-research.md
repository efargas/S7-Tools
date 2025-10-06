# VSCode-like UI Implementation Research

**Research Date**: January 6, 2025  
**Project**: S7Tools - VSCode-like UI Implementation  
**Target Framework**: .NET 8.0 with Avalonia UI 11.3.6

## Research Summary

This research document provides comprehensive analysis for implementing a VSCode-like user interface in the S7Tools Avalonia application, incorporating architectural improvements from previous reviews.

## Current Project Analysis

### **Existing Architecture**
- **Framework**: .NET 8.0 with Avalonia UI 11.3.6
- **UI Pattern**: MVVM with ReactiveUI
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Current Layout**: FluentAvalonia NavigationView-based interface

### **Current Dependencies**
```xml
<PackageReference Include="Avalonia" Version="11.3.6" />
<PackageReference Include="Avalonia.ReactiveUI" Version="11.3.6" />
<PackageReference Include="FluentAvaloniaUI" Version="2.4.0" />
<PackageReference Include="ReactiveUI" Version="20.1.1" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.0" />
```

## VSCode UI Layout Analysis

### **Required Components**

#### 1. **Top Menu Bar**
- **File Menu**: New, Open, Save, Save As, Exit
- **Edit Menu**: Cut, Copy, Paste, Find, Replace
- **View Menu**: Activity Bar, Side Bar, Panel, Status Bar toggles
- **Key Bindings**: Ctrl+N, Ctrl+O, Ctrl+S, Ctrl+Shift+S, etc.

#### 2. **Activity Bar** (Left Side)
- **Width**: Fixed 48px
- **Background**: Dark theme (#2D2D30)
- **Icons**: 24x24px, gray when unselected, white when selected/hovered
- **Selection Indicator**: 2px wide accent rectangle on left edge
- **Behavior**: 
  - Click unselected item → expand sidebar
  - Click selected item → collapse sidebar
  - Hover → show tooltip
  - First item selected on startup

#### 3. **Primary Sidebar** (Collapsible)
- **Width**: 300px default, resizable
- **Background**: Slightly different from activity bar (#252526)
- **Content**: Dynamic based on activity bar selection
- **Behavior**: Expands/collapses based on activity bar interaction

#### 4. **Main Content Area**
- **Position**: Follows activity bar or sidebar dynamically
- **Background**: Editor background (#1E1E1E)
- **Content**: Primary application content

#### 5. **Bottom Panel** (Collapsible/Resizable)
- **Height**: 200px default, resizable
- **Background**: Same as sidebar (#252526)
- **Tab Control**: Output, Problems, Terminal, Debug Console
- **Behavior**: 
  - Collapsed → show tab headers only
  - Expanded → show full content
  - Resizable with splitter

#### 6. **Status Bar** (Bottom)
- **Height**: 22px fixed
- **Background**: Accent color (#007ACC)
- **Content**: Status information, language mode, line/column, etc.

## Avalonia UI Implementation Research

### **Layout Containers**

#### **DockPanel** (Root Container)
```xml
<DockPanel>
    <Menu DockPanel.Dock="Top" />
    <Border DockPanel.Dock="Bottom" /> <!-- Status Bar -->
    <Grid> <!-- Main content area -->
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="48" /> <!-- Activity Bar -->
            <ColumnDefinition Width="Auto" /> <!-- Sidebar -->
            <ColumnDefinition Width="*" /> <!-- Main Content -->
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" /> <!-- Bottom Panel -->
        </Grid.RowDefinitions>
    </Grid>
</DockPanel>
```

#### **Grid with Dynamic Columns**
- **Activity Bar**: Fixed 48px width
- **Sidebar**: Auto width (0 when collapsed, 300px when expanded)
- **Main Content**: Star sizing (fills remaining space)
- **Bottom Panel**: Auto height (0 when collapsed, 200px+ when expanded)

### **Key Binding Implementation**

Based on Avalonia GitHub research, key bindings should use:

```xml
<Window.KeyBindings>
    <KeyBinding Gesture="Ctrl+N" Command="{Binding NewFileCommand}" />
    <KeyBinding Gesture="Ctrl+O" Command="{Binding OpenFileCommand}" />
    <KeyBinding Gesture="Ctrl+S" Command="{Binding SaveFileCommand}" />
</Window.KeyBindings>
```

### **Menu Implementation**

```xml
<Menu>
    <MenuItem Header="_File">
        <MenuItem Header="_New" Command="{Binding NewFileCommand}" InputGesture="Ctrl+N" />
        <MenuItem Header="_Open" Command="{Binding OpenFileCommand}" InputGesture="Ctrl+O" />
        <MenuItem Header="_Save" Command="{Binding SaveFileCommand}" InputGesture="Ctrl+S" />
        <Separator />
        <MenuItem Header="E_xit" Command="{Binding ExitCommand}" InputGesture="Alt+F4" />
    </MenuItem>
    <MenuItem Header="_Edit">
        <MenuItem Header="Cu_t" Command="{Binding CutCommand}" InputGesture="Ctrl+X" />
        <MenuItem Header="_Copy" Command="{Binding CopyCommand}" InputGesture="Ctrl+C" />
        <MenuItem Header="_Paste" Command="{Binding PasteCommand}" InputGesture="Ctrl+V" />
    </MenuItem>
    <MenuItem Header="_View">
        <MenuItem Header="Toggle _Activity Bar" Command="{Binding ToggleActivityBarCommand}" />
        <MenuItem Header="Toggle _Sidebar" Command="{Binding ToggleSidebarCommand}" />
        <MenuItem Header="Toggle _Panel" Command="{Binding TogglePanelCommand}" />
    </MenuItem>
</Menu>
```

### **Activity Bar Implementation**

```xml
<StackPanel Grid.Column="0" Background="#2D2D30" Orientation="Vertical">
    <ItemsControl ItemsSource="{Binding ActivityBarItems}">
        <ItemsControl.ItemTemplate>
            <DataTemplate>
                <Button Command="{Binding SelectCommand}" 
                        Background="Transparent" 
                        BorderThickness="0"
                        Width="48" Height="48"
                        ToolTip.Tip="{Binding Tooltip}">
                    <Button.Styles>
                        <Style Selector="Button:pointerover">
                            <Setter Property="Background" Value="#3E3E42" />
                        </Style>
                        <Style Selector="Button[IsPressed=True]">
                            <Setter Property="Background" Value="#094771" />
                        </Style>
                    </Button.Styles>
                    <Grid>
                        <Rectangle Width="2" Height="48" 
                                   Fill="#007ACC" 
                                   HorizontalAlignment="Left"
                                   IsVisible="{Binding IsSelected}" />
                        <PathIcon Data="{Binding IconPath}" 
                                  Width="24" Height="24"
                                  Foreground="{Binding IconBrush}" />
                    </Grid>
                </Button>
            </DataTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</StackPanel>
```

### **Resizable Panels**

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="48" />
        <ColumnDefinition Width="{Binding SidebarWidth}" />
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>
    
    <!-- Activity Bar -->
    <StackPanel Grid.Column="0" />
    
    <!-- Sidebar -->
    <Border Grid.Column="1" IsVisible="{Binding IsSidebarVisible}" />
    
    <!-- Splitter -->
    <GridSplitter Grid.Column="2" Width="4" IsVisible="{Binding IsSidebarVisible}" />
    
    <!-- Main Content -->
    <Grid Grid.Column="3">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="{Binding PanelHeight}" />
        </Grid.RowDefinitions>
        
        <!-- Main Editor Area -->
        <ContentControl Grid.Row="0" />
        
        <!-- Panel Splitter -->
        <GridSplitter Grid.Row="1" Height="4" IsVisible="{Binding IsPanelVisible}" />
        
        <!-- Bottom Panel -->
        <TabControl Grid.Row="2" IsVisible="{Binding IsPanelVisible}" />
    </Grid>
</Grid>
```

## Architectural Improvements Integration

### **Resource Management**
```csharp
// UIStrings.resx
public static class UIStrings
{
    public static string File => GetString("File");
    public static string Edit => GetString("Edit");
    public static string View => GetString("View");
    public static string NewFile => GetString("NewFile");
    // ... other strings
}
```

### **Thread-Safe UI Operations**
```csharp
public interface IUIThreadService
{
    Task InvokeAsync(Action action);
    Task<T> InvokeAsync<T>(Func<T> func);
    bool IsUIThread { get; }
}

public class AvaloniaUIThreadService : IUIThreadService
{
    public bool IsUIThread => Dispatcher.UIThread.CheckAccess();
    
    public async Task InvokeAsync(Action action)
    {
        if (IsUIThread)
            action();
        else
            await Dispatcher.UIThread.InvokeAsync(action);
    }
}
```

### **Service-Based Architecture**
```csharp
public interface ILayoutService
{
    bool IsActivityBarVisible { get; set; }
    bool IsSidebarVisible { get; set; }
    bool IsPanelVisible { get; set; }
    GridLength SidebarWidth { get; set; }
    GridLength PanelHeight { get; set; }
    
    void ToggleActivityBar();
    void ToggleSidebar();
    void TogglePanel();
}

public interface IActivityBarService
{
    ObservableCollection<ActivityBarItemViewModel> Items { get; }
    ActivityBarItemViewModel? SelectedItem { get; set; }
    void SelectItem(string id);
}
```

## Theme and Styling Research

### **VSCode Color Scheme**
```xml
<ResourceDictionary>
    <!-- Activity Bar -->
    <SolidColorBrush x:Key="ActivityBarBackground">#2D2D30</SolidColorBrush>
    <SolidColorBrush x:Key="ActivityBarForeground">#CCCCCC</SolidColorBrush>
    <SolidColorBrush x:Key="ActivityBarInactiveForeground">#858585</SolidColorBrush>
    
    <!-- Sidebar -->
    <SolidColorBrush x:Key="SidebarBackground">#252526</SolidColorBrush>
    <SolidColorBrush x:Key="SidebarForeground">#CCCCCC</SolidColorBrush>
    
    <!-- Editor -->
    <SolidColorBrush x:Key="EditorBackground">#1E1E1E</SolidColorBrush>
    <SolidColorBrush x:Key="EditorForeground">#D4D4D4</SolidColorBrush>
    
    <!-- Panel -->
    <SolidColorBrush x:Key="PanelBackground">#252526</SolidColorBrush>
    
    <!-- Status Bar -->
    <SolidColorBrush x:Key="StatusBarBackground">#007ACC</SolidColorBrush>
    <SolidColorBrush x:Key="StatusBarForeground">#FFFFFF</SolidColorBrush>
    
    <!-- Accent -->
    <SolidColorBrush x:Key="AccentBrush">#007ACC</SolidColorBrush>
</ResourceDictionary>
```

## Performance Considerations

### **Virtualization**
- Use `VirtualizingStackPanel` for large lists in sidebar
- Implement lazy loading for sidebar content
- Use `ContentControl` with view caching for main content

### **Memory Management**
- Dispose of unused ViewModels
- Use weak event subscriptions
- Implement proper cleanup in disposable services

## Implementation Challenges

### **Dynamic Layout**
- **Challenge**: Smooth animations for sidebar collapse/expand
- **Solution**: Use `DoubleAnimation` on `GridLength` values

### **Key Binding Conflicts**
- **Challenge**: Menu vs global key bindings
- **Solution**: Use scoped key binding system with priority

### **Theme Switching**
- **Challenge**: Runtime theme changes
- **Solution**: Reactive theme service with resource dictionary swapping

## External Dependencies

### **Additional NuGet Packages**
```xml
<PackageReference Include="Avalonia.Xaml.Behaviors" Version="11.3.0.6" />
<PackageReference Include="Avalonia.Xaml.Interactivity" Version="11.3.0.6" />
```

### **Icon Resources**
- **Font Awesome**: Already included via `Projektanker.Icons.Avalonia.FontAwesome`
- **Custom Icons**: SVG path data for activity bar items

## Testing Strategy

### **Unit Tests**
- ViewModel behavior testing
- Service interaction testing
- Command execution testing

### **Integration Tests**
- Layout service integration
- Key binding functionality
- Theme switching

### **UI Tests**
- Activity bar interaction
- Sidebar collapse/expand
- Panel resizing

## Conclusion

The research indicates that implementing a VSCode-like UI in Avalonia is feasible using:

1. **DockPanel** for root layout structure
2. **Grid** with dynamic column/row definitions for resizable areas
3. **Custom UserControls** for activity bar, sidebar, and panel components
4. **Service-based architecture** for layout management
5. **Resource-based theming** for VSCode color scheme
6. **Proper key binding implementation** using Avalonia's KeyBinding system

The implementation should be done incrementally, starting with basic layout structure and progressively adding interactive behaviors and styling.