# 07 — Classes

Goal: bundle data and behavior into one thing you can create with `new`.

## A tiny class

```soloc
class Counter {
    int value = 0;

    void Inc() {
        this.value = this.value + 1;
    }

    int Get() {
        return this.value;
    }
}

var c = new Counter();
c.Inc();
c.Inc();
print(c.Get());   // 2
```

- Fields hold data (`value`)
- Methods do work (`Inc`, `Get`)
- `this` means “this instance”
- `new Counter()` creates one object

## Methods with parameters

```soloc
class Counter {
    int value = 0;

    void Inc(int by) {
        this.value = this.value + by;
    }

    int Get() {
        return this.value;
    }
}

var c = new Counter();
c.Inc(5);
print(c.Get());
```

## C#-style `Main`

If your file has no top-level statements, SoloC looks for `Main`:

```soloc
class Program {
    static void Main() {
        Console.WriteLine("from Main");
    }
}
```

You can also write a top-level `fn Main()` in some styles — the important part is: SoloC needs a clear place to start when the file isn't a free-form script.

## When to use classes

Use a class when several values belong together and share actions — a player, a counter, a bank account.

For a quick script, stick with variables and functions. Grow into classes when it helps clarity.

Try [examples/counter.sc](../../examples/counter.sc).

→ Next: [Next steps](08-next-steps.md)
