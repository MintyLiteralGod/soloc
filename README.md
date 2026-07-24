# SoloC

**The easiest language to learn — made by SoloGem**

SoloC is an open-source, C#-inspired programming language designed for absolute beginners. Familiar ideas, simpler rules, and friendly errors. Files use the `.sc` extension.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Made by SoloGem](https://img.shields.io/badge/Made%20by-SoloGem-0ea5e9.svg)](docs/philosophy.md)

## 60-second start

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download)
2. Clone this repo and build:

```bash
dotnet build SoloC.sln
```

3. Open **SoloC Studio** (GUI app in your browser):

```bash
dotnet run --project src/SoloC.Playground
```

Then visit [http://localhost:5088](http://localhost:5088) — Studio editor + SoloGem Arena.

Or run a script from the CLI:

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
```

## Learn SoloC

Follow the guided path — one small idea per lesson:

**[Start learning →](docs/learn/00-welcome.md)**

| Step | Lesson |
|------|--------|
| 00 | [Welcome](docs/learn/00-welcome.md) |
| 01 | [Your first program](docs/learn/01-your-first-program.md) |
| 02 | [Variables](docs/learn/02-variables.md) |
| 03 | [Decisions](docs/learn/03-decisions.md) |
| 04 | [Loops](docs/learn/04-loops.md) |
| 05 | [Functions](docs/learn/05-functions.md) |
| 06 | [Arrays](docs/learn/06-arrays.md) |
| 07 | [Classes](docs/learn/07-classes.md) |
| 08 | [Next steps](docs/learn/08-next-steps.md) |

More: [Docs hub](docs/README.md) · [Cheat sheet](docs/cheatsheet.md) · [Language overview](docs/LANGUAGE.md) · [Philosophy](docs/philosophy.md)

## Features

- **SoloC Studio** — browser GUI app: code editor, demos, and SoloGem Arena
- **Scripts** — top-level statements; no boilerplate required
- **Functions** — short `fn` definitions
- **Classes** — C#-style objects with `this` and `Main`
- **Arrays** — `[]`, indexing, and `.Length`
- **Modules** — `using` directives (e.g. `using Math;`)
- **Type checking** — catch mistakes early with clear messages
- **Bytecode VM** — an efficient runtime for curious learners ([docs/vm.md](docs/vm.md))

## Examples

```soloc
// Print
print("Hi,", "SoloGem");

// Function
fn add(int a, int b): int {
    return a + b;
}
print(add(2, 40));

// Loop
for (var i = 0; i < 3; i = i + 1) {
    print(i);
}
```

Try the repo samples:

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloC.Cli -- run examples/fibonacci.sc
dotnet run --project src/SoloC.Cli -- run examples/counter.sc
dotnet run --project src/SoloC.Cli -- run examples/main.sc
```

## CLI

```text
soloc run <file.sc>     Run a program
soloc parse <file.sc>   Check syntax
soloc repl              Interactive prompt
soloc version           Print version
```

## Open source

SoloC is **MIT** licensed — free to use, learn from, and improve. See [LICENSE](LICENSE).

Created by **SoloGem**. Contributions welcome: [CONTRIBUTING.md](CONTRIBUTING.md).

## Repository layout

```text
src/SoloC.Compiler/   Language implementation
src/SoloC.Cli/        `soloc` command-line tool
tests/                Tests
examples/             Sample .sc programs
docs/                 Learn path, reference, philosophy
```
