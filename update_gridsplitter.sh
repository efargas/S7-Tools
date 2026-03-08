#!/bin/bash

# Update Styles.axaml
cat << 'STYLE' > fix_styles.py
import re

with open('src/S7Tools/Styles/Styles.axaml', 'r') as f:
    content = f.read()

# Make wider and transparent by default
old_style = """    <!-- GridSplitter Styles for VSCode-like appearance -->
    <Style Selector="GridSplitter">
        <Setter Property="Background" Value="#464647" />"""

new_style = """    <!-- GridSplitter Styles for VSCode-like appearance -->
    <Style Selector="GridSplitter">
        <Setter Property="Background" Value="Transparent" />
        <Setter Property="Width" Value="8" />
        <Setter Property="Height" Value="8" />"""

content = content.replace(old_style, new_style)

with open('src/S7Tools/Styles/Styles.axaml', 'w') as f:
    f.write(content)
STYLE
python3 fix_styles.py

# Update MainWindow.axaml
cat << 'MAIN' > fix_main.py
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
MAIN
python3 fix_main.py

# Update JobsMainContentView.axaml
cat << 'JOBS' > fix_jobs.py
import re

with open('src/S7Tools/Views/Jobs/JobsMainContentView.axaml', 'r') as f:
    content = f.read()

# Replace Job Info GridSplitter
old_splitter = """    <!-- GridSplitter for resizing -->
    <GridSplitter Grid.Column="1"
                  x:Name="JobInfoSplitter"
                  Background="#464647"
                  Width="4"
                  IsVisible="False"
                  VerticalAlignment="Stretch"
                  ResizeDirection="Columns"
                  Cursor="SizeWestEast" />"""

new_splitter = """    <!-- GridSplitter for resizing -->
    <GridSplitter Grid.Column="1"
                  x:Name="JobInfoSplitter"
                  Background="Transparent"
                  Width="8"
                  IsVisible="False"
                  VerticalAlignment="Stretch"
                  ResizeDirection="Columns"
                  Cursor="SizeWestEast" />"""

content = content.replace(old_splitter, new_splitter)

with open('src/S7Tools/Views/Jobs/JobsMainContentView.axaml', 'w') as f:
    f.write(content)
JOBS
python3 fix_jobs.py
