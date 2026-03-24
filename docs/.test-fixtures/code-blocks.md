---
title: "Code Blocks Test"
version: "1.0.0"
created: "2026-01-01"
last-updated: "2026-03-24"
status: "current"
tags: ["test", "code-blocks", "escape-chars"]
---

# Code Blocks Test

This document exercises escape-character and code-block edge cases that
the validation scripts must handle without false positives.

## Fenced Code Blocks — No False Links

The VB.NET snippet below calls `_random.[Next](0, count)`, which looks like a
markdown link `](0, count)` if the code fence is not properly detected.

```vbnet
Private Function GenerateIndex() As Integer
    Dim index As Integer = _random.[Next](0, _eventNames.Count)
    Return _messages(_random.[Next](0, _messages.Count))
End Function
```

A C# snippet with square-bracket indexers:

```csharp
var result = myList[index].ToString();
var nested = dict["key"][0];
int[] arr = new int[items.Count];
```

The paths below are inside a fenced block and must **not** be extracted as
file references (they are hypothetical examples, not real paths):

```text
src/Does/Not/Exist.cs
docs/imaginary/file.md
tests/NonExistent/Test.cs
```

## Inline Code — Safe Backtick Paths

These inline code paths **should** be extracted because they appear in prose:

- `src/S7Tools/Services/Profiles/StandardProfileManager.cs`

## Escaped Markdown Characters

The following use backslash escapes and must not trigger validators:

\[this is not a link\](not-a-target.md)
\`not inline code\`

## Mixed Fences

Opening fence with extra backticks should be matched correctly:

````markdown
This is a code block with four backticks.
](docs/fake-link.md)
src/Fake/File.cs
````

## Tildes as Fences

~~~python
def method(self, arg):
    return self.items[index](0, len(self.items))
~~~

## Nested Code in Lists

- Item with `src/S7Tools/Services/Tasking/ResourceCoordinator.cs` inline ref.
- Item without any code.

## Links Inside Code Spans

The text `[not a link](not-a-file.md)` written inside backticks should NOT
produce a broken-link error.
