# SoloRUST

**Experimental systems on-ramp by SoloGem** — indent-friendly SoloRUST compiles to readable **Rust source**.

File extension: `.solorust`

> Status: **research / v0.1-experimental**. Great for learning the shape of Rust. Not a full borrow-checker teacher yet.

## Tools

| Tool | What it is |
|------|------------|
| **Compiler CLI** | `src/SoloRust.Cli` → `solorust` |
| **Studio app** | `src/SoloRust.Studio` → **http://localhost:5092** |
| **Compiler library** | `src/SoloRust.Compiler` |

## SoloRUST Studio

```bash
dotnet run --project src/SoloRust.Studio
```

Open **http://localhost:5092** — edit SoloRUST, see generated `.rs`, download it.

## Compiler CLI

```bash
dotnet run --project src/SoloRust.Cli -- new hello_solo
dotnet run --project src/SoloRust.Cli -- compile examples/rust/hello.solorust
dotnet run --project src/SoloRust.Cli -- notes
```

`solorust new` writes a Cargo crate (`Cargo.toml` + `src/main.rs`) plus `main.solorust`.

Compiled output includes **borrow coach** comments (`// coach: ...`) that explain ownership in plain language.

Then (optional, requires Rust toolchain):

```bash
rustc examples/rust/hello.rs -o hello
./hello
```

## Quick example

```solorust
fn add(a: i32, b: i32) -> i32
  return a + b

fn main()
  let name = "SoloRUST"
  println "Hello, {name}"

  let mut total = 0
  for i in 1..6
    total = total + i

  let answer = add(2, 40)
  if answer == 42
    println "Perfect"
  else
    println "Keep exploring"
```

## Features (v0.1)

- `fn` with optional typed params (`a: i32`) — untyped params default to `i32`
- `let` / `let mut` / beginner `var` (= `let mut`)
- `println "Hi, {name}"` → `println!("{}", name)`
- `print` alias
- `if` / `else`, `while`, `for x in a..b`
- `return`
- `and` / `or` → `&&` / `||`
- Research notes printed by the CLI / Studio

## Not yet (see research)

Ownership teaching mode, lifetimes, traits, generics deep dive, `unsafe`, modules/`use` graph, cargo project generation.

## Learn path

1. Read [research notes](research.md) — why SoloRUST exists
2. Open Studio and compile `hello`
3. Download `.rs` and skim it next to your SoloRUST
4. If you have Rust installed, run `rustc` on the output
5. Compare with SoloC / SoloJS — same ideas, stricter world

## Part of Solo5

SoloRUST is language **#5** in [Solo5](../solo5/README.md).
