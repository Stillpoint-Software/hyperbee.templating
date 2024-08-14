---
layout: default
title: Overview
parent: Syntax
nav_order: 1
---
{% raw %}
# Templating Syntax Overview

Templating provides a variety of token syntaxes for different use cases. The templating syntax supports
value and expression token replacement, conditional flow, iterators, in-line declarations, and method invocation.

## Variable and Expression Tokens

Tokens are substituted with their corresponding values directly within templates. Templating supports:

- **Variable Tokens**: These are simple variables that are replaced with their corresponding values. They are considered truthy.
- **Expression Tokens**: These are more expressive, and can include operations, method calls, and transformations.

### Variable Token

Variable tokens are simple identifiers that are replaced with their corresponding values.

`{{ identifier }}`

- **identifier**: A simple token variable.

### Expression Token

Expression tokens are runtime compiled lambdas that can perform operations or transformations on data.
They are passed a token context that provides invokable methods, and readonly token variables.

`{{ x => x.identifier + 1 }}`

- **x**: The token context.
- **identifier**: A token variable.

### Token Nesting
Tokens can contain other tokens, allowing for more complex substitutions.

`{{ {{ first }}, {{ last }} }}`

## Conditional Flow

Conditional flow allows for dynamic content rendering based on evaluated conditions. Conditions can 
be either a `Variable Token` or an `Expression Token`. The condition is evaluated to determine whether 
the content inside a conditional block should be rendered.

**Condition**: `(Variable Token | Expression Token)` is evaluated as truthy or falsy.

### If Statement

`{{ if condition }} ... {{ /if }}`

### If-Else Statement

`{{ if condition }} ... {{ else }} ... {{ /if }}`


### While Statement

The `while` statement repeats a template block while a condition is true.

`{{ while condition }} ... {{ /while }}`

## Inline Declarations

You can declare variable tokens inline within the template.

### Syntax

`{{ identifier: "value" }}`

- **identifier**: The name of the token variable defined.
- **value**: The value to assign.

{% endraw %}