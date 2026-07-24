# 05 — Functions

Goal: package steps under a name you can call again.

## Your first `fn`

```soloc
fn greet(string name) {
    print("Hey,", name);
}

greet("SoloGem");
greet("friend");
```

- `fn` starts a function
- Parameters go in `( )`
- Body goes in `{ }`

## Return a value

Use a return type after `:` and the `return` keyword:

```soloc
fn add(int a, int b): int {
    return a + b;
}

print(add(2, 40));
```

## Order

You can define functions near the top of a file and call them from top-level script code:

```soloc
fn double(int n): int {
    return n * 2;
}

var x = double(21);
print(x);
```

## Why functions help

- Give a clear name to an idea (`add`, `greet`, `fib`)
- Avoid repeating the same lines
- Test one piece of logic at a time

## Mini challenge

Write `fn square(int n): int` that returns `n * n`. Print `square(1)` through `square(5)` with a `for` loop.

Peek at [examples/fibonacci.sc](../../examples/fibonacci.sc) when you're curious.

→ Next: [Arrays](06-arrays.md)
