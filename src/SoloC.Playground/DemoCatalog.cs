namespace SoloC.Playground;

public sealed record Demo(string Id, string Title, string Blurb, string Mode, string Source);

public static class DemoCatalog
{
    public static IReadOnlyList<Demo> All { get; } =
    [
        new(
            "hello",
            "Hello SoloGem",
            "Your first SoloC program — print and strings.",
            "studio",
            """
            print("Hello from SoloC Studio!");
            print("Made by SoloGem — easiest language to learn.");
            let brand = "SoloGem";
            Console.WriteLine("Built with love by", brand);
            """),
        new(
            "math",
            "Math Module",
            "Import Math and call abs, max, sqrt, pow.",
            "studio",
            """
            using Math;

            print("abs(-12) =", abs(-12));
            print("max(3, 9) =", max(3, 9));
            print("sqrt(144) =", sqrt(144));
            print("pow(2, 10) =", pow(2, 10));
            """),
        new(
            "arrays",
            "Array Lab",
            "Lists, indexing, Length, and updates.",
            "studio",
            """
            var scores = [88, 92, 77, 95, 100];
            print("count =", scores.Length);
            print("first =", scores[0]);

            var total = 0;
            for (var i = 0; i < scores.Length; i = i + 1) {
                total = total + scores[i];
            }

            scores[2] = 85;
            print("sum =", total);
            print("updated =", scores);
            """),
        new(
            "functions",
            "Functions",
            "Write reusable fn helpers with return types.",
            "studio",
            """
            fn greet(string name): string {
                return "Hey, " + name + "!";
            }

            fn fib(int n): int {
                if (n <= 1) {
                    return n;
                }
                return fib(n - 1) + fib(n - 2);
            }

            print(greet("SoloGem"));
            for (var i = 0; i < 10; i = i + 1) {
                print("fib", i, "=", fib(i));
            }
            """),
        new(
            "classes",
            "Classes & this",
            "Objects with fields, methods, and new.",
            "studio",
            """
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
            c.Inc(3);
            c.Inc(4);
            print("count =", c.Get());
            """),
        new(
            "arena-logic",
            "Arena Damage Formula",
            "The same math the visual Arena uses — tweak and re-run.",
            "studio",
            """
            using Math;

            fn damage(int atk, int def, int luck): int {
                var raw = atk + luck;
                return max(1, raw - def);
            }

            var heroAtk = 14;
            var foeDef = 6;
            var luck = 5;

            print("strike damage =", damage(heroAtk, foeDef, luck));
            print("crit damage  =", damage(heroAtk * 2, foeDef, luck));
            """),
    ];
}
