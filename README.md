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

## Basic Usage

### Variable Substitution

You can use the `TemplateParser` to perform variable substitutions.

```csharp
var parser = new TemplateParser
{
    Variables =
    {
        ["name"] = "me"
    }
};

var template = "hello {{name}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello me.
```

### Expression Substitution

```csharp
var parser = new TemplateParser
{
    Variables =
    {
        ["name"] = "me"
    }
};

var template = "hello {{x => x.name.ToUpper()}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello ME.
```

### Token Nesting

Token values can contain other tokens.

```csharp
var parser = new TemplateParser
{
    Variables =
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

### Conditional Tokens

You can use conditional tokens to control the flow based on conditions.

```csharp
var parser = new TemplateParser
{
    Variables =
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
    Variables =
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

### While Statement

You can use a while statement to repeat a block of text while a condition is true.

```csharp
var parser = new TemplateParser
{
    Variables =
    {
        ["counter"] = "0"
    }
};

var template = "{{while x => x.counter<int> < 3}}{{counter}}{{counter:{{x => x.counter<int> + 1}}}}{{/while}}";

var result = parser.Render(template);
Console.WriteLine(result); // Output: 012. 
```

### Each Statement

```csharp
var template = "{{each n:x => x.list.Split( \",\" )}}World {{n}},{{/each}}";

var parser = new TemplateParser
{
    Variables = { ["list"] = "John,James,Sarah" }
};

var result = parser.Render(template);
Console.WriteLine(result); // hello World John,World James,World Sarah,. 
```

```csharp

var template = "{{each n:x => x.Where( t => Regex.IsMatch( t.Key, \"people*\" ) ).Select( t => t.Value )}}hello {{n}}. {{/each}}";

var parser = new TemplateParser
{
    Variables = 
        {
            ["people[0]"] = "John",
            ["people[1]"] = "Jane",
            ["people[2]"] = "Doe"
        }
};

var result = parser.Render(template);
Console.WriteLine(result); // hello John. hello Jane. hello Doe. 
```

### Methods

You can invoke methods within token expressions.

```csharp
var options = new TemplateOptions()
    .AddVariable("name", "me")
    .AddMethod("ToUpper").Expression<string,string>( value => value.ToUpper() );

var parser = new TemplateParser( options );

var template = "hello {{x => x.ToUpper( x.name )}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello ME.
```

## Credits

Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) for more details.