# 01 — Your first program

Goal: make SoloC print a message.

## Create a file

Save this as `hello.sc`:

```soloc
print("Hello from SoloC!");
```

## Run it

From the repo root:

```bash
dotnet run --project src/SoloC.Cli -- run hello.sc
```

You should see:

```text
Hello from SoloC!
```

## What happened?

`print` is a built-in that writes values to the screen, then a newline.

You can print several values — they're separated by spaces:

```soloc
print("Hi,", "SoloGem");
```

## Familiar cousin: Console

If you've seen C#, this also works:

```soloc
Console.WriteLine("Hello from SoloC!");
```

Same idea as `print`. Early lessons prefer `print` because it's shorter.

## Try it

Change the string. Add a second `print` line. Run again.

```soloc
print("Line one");
print("Line two");
```

## Common gotcha

Forgot the semicolon?

```soloc
print("oops")
```

SoloC will point you to the line and column. See [Friendly errors](../errors.md).

→ Next: [Variables](02-variables.md)
