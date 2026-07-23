# 04 — Loops

Goal: repeat work without copy-pasting.

## `while`

Runs as long as the condition is true:

```soloc
var n = 3;

while (n > 0) {
    print(n);
    n = n - 1;
}

print("lift off");
```

Make sure something inside the loop moves toward `false`, or it never stops.

## `for`

Great for counting:

```soloc
for (var i = 0; i < 5; i = i + 1) {
    print(i);
}
```

Three parts inside the parentheses:

1. **Start** — `var i = 0`
2. **Keep going while** — `i < 5`
3. **After each lap** — `i = i + 1`

## Loop + decision

```soloc
for (var i = 1; i <= 10; i = i + 1) {
    if (i % 2 == 0) {
        print(i, "even");
    } else {
        print(i, "odd");
    }
}
```

`%` is remainder — handy for “every other” patterns.

## Mini challenge

Print the numbers 1 through 10. Then print only the ones greater than 5.

→ Next: [Functions](05-functions.md)
