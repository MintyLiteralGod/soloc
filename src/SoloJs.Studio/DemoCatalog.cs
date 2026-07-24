namespace SoloJs.Studio;

public sealed record SoloJsDemo(string Id, string Title, string Blurb, string Source);

public static class SoloJsDemoCatalog
{
    public static IReadOnlyList<SoloJsDemo> All { get; } =
    [
        new("hello", "Hello SoloJS", "print, fn, and a tiny loop.",
            """
            print "Hello, SoloJS"

            fn add(a, b)
              return a + b

            print add(2, 40)

            for i in 0..3
              print i
            """),

        new("dom", "Click counter", "when ready + on + set helpers.",
            """
            score = 0

            when ready
              set "#out" text "Ready — click the button!"
              on click "#btn"
                score = score + 1
                set "#score" text score
                set "#out" text "Nice click!"
            """),

        new("branch", "If / else", "Friendly branching.",
            """
            name = "SoloGem"

            if name == "SoloGem"
              print "Welcome home"
            else
              print "Nice to meet you"

            fn grade(score)
              if score >= 90
                return "A"
              elif score >= 80
                return "B"
              else
                return "Keep going"

            print grade(95)
            print grade(72)
            """),
    ];
}
