# SoloRUST research notes

**Question:** Can we make systems programming feel like the next natural step after SoloC and SoloJS?

## Findings (v0.1)

### What beginners struggle with in Rust

1. **Ceremony before joy** — `Cargo.toml`, modules, and `Result` appear before “print a loop”
2. **Ownership vocabulary** — moves, borrows, and lifetimes are powerful but front-loaded
3. **Error surface** — excellent compiler messages, still dense for day-one learners
4. **Syntax noise** — braces + types + macros (`println!`) in the first five minutes

### What SoloRUST tries first

| Approach | Why |
|----------|-----|
| Indent blocks like SoloHTML/SoloJS | One less syntax tax while learning ideas |
| `println "Hi, {name}"` | Interpolation without teaching macros first |
| Emit **real Rust source** | Learners can graduate into `rustc` / `cargo` without a dead-end dialect |
| `var` → `let mut` | Familiar wording from SoloC |
| Explicit “experimental” banner | Honest about scope; Rust’s safety model is not diluted silently |

### Architecture choice

SoloRUST is a **transpiler**, not a competing runtime.

```text
.solorust  →  SoloRust.Compiler  →  .rs  →  rustc/cargo  →  binary
```

This keeps SoloGem honest: we teach the path into Rust’s ecosystem instead of replacing it.

### Roadmap ideas (not scheduled)

1. **Borrow coach** — annotate emitted Rust with comments explaining moves
2. **Cargo scaffold** — `solorust new hello` writes a tiny crate
3. **SoloC ↔ SoloRUST** dual examples for the same algorithm
4. **Error bridge** — map rustc errors back to SoloRUST line numbers
5. **Gradual braces mode** — optional brace syntax for learners ready to match upstream Rust

### Risks

- Oversimplifying ownership could teach false confidence — mitigate with Studio notes + docs
- Diverging too far from Rust syntax could trap learners — mitigate by emitting idiomatic Rust
- Scope creep vs SoloC — keep SoloRUST small until Studio feedback says otherwise

### Recommendation

Ship SoloRUST as **Solo5 language #5 (experimental)** with Studio + CLI + docs, gather learner feedback, then deepen ownership pedagogy rather than chasing feature parity with `rustc`.

— SoloGem research, Solo5
