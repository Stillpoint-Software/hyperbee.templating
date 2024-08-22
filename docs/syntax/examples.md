---
layout: default
title: Examples
parent: Syntax
nav_order: 5
---
{% raw %}
# Examples

## Variable Substitution

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

## Expression Substitution

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

## Token Nesting

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

## Conditional Flow

### If Statement

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

### If-Else Statement

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

### While Statement

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

## Inline Definitions

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

## Method Invocation

### Framework Method

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

### User-Defined Method

```csharp
var parser = new TemplateParser
{
    Methods =
    {
        ["MyUpper"] = Method.Create<string, string>( arg => arg.ToUpper() )
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
{% endraw %}
