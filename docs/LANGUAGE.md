# SoloC Language Guide (v0.1)

SoloC is SoloGem’s developer language. The syntax is intentionally close to **C#**, with a few SoloC-native conveniences (`fn`, `let`, `print`, top-level scripts).

File extension: `.sc`

## Quick start

```soloc
print("Hello, SoloC!");
```

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
```

## Programs

SoloC accepts three program shapes:

1. **Top-level scripts** (statements at file scope)
2. **Functions** with `fn`
3. **C#-style classes** with `static void Main()`

### Top-level script

```soloc
var name = "SoloGem";
print("hello", name);
```

### Functions

```soloc
fn add(int a, int b): int {
    return a + b;
}

print(add(2, 40));
```

### C#-style entry point

```soloc
class Program {
    static void Main() {
        Console.WriteLine("from Main");
    }
}
```

If a file has no top-level statements, SoloC looks for a `Main` function/method and runs it.

## Types

| Type     | Example        |
|----------|----------------|
| `int`    | `42`           |
| `double` | `3.14`         |
| `bool`   | `true`/`false` |
| `string` | `"text"`       |
| `void`   | (no value)     |
| class    | `new Counter()`|

`null` is supported.

## Variables

```soloc
var x = 10;        // mutable, inferred
let y = 20;        // immutable
int z = 30;        // mutable, typed
string s = "hi";
```

Reassigning a `let` binding is a runtime error.

## Control flow

```soloc
if (x > 0) {
    print("positive");
} else {
    print("non-positive");
}

while (n > 0) {
    n = n - 1;
}

for (var i = 0; i < 10; i = i + 1) {
    print(i);
}
```

## Classes

```soloc
class Counter {
    int value = 0;

    void Inc() {
        this.value = this.value + 1;
    }

    int Get() {
        return this.value;
    }
}

var c = new Counter();
c.Inc();
print(c.Get());
```

Inside methods, use `this` to read/write fields.

## Built-ins

- `print(...)` — write values separated by spaces, then a newline
- `Console.WriteLine(...)` — C#-familiar alias for printing

## Operators

Arithmetic: `+ - * / %`  
Comparison: `== != < <= > >=`  
Logical: `&& || !`  
String concatenation via `+` when either side is a string

## Comments

```soloc
// line comment
/* block comment */
```

## What’s next

Planned after v0.1:

- Static type checking before execution
- Modules / `using`
- Arrays and generics
- Bytecode VM + better diagnostics with line/column
