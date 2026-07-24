# Syntax reference

SoloC syntax is C#-inspired and kept small on purpose.

## Files

- Extension: `.sc`
- Encoding: UTF-8 text
- Comments: `//` line and `/* */` block

## Program shapes

A file may mix (as supported):

1. Top-level statements (a script)
2. `fn` function declarations
3. `class` declarations
4. `using` directives (usually at the top)

### Script

```soloc
print("hi");
var x = 1;
```

### Functions

```soloc
fn add(int a, int b): int {
    return a + b;
}
```

### Class with Main

```soloc
class Program {
    static void Main() {
        Console.WriteLine("go");
    }
}
```

If there are no top-level statements, SoloC runs `Main`.

## Declarations

### Variables

```soloc
var a = 1;
let b = 2;
int c = 3;
string s = "text";
```

### Functions

```soloc
fn name(Type param, ...): ReturnType {
    // body
    return value;
}
```

Omit `: ReturnType` when you don't return a value (`void`-like).

### Classes

```soloc
class Name {
    Type field = initial;

    ReturnType Method(Type p) {
        // use this.field
    }
}
```

Create instances with `new Name(...)`.

## Statements

| Statement | Form |
|-----------|------|
| Block | `{ ... }` |
| Variable | `var` / `let` / typed declaration |
| Expression | `expr;` |
| `if` | `if (cond) stmt else stmt` |
| `while` | `while (cond) stmt` |
| `for` | `for (init; cond; incr) stmt` |
| `return` | `return;` or `return expr;` |
| Empty | `;` |

## Expressions (overview)

- Literals: numbers, strings, `true` / `false`, `null`
- Names: `identifier`
- Members: `obj.field`, `obj.Method(...)`
- Calls: `fn(...)`
- `new Type(...)`
- Arrays: `[a, b]`, `arr[i]`, `arr.Length`
- Unary: `!` `-` `+`
- Binary: arithmetic, compare, logic (see below)
- Assignment: `name = expr`, `arr[i] = expr`

## Operators

Highest ideas first in day-to-day code:

| Group | Operators |
|-------|-----------|
| Arithmetic | `+` `-` `*` `/` `%` |
| Comparison | `==` `!=` `<` `<=` `>` `>=` |
| Logical | `&&` `\|\|` `!` |
| Assignment | `=` |

`+` also concatenates when a string is involved.

## Semicolons and braces

- Statements end with `;` (blocks don't need a trailing `;` after `}`)
- Use `{ }` for method/function bodies and multi-statement control flow

See also: [types](types.md), [stdlib](stdlib.md), [modules](modules.md).
