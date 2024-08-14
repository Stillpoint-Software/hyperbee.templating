---
layout: default
title: Token Types
parent: Syntax
nav_order: 2
---
{% raw %}
# Token Types

Templating supports two kinds of tokens:

- **Variable Tokens**: These are simple variables that are replaced with their corresponding values. They are considered truthy.
- **Expression Tokens**: These are more expressive, and can include operations, method calls, and transformations.

### Variable Token

Variable tokens are simple identifiers that are replaced with their corresponding values.

`{{ identifier }}`

- **identifier**: A simple token variable.

### Expression Token

Expression tokens are runtime compiled lambdas that can perform operations or transformations on data.
They are passed a token context that provides invokable methods, and readonly token variables.

`{{ x => x.identifier + 1 }}`

- **x**: The token context.
- **identifier**: A token variable.

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["choice"] = "2"
    }
};

const string template =
    """
    hello {{x => {
        return x.choice switch
        {
            "1" => "me",
            "2" => "you",
            _ => "default"
        };
    } }}.
    """;

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello you.
```

## Truthy Tokens

Variable Tokens are `Truthy`. This means they evaluate to a boolean value
(`true` or `false`). A token's `Truthy-ness` is determined according to the following rules. 

- **Falsy Values**: The following values are considered falsy:
   - If the identifier does not exist
   - `null`
   - An empty string (`String.Empty`)
   - The strings `"False"`, `"No"`, `"Off"`, and `"0"` (case-insensitive)

- **Truthy Values**: Any value that is not falsy is considered truthy. This includes non-empty strings 
  and any string that does not match the falsy values listed above.

{% endraw %}