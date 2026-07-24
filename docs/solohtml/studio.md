# SoloHTML Studio

Dedicated browser GUI for SoloHTML — made by SoloGem.

## Run

```bash
dotnet run --project src/SoloHtml.Studio
```

Open **http://localhost:5089**

## What’s inside

- **Live editor** — write `.solohtml` and see HTML update as you type
- **Demo gallery** — starter pages you can load and tweak
- **Compile + download** — export a real `.html` file

## Compiler CLI

For terminal workflows, use the dedicated compiler:

```bash
dotnet run --project src/SoloHtml.Cli -- compile examples/html/showcase.solohtml
dotnet run --project src/SoloHtml.Cli -- watch examples/html/hello.solohtml
```

Shortcut once installed: `solohtml file.solohtml`

## Related

- [SoloHTML language](README.md)
- [SoloC Studio](../studio.md) — SoloC + embedded SoloHTML tab at `:5088`
