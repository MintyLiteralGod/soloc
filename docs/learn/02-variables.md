# 02 — Variables

Goal: store values and give them names.

## Three ways to declare

```soloc
var count = 3;         // mutable, type inferred
let title = "SoloC";   // immutable — cannot reassign
int lives = 5;         // mutable, type written out
```

| Keyword / form | Can change later? | Type |
|----------------|-------------------|------|
| `var` | Yes | Inferred from the value |
| `let` | No | Inferred |
| `int`, `string`, … | Yes | You write the type |

## Types you'll use first

```soloc
int n = 42;
double price = 9.99;
bool ready = true;
string name = "SoloGem";
```

`null` means “no value.” You'll meet it more as you grow.

## Using variables

```soloc
var greeting = "Hello";
var who = "SoloC";
print(greeting, who);

var score = 10;
score = score + 5;
print(score);
```

## `let` protects you

```soloc
let max = 100;
// max = 200;  ← error: let cannot be reassigned
```

Use `let` when a name should stay fixed. Use `var` when it should change.

## Mini challenge

1. Store your name in a `let`
2. Store a number in a `var`
3. Print both
4. Change the number and print again

→ Next: [Decisions](03-decisions.md)
