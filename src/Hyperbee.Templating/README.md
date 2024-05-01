# Hyperbee.Templating

A simple templating engine supporting value replacements, code expressions, token nesting, 
in-line definitions, and `if` `else` conditions.

## Syntax
The templating engine supports a simple syntax. 

### Token Values
Token values are either simple substitutions or expression results:

* `{{token}}`
* `{{context => context.token}}`
* `{{context => context.token.ToUpper()}}`

### Token Conditions
Token conditions are either simple truthy tokens or expression results:

* `{{if token}} _truthy_content_ {{/if}}`
* `{{if !token}} _falsy_content_ {{/if}}`
* `{{if token}} _true_content_ {{else}} _false_content_ {{/if}}`
* `{{if context => context.token == "test"}} _true_content_ {{/if}}`
* `{{if context => context.token == "test"}} _true_content_ {{else}} _false_content_ {{/if}}`

## Methods
The templating engine supports two kinds of method evaluations; strongly typed CLR class 
methods through the Roslyn compiler, and dynamic methods in the form of Func expressions
that are runtime bound to the expression argument context.

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

## Token Nesting
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

## Inline Token Definitions
You can define tokens, inline, within a template. Inline tokens must be defined before they are referenced.

```csharp
{{identity:"me"}}
hello {{identity}}.
```

```csharp
{{identity:{{x=> "me"}} }}
hello {{identity}}.
```
# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `develop`  | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/publish.yml/badge.svg?branch=develop)](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/publish.yml)  |
| `main`     | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/publish.yml/badge.svg)](https://github.com/Stillpoint-Software/Hyperbee.Templating/actions/workflows/publish.yml)                 |


[![Hyperbee.Templating](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/assets/hyperbee.svg?raw=true)](https://github.com/Stillpoint-Software/HyperbeeTemplating)
# Help
 See [Todo](https://github.com/Stillpoint-Software/Hyperbee.Templating/blob/main/docs/todo.md)