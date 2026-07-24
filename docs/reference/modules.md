# Modules

Modules let you bring in a named toolbox. SoloC keeps this simple for learners.

## `using` directive

Put `using` near the top of a `.sc` file:

```soloc
using Math;

print(abs(-10));
print(sqrt(9));
```

After `using Math;`, you can call Math functions by name (`abs`, `min`, `max`, `sqrt`, `pow`).

## File imports (multi-file)

Import another SoloC file with a string path:

```soloc
using "lib/greet.sc";
// or: import "lib/greet.sc";

print(greet("SoloGem"));
```

Paths are relative to the current file. Imports can nest; cycles are skipped safely.

## Why use modules?

- Keep the core language small
- Group related helpers (like Math)
- Split programs across files as they grow
- Make dependencies visible at the top of a file

## Style tips

```soloc
using Math;
using "helpers.sc";

// your script or functions below
fn hypotenuse(double a, double b): double {
    return sqrt(pow(a, 2) + pow(b, 2));
}

print(hypotenuse(3, 4));
```

- One `using` / `import` per line
- Prefer listing only what you need
- If a name isn't found, check for a missing `using` or import path

## Related

- [Standard library](stdlib.md) — what's inside Math
- [Syntax](syntax.md) — where directives fit in a file
