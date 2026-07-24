# Standard library

SoloC keeps the built-in surface small so beginners aren't lost. Made by SoloGem.

## `print`

Writes values separated by spaces, then a newline.

```soloc
print("Hello");
print("score", 42, true);
```

## `input`

Reads a line from the console. Optional prompt argument.

```soloc
var name = input("Your name: ");
print("Hello,", name);
```

## `Console.WriteLine`

C#-familiar printing. Same job as `print` for learning purposes.

```soloc
Console.WriteLine("Hello");
Console.WriteLine("score", 42);
```

## `Math` module

Import Math, then call its functions:

```soloc
using Math;

print(abs(-5));      // 5
print(min(3, 9));    // 3
print(max(3, 9));    // 9
print(sqrt(16));     // 4
print(pow(2, 3));    // 8
```

| Function | Meaning |
|----------|---------|
| `abs(x)` | Absolute value |
| `min(a, b)` | Smaller of two values |
| `max(a, b)` | Larger of two values |
| `sqrt(x)` | Square root |
| `pow(base, exp)` | Exponentiation |

See [modules.md](modules.md) for how `using` works.

## Arrays (built-in behavior)

Not a separate module — arrays are language syntax:

```soloc
var a = [1, 2, 3];
print(a.Length);
print(a[0]);
```

## What isn't here (yet)

SoloC docs intentionally avoid inventing a huge API. File I/O beyond `input`/`print`, networking, and GUIs live in other Solo5 languages (SoloJS / SoloPage). Propose more via PR when it helps beginners.
