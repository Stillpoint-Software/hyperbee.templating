---
layout: default
title: Methods
parent: Syntax
nav_order: 3
---
# Methods

Templating supports two types of methods:

- Framework Methods
- User-Defined Methods

The syntax for invoking **framework** methods, and **user-defined** methods is the same; the difference lies in how **user-defined** methods are declared.

## Framework Method Invocation

You can invoke framework methods within the template. 

| Syntax                                | Description                                
|---------------------------------------|---------------------------------
| `{{x => x.token.ToUpper()}}`          | Invoke a framework method.           

## User-Defined Methods

You can define custom methods and use them within the template. User-defined methods are `Action` lambdas that are registerd with the template parser.
(see example below).

| Syntax                                | Description
|---------------------------------------|------------
| `{{x => x.token.CustomUpper()}}`      | Invoke a user-defined method. 

```csharp
var parser = new TemplateParser
{
    Methods =
    {
        ["CustomUpper"] = args => ((string)args[0]).ToUpper()
    },
    Tokens =
    {
        ["name"] = "me"
    }
};

var template = "hello {{x => x.name.CustomUpper()}}.";

var result = parser.Render(template);
Console.WriteLine(result); // Output: hello ME.
```
