# Contributing to SoloC

Thanks for helping SoloGem make SoloC the easiest language to learn. Every PR — docs, examples, compiler fixes — counts.

## Before you start

- Read the [Code of Conduct](CODE_OF_CONDUCT.md)
- Skim [docs/README.md](docs/README.md) so your words match the learning tone
- Prefer small, focused changes

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Build

```bash
dotnet build SoloC.sln
```

## Test

```bash
dotnet test
```

Or:

```bash
dotnet test SoloC.sln
```

## Run an example

```bash
dotnet run --project src/SoloC.Cli -- run examples/hello.sc
```

## What to contribute

| Area | Ideas |
|------|--------|
| Docs | Clearer lessons, more tiny examples, better error guides |
| Examples | Short `.sc` programs that teach one idea |
| Compiler / CLI | Bugs, diagnostics, friendlier messages |
| Tests | Cases that protect beginner-facing behavior |

**Do not** invent new APIs in docs unless they already exist in the language. Stick to what's documented in [docs/reference/](docs/reference/).

## Docs style

SoloC docs should feel warm and beginner-friendly:

- Short paragraphs and tiny code samples
- Prefer `print(...)` in early lessons; introduce `Console.WriteLine` as a familiar alias
- Brand SoloGem as the creator
- Use the `.sc` extension in filenames and examples
- Link forward to the next lesson instead of dumping everything at once

Markdown tips:

- One idea per heading
- Prefer fenced `soloc` code blocks for SoloC source
- Avoid jargon; if you must use a term, explain it in one sentence

## Pull request tips

1. Create a branch from the default branch
2. Keep the diff readable — one concern per PR when you can
3. Run `dotnet build SoloC.sln` and `dotnet test` before opening the PR
4. Fill out the PR template: what changed, why, and how you tested
5. For docs-only PRs, say so — reviewers will still check tone and accuracy

## Questions?

Open a GitHub Discussion or issue. Security issues go to the process in [SECURITY.md](SECURITY.md), not a public issue.
