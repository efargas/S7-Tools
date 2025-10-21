#!/usr/bin/env python3

import re

# Read the file
with open('src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml', 'r') as f:
    content = f.read()

# Find all incomplete Border elements and complete them
incomplete_border_pattern = r'<Border Classes="PropertyTable" MaxHeight="200"\s+IsVisible="{Binding (\w+)Properties\.Count, Converter={x:Static converters:ObjectConverters\.CountToVisibility}}"\s+Margin="[^"]*"\s*(?!\>)'

def complete_border(match):
    property_type = match.group(1)
    return f'''<Border Classes="PropertyTable" MaxHeight="200"
                          IsVisible="{{Binding {property_type}Properties.Count, Converter={{x:Static converters:ObjectConverters.CountToVisibility}}}}"
                          Margin="0,8,0,0">
                    <ScrollViewer HorizontalScrollBarVisibility="Disabled"
                                  VerticalScrollBarVisibility="Auto">
                      <StackPanel>
                        <!-- Header -->
                        <Border Classes="PropertyHeader">
                          <Grid>
                            <Grid.ColumnDefinitions>
                              <ColumnDefinition Width="120" />
                              <ColumnDefinition Width="*" />
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Setting" Classes="PropertyHeaderText" />
                            <TextBlock Grid.Column="1" Text="Value" Classes="PropertyHeaderText" />
                          </Grid>
                        </Border>

                        <!-- Property Rows -->
                        <ItemsControl ItemsSource="{{Binding {property_type}Properties}}">
                          <ItemsControl.ItemTemplate>
                            <DataTemplate>
                              <Border Classes="PropertyRow">
                                <Grid>
                                  <Grid.ColumnDefinitions>
                                    <ColumnDefinition Width="120" />
                                    <ColumnDefinition Width="*" />
                                  </Grid.ColumnDefinitions>
                                  <TextBlock Grid.Column="0"
                                             Text="{{Binding Label}}"
                                             Classes="PropertyName" />
                                  <TextBlock Grid.Column="1"
                                             Text="{{Binding Value}}"
                                             Classes="PropertyValue"
                                             ToolTip.Tip="{{Binding Tooltip}}" />
                                </Grid>
                              </Border>
                            </DataTemplate>
                          </ItemsControl.ItemTemplate>
                        </ItemsControl>
                      </StackPanel>
                    </ScrollViewer>
                  </Border>'''

# Replace incomplete borders
content = re.sub(incomplete_border_pattern, complete_border, content)

# Write the file back
with open('src/S7Tools/Views/Jobs/JobInfoDisplayView.axaml', 'w') as f:
    f.write(content)

print("Fixed incomplete Border elements")
