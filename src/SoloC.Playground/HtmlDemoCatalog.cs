namespace SoloC.Playground;

public sealed record HtmlDemo(string Id, string Title, string Blurb, string Source);

public static class HtmlDemoCatalog
{
    public static IReadOnlyList<HtmlDemo> All { get; } =
    [
        new(
            "hello",
            "Hello SoloHTML",
            "A tiny page with hero + button.",
            """
            page Hello
              title Hello SoloHTML
              hero
                brand SoloGem
                h1 Hello, SoloHTML
                p The easiest way to make a web page.
                button primary href=#ok Let's go
            """),
        new(
            "showcase",
            "Landing Showcase",
            "Hero, cards, list, footer — full page.",
            """
            page SoloHTML Showcase
              title SoloHTML — by SoloGem
              hero
                brand SoloHTML
                h1 Write pages without angle-bracket pain
                p SoloGem's markup language. Indent, write words, get real HTML5.
                button primary href=#features Explore features
              section #features
                h2 Why SoloHTML?
                row
                  card
                    h3 Simple
                    p No nested brackets maze.
                  card
                    h3 Friendly
                    p Designed for beginners.
                  card
                    h3 Real HTML
                    p Compiles to clean HTML5.
              section
                h2 What you get
                list
                  item page / hero / section shortcuts
                  item card + row layouts
                  item buttons, lists, and links
              footer
                p Made by SoloGem · SoloHTML is open source
            """),
        new(
            "profile",
            "Profile Card",
            "A simple personal card layout.",
            """
            page Profile
              title SoloGem Profile
              section
                card center
                  brand SoloGem
                  h2 Building easy languages
                  p SoloC for code. SoloHTML for pages.
                  button secondary href=https://github.com/MintyLiteralGod/soloc GitHub
            """),
    ];
}
