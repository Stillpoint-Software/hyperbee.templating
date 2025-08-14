# Hyperbee Templating

Hyperbee Templating is a lightweight templating and variable substitution syntax engine. The library supports value replacements, 
code expressions, token nesting, in-line definitions, conditional flow, and looping. It is designed to be lightweight and fast.

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

## Basic Usage

### Variable Substitution

You can use the `TemplateParser` to perform variable substitutions.

```csharp
var template = "hello {{name}}.";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["name"] = "me"
    }
});

Console.WriteLine(result); // Output: hello me.
```

### Expression Substitution

```csharp
var template = "hello {{x => x.name.ToUpper()}}.";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["name"] = "me"
    }
});

Console.WriteLine(result); // Output: hello ME.
```

### Token Nesting

Token values can contain other tokens.

```csharp
var template = "hello {{fullname}}.";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["fullname"] = "{{first}} {{last}}",
        ["first"] = "Hari",
        ["last"] = "Seldon"
    }
});

Console.WriteLine(result); // Output: hello Hari Seldon.
```

### Conditional Tokens

You can use conditional tokens to control the flow based on conditions.

```csharp
var template = "{{#if condition}}hello {{name}}.{{/if}}";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["condition"] = "true",
        ["name"] = "me"
    }
});

Console.WriteLine(result); // Output: hello me.
```

```csharp
var template = "hello {{#if condition}}{{name1}}{{else}}{{name2}}{{/if}}.";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["condition"] = "false",
        ["name1"] = "me",
        ["name2"] = "you",
    }
});

Console.WriteLine(result); // Output: hello you.
```

### While Statement

You can use a while statement to repeat a block of text while a condition is true.

```csharp
var template = "{{while x => x.counter<int> < 3}}{{counter}}{{counter:{{x => x.counter<int> + 1}}}}{{/while}}";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["counter"] = "0"
    }
});

Console.WriteLine(result); // Output: 012. 
```

### Each Statement

```csharp
var template = "{{each n:x => x.list.Split( \",\" )}}World {{n}},{{/each}}";

var result = Template.Render(template, new()
{
    Variables = 
    { 
        ["list"] = "John,James,Sarah" 
    }
});

Console.WriteLine(result); // hello World John,World James,World Sarah,. 
```

```csharp

var template = "{{each n:x => x.Where( t => Regex.IsMatch( t.Key, \"people*\" ) ).Select( t => t.Value )}}hello {{n}}. {{/each}}";

var result = Template.Render(template, new()
{
    Variables = 
    {
        ["people[0]"] = "John",
        ["people[1]"] = "Jane",
        ["people[2]"] = "Doe"
    }
});

Console.WriteLine(result); // hello John. hello Jane. hello Doe. 
```

### Methods

You can invoke methods within token expressions.

```csharp
var options = new TemplateOptions()
    .AddVariable("name", "me")
    .AddMethod("ToUpper").Expression<string,string>( value => value.ToUpper() );

var template = "hello {{x => x.ToUpper( x.name )}}.";

var result = Template.Render(template, options);

Console.WriteLine(result); // Output: hello ME.
```

## Credits

Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) for more details.

# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `develop`  | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/pack_publish.yml/badge.svg?branch=develop)](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/pack_publish.yml)  |
| `main`     | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/pack_publish.yml/badge.svg)](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/pack_publish.yml)                 |

# Help
 See [Todo](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/docs/todo.md)

[![Hyperbee.Templating](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/assets/hyperbee.svg?raw=true)](https://github.com/Stillpoint-Software/Hyperbee.Templating)

