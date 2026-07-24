# SoloC cheat sheet

One page. File extension: `.sc`. Made by SoloGem.

## Hello

```soloc
print("Hello, SoloC!");
```

## Variables

```soloc
var x = 10;          // mutable, type inferred
let name = "Gem";    // immutable
int score = 0;       // mutable, typed
string s = "hi";
bool ok = true;
double pi = 3.14;
```

## Print

```soloc
print("a", 1, true);
Console.WriteLine("same idea");
```

## Decisions

```soloc
if (x > 0) {
    print("positive");
} else {
    print("not positive");
}
```

## Loops

```soloc
while (n > 0) {
    n = n - 1;
}

for (var i = 0; i < 5; i = i + 1) {
    print(i);
}
```

## Functions

```soloc
fn add(int a, int b): int {
    return a + b;
}

print(add(2, 3));
```

## Arrays

```soloc
var nums = [10, 20, 30];
print(nums[0]);
print(nums.Length);
nums[1] = 99;
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

## Main entry

```soloc
class Program {
    static void Main() {
        Console.WriteLine("from Main");
    }
}
```

## Math module

```soloc
using Math;

print(abs(-3));
print(max(2, 9));
print(sqrt(16));
```

## Operators

| Kind | Operators |
|------|-----------|
| Arithmetic | `+ - * / %` |
| Compare | `== != < <= > >=` |
| Logic | `&& \|\| !` |

## Comments

```soloc
// line
/* block */
```

## Run

```bash
dotnet run --project src/SoloC.Cli -- run yourfile.sc
```

More: [Learn path](learn/00-welcome.md) · [Reference](reference/README.md)
