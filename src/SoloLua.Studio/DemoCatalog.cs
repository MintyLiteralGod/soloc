namespace SoloLua.Studio;

public sealed record SoloLuaDemo(string Id, string Title, string Blurb, string Source);

public static class SoloLuaDemoCatalog
{
    public static IReadOnlyList<SoloLuaDemo> All { get; } =
    [
        new("hello", "Hello SoloLua", "Locals, fn, interpolation.",
            """
            name = "SoloLua"
            print "Hello, {name}"

            fn add(a, b)
              return a + b

            print "2 + 40 = {add(2, 40)}"
            """),

        new("lists", "Dense lists", "No nil holes. continue works.",
            """
            nums = list 1, 2, 3
            nums.push(4)

            for i in 1..5
              if i % 2 == 0
                continue
              print i

            print "len = {nums.len()}"
            print "at0(0) = {nums.at0(0)}"
            """),

        new("class", "Class without metatable hell", "class + new + methods.",
            """
            class Counter
              fn new(start)
                self.n = start ?? 0
                return self

              fn inc()
                self.n += 1
                return self.n

            c = Counter.new(10)
            print c.inc()
            print c.inc()
            """),

        new("ops", "Operators + try/catch", "!= && || and pcall sugar.",
            """
            a = 3
            b = 0

            if a != 0 && b == 0
              print "branch ok"

            try
              if b == 0
                error("divide by zero")
              print a / b
            catch err
              print "caught: {err}"
            """),
    ];
}
