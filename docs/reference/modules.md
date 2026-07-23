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

## Why use modules?

- Keep the core language small
- Group related helpers (like Math)
- Make dependencies visible at the top of a file

## Style tips

```soloc
using Math;

// your script or functions below
fn hypotenuse(double a, double b): double {
    return sqrt(pow(a, 2) + pow(b, 2));
}

print(hypotenuse(3, 4));
```

- One `using` per line
- Prefer listing only what you need (today: Math)
- If a name isn't found, check for a missing `using`

## Related

- [Standard library](stdlib.md) — what's inside Math
- [Syntax](syntax.md) — where directives fit in a file
