---
layout: default
title: Methods
parent: Syntax
nav_order: 4
---
{% raw %}
# Templating Methods

## Methods

Templating supports two types of methods:

- Framework Methods
- User-Defined Methods

The syntax for invoking **framework** methods, and **user-defined** methods is the same; the difference lies in how **user-defined** methods are declared.

### Framework Method Invocation

You can invoke framework methods within the template. 

| Syntax                                | Description                                
|---------------------------------------|---------------------------------
| `{{x => x.variable.ToUpper()}}`       | Invoke a framework method.           

### User-Defined Methods

You can define custom methods and use them within the template. User-defined methods are `Action` lambdas that are registerd with the template parser.
(see example below).

| Syntax                                | Description
|---------------------------------------|------------
| `{{x => x.CustomUpper( x.variable )}}`| Invoke a user-defined method. 

```csharp

var options = new TemplateOptions()
    .AddVariable( "name", "me" )
    .AddMethod( "CustomUpper" ).Expression<string, string>( arg => arg.ToUpper() ) ;

var template = "hello {{x => x.CustomUpper(x.name)}}.";

var result = Template.Render(template, options);
Console.WriteLine(result); // Output: hello ME.
```
{% endraw %}