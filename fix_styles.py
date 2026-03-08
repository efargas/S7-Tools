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
