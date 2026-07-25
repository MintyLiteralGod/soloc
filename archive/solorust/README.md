# SoloRUST — archived

**Status:** archived experimental Solo5 language. Kept for history and salvage; not part of the active Solo5 set.

SoloRUST compiled indentation-friendly `.solorust` into readable Rust (`.rs`), with Cargo `new` scaffolding and borrow-coach comments.

## Replaced by

**[SoloLua](../../docs/sololua/README.md)** — Solo5 language #5. Same “ceremony off, real target on” mission, aimed at Lua’s real pain points, compiling to Lua 5.4.

## Build / run (still works)

```bash
dotnet build archive/solorust/SoloRust.Compiler
dotnet run --project archive/solorust/SoloRust.Cli -- compile archive/solorust/examples/hello.solorust
dotnet run --project archive/solorust/SoloRust.Studio   # :5099 (archived Studio)
dotnet test archive/solorust/SoloRust.Tests
```

## Layout

| Path | What |
|------|------|
| `SoloRust.Compiler/` | Parser → Rust emitter |
| `SoloRust.Cli/` | `solorust` CLI |
| `SoloRust.Studio/` | Browser Studio |
| `SoloRust.Tests/` | xUnit tests |
| `docs/` | Original language docs |
| `examples/` | Sample `.solorust` files |
