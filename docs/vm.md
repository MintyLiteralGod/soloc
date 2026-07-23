# How SoloC runs (bytecode VM)

This page is **optional**. You can write great SoloC without knowing any of it. SoloGem includes it for curious learners.

## The short story

When you run a `.sc` file, SoloC doesn't only “read and do” each line forever in the slowest possible way. It can compile your program into **bytecode** — a compact list of simple instructions — and then a **virtual machine (VM)** executes those instructions.

```text
your code (.sc)
    → check / compile
    → bytecode (simple ops)
    → VM runs the ops
    → print / results
```

## Why bother?

| Benefit | In plain words |
|---------|----------------|
| Speed | The VM runs a tight loop of small instructions |
| Clarity | Errors can still point to your original `line:column` |
| Room to grow | Optimizations happen without changing how *you* write SoloC |

## What you write vs what the VM sees

You write:

```soloc
print(1 + 2);
```

The VM thinks in tiny steps more like: load `1`, load `2`, add, call `print`. You never have to write bytecode yourself.

## Type checking + VM

Before or while compiling, SoloC's **type checking** catches many mistakes early (wrong argument types, bad assignments). Friendly messages guide you back to the source line. See [errors.md](errors.md).

## Do I need this for lessons 00–08?

No. Finish the [learn path](learn/00-welcome.md) first. Come back here when you wonder “how does my `for` loop actually run?”

## Keep exploring

- [Language overview](LANGUAGE.md)
- [Reference](reference/README.md)
- Compiler sources under `src/SoloC.Compiler/` (for contributors)
