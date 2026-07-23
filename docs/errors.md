# Friendly errors

SoloC (by SoloGem) tries to tell you **where** something went wrong and **what** to try next.

## Reading a message

A typical error looks like:

```text
error (3:12): Unexpected token '}'
  --> hello.sc:3:12
```

| Part | Meaning |
|------|---------|
| `3:12` | **Line 3**, **column 12** (character from the left) |
| `error` | Severity — fix this before the program can run |
| Message | Plain-language description of the problem |

Open your `.sc` file, go to that line, and look near that column.

### Tip

Count columns from **1** at the start of the line. Spaces count. A missing `)` or `;` often sits *just before* the reported column.

## Common mistakes

### Forgot a semicolon

```soloc
print("hi")
```

Fix:

```soloc
print("hi");
```

### Mismatched braces

```soloc
fn greet() {
    print("hey");
// missing }
```

Every `{` needs a matching `}`.

### Wrong quotes

```soloc
print('hi');   // use double quotes for strings
```

Fix:

```soloc
print("hi");
```

### Reassigning a `let`

```soloc
let x = 1;
x = 2;   // error: let bindings cannot change
```

Use `var` if the value must change:

```soloc
var x = 1;
x = 2;
```

### Name not found

```soloc
print(score);   // score was never declared
```

Declare it first:

```soloc
var score = 10;
print(score);
```

### Calling something that isn't a function

```soloc
var n = 5;
n();   // n is a number, not a function
```

### Array index out of range

```soloc
var a = [1, 2, 3];
print(a[3]);   // Length is 3 → valid indexes are 0, 1, 2
```

### Wrong number of arguments

```soloc
fn add(int a, int b): int {
    return a + b;
}
print(add(1));   // needs two arguments
```

## Still stuck?

1. Run `soloc parse yourfile.sc` to check syntax only
2. Compare with a tiny working sample from [examples/](../examples/)
3. Skim the [cheat sheet](cheatsheet.md)
4. Ask in a GitHub issue — include the full error and your `.sc` file

Mistakes are part of learning. SoloC is built so each one teaches you something.
