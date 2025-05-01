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

## Expression Substitution

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

## Token Nesting

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

## Conditional Flow

### If Statement

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

### If-Else Statement

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

## Inline Definitions

```csharp
var template = """{{identity:"me"}} hello {{identity}}.""";

var result = Template.Render(template, default);

Console.WriteLine(result); // Output: hello me.
```

```csharp
var template = """{{identity:{{x => "me"}} }} hello {{identity}}.""";

var result = Template.Render(template, default);

Console.WriteLine(result); // Output: hello me.
```

## Method Invocation

### Framework Method

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

### User-Defined Method

```csharp

var options = new TemplateOptions()
    .AddMethod("MyUpper").Expression<string,string>( input => input.ToUpper());
    .AddVariable("name", "me");

var parser = new TemplateParser( options );

var template = "hello {{x => x.MyUpper( x.name )}}.";

var result = Template.Render(template, options);

Console.WriteLine(result); // Output: hello ME.
```
{% endraw %}
