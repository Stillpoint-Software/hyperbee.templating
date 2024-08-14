---
layout: default
title: Hyperbee Templating
nav_order: 1
---
{% raw %}
# Hyperbee Templating

Hyperbee Templating is a lightweight templating and variable substitution syntax engine. The library supports value replacements, 
code expressions, token nesting, in-line definitions, conditional flow, and looping. It is designed to be lightweight and fast, 
and does not rely on any external dependencies.

## Features

* Variable substitution syntax engine
* Value replacements
* Expression replacements
* Token nesting
* Conditional tokens
* Conditional flow
* Iterators
* User-defined methods

## Getting Started

Install via NuGet:

```bash
dotnet add package Hyperbee.Templating
```

## Usage

### Basic Example: Variable Substitution

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["fullname"] = "{{first}} {{last}}",
        ["first"] = "Hari",
        ["last"] = "Seldon"
    }
};

var template = "hello {{fullname}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello Hari Seldon.
```

For more examples and detailed instructions, refer to the [examples](https://stillpoint-software.github.io/hyperbee.templating/syntax/examples.html).

## Credits

Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) for more details.
{% endraw %}