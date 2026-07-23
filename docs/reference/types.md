# Types

SoloC has a small set of built-in types plus arrays and classes.

## Built-in types

| Type | Examples | Notes |
|------|----------|--------|
| `int` | `0`, `42`, `-3` | Whole numbers |
| `double` | `3.14`, `0.5` | Floating-point |
| `bool` | `true`, `false` | Logic |
| `string` | `"hello"` | Text in double quotes |
| `void` | — | “No value” (return type) |

`null` means no value.

## Declaring with types

```soloc
int lives = 3;
double speed = 1.5;
bool done = false;
string title = "SoloC";
```

Or let SoloC infer:

```soloc
var lives = 3;       // int
let title = "SoloC"; // string
```

## Arrays

Arrays hold a sequence of values with a fixed order.

### Literals

```soloc
var nums = [1, 2, 3];
var words = ["a", "b"];
```

### Typed arrays

```soloc
int[] nums = [1, 2, 3];
string[] words = ["a", "b"];
```

### Indexing

Indexes start at `0`:

```soloc
print(nums[0]);
nums[0] = 99;
```

### Length

```soloc
print(nums.Length);
```

Valid indexes: `0` .. `Length - 1`.

## Classes as types

A `class` name is a type you construct with `new`:

```soloc
class Point {
    int x = 0;
    int y = 0;
}

var p = new Point();
```

## Type checking

SoloC checks types so beginners catch mismatches early — for example, passing a `string` where an `int` parameter is expected. Error messages include `line:column` so you can jump straight to the spot. See [errors.md](../errors.md).

## Conversions (keep it simple)

Prefer matching types on purpose. Use clear values:

```soloc
int a = 2;
double b = 2.0;
string label = "" + a;   // string via concatenation when needed
```
