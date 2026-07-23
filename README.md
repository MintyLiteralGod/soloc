# SoloC

**SoloGem’s own language for developers** — familiar like C#, lighter to experiment with.

SoloC is a C#-inspired language with a toolchain written in C# (.NET 8). v0.1 ships a lexer, parser, AST, tree-walk interpreter, and CLI.

## Status

**v0.1 foundation** — runnable scripts, functions, classes, and a `Main` entry point.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Build

```bash
dotnet build SoloC.sln
```

## Run examples

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
dotnet run --project src/SoloC.Cli -- run examples/fibonacci.sc
dotnet run --project src/SoloC.Cli -- run examples/counter.sc
dotnet run --project src/SoloC.Cli -- run examples/main.sc
```

Or after build:

```bash
dotnet src/SoloC.Cli/bin/Debug/net8.0/soloc.dll run examples/hello.sc
```

## CLI

```text
soloc run <file.sc>     Execute a program
soloc parse <file.sc>   Parse / validate syntax
soloc repl              Interactive prompt
soloc version           Print version
```

## Hello, SoloC

```soloc
print("Hello from SoloC!");

fn greet(string name) {
    Console.WriteLine("Hey,", name);
}

greet("SoloGem");
```

## Repository layout

```text
src/SoloC.Compiler/   Lexer, parser, AST, interpreter
src/SoloC.Cli/        `soloc` command-line tool
tests/SoloC.Tests/    Unit tests
examples/             Sample .sc programs
docs/LANGUAGE.md      Language guide
```

## Learn more

See [docs/LANGUAGE.md](docs/LANGUAGE.md) for syntax, types, classes, and built-ins.

## License

All rights reserved unless otherwise noted by SoloGem.
