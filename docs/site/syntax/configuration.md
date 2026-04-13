---
layout: default
title: Configuration
parent: Syntax
nav_order: 2
---
{% raw %}
# Templating Configuration

## Configuration Syntax

Templating can be configured directly at `TemplateParser` construction or through a configuration options object.
This section outlines the various method overloads available for configuring `TemplateParser`.

### Configuration Methods Table

| Method Name              | Parameters                                  | Description                                                     
|--------------------------|---------------------------------------------|-----------------------------------------------------------------
| `AddVariable`            | `string name, string value`                 | Adds a single variable with the given name and value.              
| `AddVariables`           | `IDictionary<string, string> variables`     | Adds multiple variables from a dictionary.                         
| `AddVariables`           | `params (string, string)[] variables`       | Adds multiple variables using a collection initializer syntax.     
| `AddVariables`           | `object instance`                           | Adds multiple variables for the public properties of instance. 
| `AddMethod`              | `string name, Func<[..,] TResult> method`   | Adds a method with zero or more parameters.                     
| `SetTokenStyle`          | `TokenStyle style`                          | Sets the token style for the template parser.                   
| `SetKeyValidator`        | `Func<string, bool> validator`              | Sets a custom key validator.                                    
| `SetMaxTokenDepth`       | `int depth`                                 | Sets the maximum token depth allowed.                           
| `SetEnvironmentVar`      | `bool substituteEnvironmentVariables`       | Determines whether environment variables should be substituted. 
| `SetIgnoreMissingTokens` | `bool ignoreMissingTokens`                  | Determines whether missing tokens should be ignored.            

## Configuration

Templating can be configured directly at `TemplateParser` construction or through a configuration object.

### Simple Configuration

If you are performing simple variable substitution you can configure the `TemplateParser` directly.

```csharp
const string template = "hello {{who}}.";

var result = Template.Render(template, new()
{
    Variables =
    {
        ["who"] = "you"
    }
});

Console.WriteLine(result); // Output: hello you.
```

### Configuration Object

You can perform more complex configuration by creating an instance of `TemplateOptions` and using it to set up the `TemplateParser`.
This approach is particularly useful when you need to add methods or configure advanced settings.

#### Example: Adding Variables and Methods

```csharp
var options = new TemplateOptions()
    .AddVariables( 
        new Dictionary<string, string>
        {
            ["greeting"] = "Hello",
            ["name"] = "John"
        } 
    )
    .AddMethod("ToUpper", (string input) => input.ToUpper());

const string template = "{{greeting}} {{name.ToUpper()}}!";

var result = Template.Render(template, options);
Console.WriteLine(result); // Output: Hello JOHN!
```

#### Example: Using Collection Initializer Syntax for Tokens

```csharp
var options = new TemplateOptions()
    .AddVariables(
        new (string, string)[]
        {
            ("greeting", "Hello"),
            ("name", "John"),
            ("location", "world"),
            ("timeOfDay", "morning")
        }
    );

const string template = "{{greeting}} {{name}}, good {{timeOfDay}} from {{location}}!";

var result = Template.Render(template, options);
Console.WriteLine(result); // Output: Hello John, good morning from world!
```

#### Example: Adding Methods with Multiple Parameters

```csharp
var options = new TemplateOptions()
    .AddMethod("Format").Expression<string,string,string>((format, arg1, arg2) => string.Format(format, arg1, arg2));

const string template = "{{Format('{0} and {1}', 'Alice', 'Bob')}}";

var result = Template.Render(template, options);
Console.WriteLine(result); // Output: Alice and Bob
```

{% endraw %}
