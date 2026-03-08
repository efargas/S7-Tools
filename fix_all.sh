#!/bin/bash

# Update GridLengthToDoubleConverter.cs
sed -i 's|/// <exception cref="NotImplementedException">This converter does not support ConvertBack.</exception>||' src/S7Tools/Converters/GridLengthToDoubleConverter.cs
sed -i 's/throw new NotImplementedException(UIStrings.Exception_GridLengthToDoubleConverterNoConvertBack);/return Avalonia.Data.BindingOperations.DoNothing;/' src/S7Tools/Converters/GridLengthToDoubleConverter.cs

# Update BooleanToVisibilityConverter.cs
sed -i 's|/// <exception cref="NotImplementedException">This converter does not support ConvertBack.</exception>||' src/S7Tools/Converters/BooleanToVisibilityConverter.cs
sed -i 's/throw new NotImplementedException(UIStrings.Exception_BooleanToVisibilityConverterNoConvertBack);/return Avalonia.Data.BindingOperations.DoNothing;/' src/S7Tools/Converters/BooleanToVisibilityConverter.cs
