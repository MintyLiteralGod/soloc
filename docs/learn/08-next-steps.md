# 08 — Next steps

You made it. You've seen SoloC's core ideas — scripts, variables, decisions, loops, functions, arrays, and classes. SoloGem built this path so you could finish it.

## Project ideas

Pick one and keep it small:

1. **Greeter** — ask (hard-code for now) a name and print a custom hello
2. **Counter app** — class that increments and prints the value in a loop
3. **Scoreboard** — array of scores; print the total with a `for` loop
4. **Fizz-ish** — for 1..20, print a message when a number is divisible by 3
5. **Math toys** — `using Math;` then try `abs`, `min`, `max`, `sqrt`, `pow`

```soloc
using Math;

print(abs(-7));
print(max(3, 11));
print(sqrt(25));
```

## Where to look next

| Resource | Why |
|----------|-----|
| [Cheat sheet](../cheatsheet.md) | Fast syntax lookup |
| [Friendly errors](../errors.md) | Decode `line:column` |
| [Language overview](../LANGUAGE.md) | Full map of SoloC |
| [Reference](../reference/README.md) | Precise syntax & stdlib |
| [Bytecode VM](../vm.md) | Optional deep dive |
| [examples/](../../examples/) | Working `.sc` programs |
| [Solo5](../solo5/README.md) | SoloHTML · SoloCSS · SoloJS · SoloRUST |

## Explore the rest of Solo5

```bash
dotnet run --project src/SoloHtml.Studio   # pages     :5089
dotnet run --project src/SoloCss.Studio    # styles    :5090
dotnet run --project src/SoloJs.Studio     # scripts   :5091
dotnet run --project src/SoloRust.Studio   # systems   :5092 (experimental)
```

## Run the samples

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloC.Cli -- run examples/fibonacci.sc
dotnet run --project src/SoloC.Cli -- run examples/counter.sc
dotnet run --project src/SoloC.Cli -- run examples/main.sc
```

## Help SoloC grow

SoloC is **MIT** open source. Fix a typo, add an example, or improve a lesson:

- [Contributing](../../CONTRIBUTING.md)
- [Code of Conduct](../../CODE_OF_CONDUCT.md)

Welcome to the community — keep shipping tiny programs. That's how fluency starts.
