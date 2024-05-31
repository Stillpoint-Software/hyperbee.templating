# Hyperbee.Templating

A simple templating engine supporting value replacements, code expressions, token nesting, 
in-line definitions, and `if` `else` conditions.

## Features

* Supports simple syntax.
* Token values are either simple substitutions or expression results.
* The templating engine supports two kinds of method evaluations.
    * Strongly typed CLR class methods through the Roslyn compiler
    * Dynamic methods in the form of Func expressions that are runtime bound to the expression argument context.
* Token nesting
* Inline Token Definitions
    * You can define tokens, inline, within a template.

## Example

```csharp
\\ example of CLR String.ToUpper

var parser = new TemplateParser
{
    Tokens =
    {
        ["name"] = "me"
    }
};

var result = parser.Render( $"hello {{x => x.name.ToUpper()}}." );
```

```csharp
\\ example of a Func expression MyUpper

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

var result = parser.Render( $"hello {{x => x.MyUpper( x.name )}}." );
```

## Example Token Nesting
Token values can contain tokens.

```csharp
\\ example of token nesting

var parser = new TemplateParser
{
    Tokens =
    {
        ["fullname"] = "{{first}} {{last}}",
        ["first"] = "Hari",
        ["last"] = "Seldon"
    }
};

var result = parser.Render( $"hello {{fullname}}." );
```

## Example Inline Token Definitions
You can define tokens, inline, within a template. Inline tokens must be defined before they are referenced.

```csharp
{{identity:"me"}}
hello {{identity}}.
```

```csharp
{{identity:{{x=> "me"}} }}
hello {{identity}}.
```


# Build Requirements

* To build and run this project, **.NET 8 SDK** is required.
* Ensure your development tools are compatible with .NET 8.

## Building the Solution

* With .NET 8 SDK installed, you can build the solution using the standard `dotnet build` command.


# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `develop`  | [![Build status](https://github.com/Stillpoint-Software/hyperbee.templating/actions/workflows/publish.yml/badge.svg?branch=develop)](https://github.com/Stillpoint-Software/hyperbee.templating/actions/workflows/publish.yml)  |
| `main`     | [![Build status](https://github.com/Stillpoint-Software/hyperbee.templating/actions/workflows/publish.yml/badge.svg)](https://github.com/Stillpoint-Software/hyperbee.templating/actions/workflows/publish.yml)                 |


# Benchmarks
 See [Benchmarks](https://github.com/Stillpoint-Software/Hyperbee.Templating/test/Hyperbee.Templating.Benchmark/benchmark/results/Hyperbee.Templating.Benchmark.TemplateBenchmarks-report-github.md)
 
 
# Help
 See [Todo](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/docs/todo.md)

 [![Hyperbee.Templating](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/assets/hyperbee.svg?raw=true)](https://github.com/Stillpoint-Software/Hyperbee.Templating)