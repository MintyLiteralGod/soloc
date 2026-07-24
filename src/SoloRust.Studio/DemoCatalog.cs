namespace SoloRust.Studio;

public sealed record SoloRustDemo(string Id, string Title, string Blurb, string Source);

public static class SoloRustDemoCatalog
{
    public static IReadOnlyList<SoloRustDemo> All { get; } =
    [
        new("hello", "Hello SoloRUST", "main + println with {name}.",
            """
            fn main()
              let name = "SoloRUST"
              println "Hello, {name}"
              println "Welcome to Solo5"
            """),

        new("loop", "Ranges + mut", "for loops and mutable counters.",
            """
            fn main()
              let mut total = 0
              for i in 1..6
                total = total + i
              println "sum 1..5 = {total}"
            """),

        new("fn", "Functions", "Typed params with easy defaults.",
            """
            fn add(a: i32, b: i32) -> i32
              return a + b

            fn main()
              let answer = add(2, 40)
              println "2 + 40 = {answer}"

              if answer == 42
                println "Perfect"
              else
                println "Keep exploring"
            """),
    ];
}
