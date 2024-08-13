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

To get started with Hyperbee.Templating, refer to the [documentation](https://stillpoint-software.github.io/hyperbee.templating) for 
detailed instructions and examples. 

Install via NuGet:

```bash
dotnet add package Hyperbee.Templating
```

## Usage

### Basic Variable Substitution

You can use the `TemplateParser` to perform basic variable substitutions.

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["name"] = "me"
    }
};

var template = "hello {{name}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

### Token Nesting

Token values can contain other tokens.

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

### Inline Token Definitions

You can define tokens inline within a template. Inline tokens must be defined before they are referenced.

```csharp
var template = """{{identity:"me"}} hello {{identity}}.""";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

### Conditional Tokens

You can use conditional tokens to control the flow based on conditions.

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["condition"] = "true",
        ["name"] = "me"
    }
};

var template = "{{#if condition}}hello {{name}}.{{/if}}";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["condition"] = "false",
        ["name1"] = "me",
        ["name2"] = "you",
    }
};

var template = "hello {{#if condition}}{{name1}}{{else}}{{name2}}{{/if}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello you.
```

### Inline Token Definitions

```csharp
var template = """{{identity:"me"}} hello {{identity}}.""";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

```csharp
var template = """{{identity:{{x => "me"}} }} hello {{identity}}.""";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

### While Loop

You can use a while loop to repeat a block of text while a condition is true.

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["counter"] = 0
    }
};

var template = "{{while x => int.Parse(x.counter) < 3}}{{counter}}{{counter:{{x => int.Parse(x.counter) + 1}}}}{{/while}}";

var result = parser.Render(template);
Console.WriteLine(result); // Output: 012. 
```

### CLR Method Invocation

You can invoke CLR methods within the template.

```csharp
var parser = new TemplateParser
{
    Tokens =
    {
        ["name"] = "me"
    }
};

var template = "hello {{x => x.name.ToUpper()}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello ME.
```

### User-Defined Methods

You can define custom methods and use them within the template.

```csharp
var parser = new TemplateParser
{
    Methods =
    {
        ["MyUpper"] = args => ((string)args[0]).ToUpper()
    },
    Tokens =
    {
        ["name"] = "me"
    }
};

var template = "hello {{x => x.name.MyUpper()}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello ME.
```

## Credits

Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) for more details.
{% endraw %}