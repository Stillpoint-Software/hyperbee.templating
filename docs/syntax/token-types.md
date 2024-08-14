---
layout: default
title: Token Types
parent: Syntax
nav_order: 2
---
{% raw %}
# Token Types

Templating supports two main types of tokens:

- **Variable Tokens**: These tokens are simple variable identifiers that are replaced with their corresponding values. They are considered truthy.
- **Expression Tokens**: These tokens are more complex and can include operations or transformations. Expression tokens can evaluate conditions or invoke methods.

### Variable Token

Variable tokens are simple identifiers that are replaced with their corresponding values.

`{{ identifier }}`

- **identifier**: A simple token variable.

### Expression Token

Expression tokens are lambdas that can perform operations or transformations on data.
They are passed a token context that expose token variables and methods as readonly properties.

`{{ x => x.identifier + 1 }}`

- **x**: Represents the token context.
- **identifier**: A token variable.

## Truthy Tokens

Value Tokens are considered truthy when used in a condition. This means they evaluate to a boolean value
(`true` or `false`). A token is `Truthy`. based on whether its value is considered "truthy" or "falsy". 

- **Falsy Values**: The following values are considered falsy:
   - If the identifier does not exist
   - `null`
   - An empty string (`String.Empty`)
   - The strings `"False"`, `"No"`, `"Off"`, and `"0"` (case-insensitive)

- **Truthy Values**: Any value that is not falsy is considered truthy. This includes non-empty strings 
  and any string that does not match the falsy values listed above.

{% endraw %}