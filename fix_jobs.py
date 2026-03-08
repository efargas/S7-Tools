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
