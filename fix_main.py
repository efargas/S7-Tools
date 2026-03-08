import re

with open('src/S7Tools/Views/Layout/MainWindow.axaml', 'r') as f:
    content = f.read()

# Replace Sidebar GridSplitter
old_sidebar = """                <!-- Sidebar GridSplitter -->
                <GridSplitter Grid.Column="2"
                    Width="1"
                    VerticalAlignment="Stretch"
                    ResizeDirection="Columns"
                    IsVisible="{Binding Navigation.IsSidebarVisible}"
                    Cursor="SizeWestEast"
                    ShowsPreview="False" />"""

new_sidebar = """                <!-- Sidebar GridSplitter -->
                <GridSplitter Grid.Column="2"
                    Width="8"
                    Background="Transparent"
                    VerticalAlignment="Stretch"
                    ResizeDirection="Columns"
                    IsVisible="{Binding Navigation.IsSidebarVisible}"
                    Cursor="SizeWestEast"
                    ShowsPreview="False" />"""

content = content.replace(old_sidebar, new_sidebar)

# Replace Bottom Panel GridSplitter
old_bottom = """            <!-- GridSplitter for Bottom Panel -->
            <GridSplitter Grid.Row="1"
                Height="1"
                HorizontalAlignment="Stretch"
                ResizeDirection="Rows"
                IsVisible="{Binding BottomPanel.IsExpanded}"
                Cursor="SizeNorthSouth"
                ShowsPreview="False" />"""

new_bottom = """            <!-- GridSplitter for Bottom Panel -->
            <GridSplitter Grid.Row="1"
                Height="8"
                Background="Transparent"
                HorizontalAlignment="Stretch"
                ResizeDirection="Rows"
                IsVisible="{Binding BottomPanel.IsExpanded}"
                Cursor="SizeNorthSouth"
                ShowsPreview="False" />"""

content = content.replace(old_bottom, new_bottom)

with open('src/S7Tools/Views/Layout/MainWindow.axaml', 'w') as f:
    f.write(content)
