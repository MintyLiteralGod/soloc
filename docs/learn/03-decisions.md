# 03 — Decisions

Goal: run different code depending on a condition.

## `if` and `else`

```soloc
var temperature = 22;

if (temperature > 25) {
    print("Warm");
} else {
    print("Cool or mild");
}
```

The condition goes in parentheses. The body goes in braces `{ }`.

## Only `if`

```soloc
var lives = 0;

if (lives <= 0) {
    print("Game over");
}
```

## Comparisons

| Operator | Meaning |
|----------|---------|
| `==` | Equal |
| `!=` | Not equal |
| `<` `<=` | Less / less or equal |
| `>` `>=` | Greater / greater or equal |

```soloc
var x = 10;
print(x == 10);   // true
print(x != 5);    // true
print(x > 3);     // true
```

## Logic

```soloc
var age = 16;
var hasTicket = true;

if (age >= 13 && hasTicket) {
    print("Welcome in");
}

if (age < 5 || age > 65) {
    print("Discount day");
}

if (!hasTicket) {
    print("Need a ticket");
}
```

- `&&` — both must be true
- `||` — either can be true
- `!` — flips true/false

## Mini challenge

Ask yourself a yes/no question in code (hard-code the answer in a `bool`). Print different messages for `true` and `false`.

→ Next: [Loops](04-loops.md)
