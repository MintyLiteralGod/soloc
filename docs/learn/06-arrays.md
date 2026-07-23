# 06 — Arrays

Goal: keep many values in one ordered list.

## Create an array

```soloc
var nums = [10, 20, 30];
var names = ["SoloC", "SoloGem"];
```

Square brackets `[ ]` hold the items, separated by commas.

## Indexing

Indexes start at **0**:

```soloc
var nums = [10, 20, 30];
print(nums[0]);   // 10
print(nums[1]);   // 20
print(nums[2]);   // 30
```

Change an item:

```soloc
nums[1] = 99;
print(nums[1]);   // 99
```

## Length

```soloc
var nums = [10, 20, 30];
print(nums.Length);   // 3
```

Valid indexes are `0` through `Length - 1`.

## Loop over an array

```soloc
var scores = [8, 9, 10];

for (var i = 0; i < scores.Length; i = i + 1) {
    print("slot", i, "=", scores[i]);
}
```

## Typed arrays (when you want them)

```soloc
int[] points = [1, 2, 3];
string[] labels = ["a", "b"];
```

## Mini challenge

Make an array of three favorite words. Print each one. Print the `.Length`.

→ Next: [Classes](07-classes.md)
