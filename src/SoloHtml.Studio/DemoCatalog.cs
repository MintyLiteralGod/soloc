namespace SoloHtml.Studio;

public sealed record SoloHtmlDemo(string Id, string Title, string Blurb, string Source);

public static class SoloHtmlDemoCatalog
{
    public static IReadOnlyList<SoloHtmlDemo> All { get; } =
    [
        new("hello", "Hello SoloHTML", "Tiny hero page with a button.",
            """
            page Hello
              title Hello SoloHTML
              hero
                brand SoloGem
                h1 Hello, SoloHTML
                p The easiest way to make a web page.
                button primary href=#ok Let's go
            """),
        new("showcase", "Landing Showcase", "Hero, cards, list, footer.",
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
                  item Dedicated compiler CLI
                  item SoloHTML Studio with live preview
                  item page / hero / card shortcuts
              footer
                p Made by SoloGem · SoloHTML is open source
            """),
        new("docs", "Docs Shell", "A clean documentation-style layout.",
            """
            page Docs
              title SoloHTML Docs
              nav
                brand SoloHTML
                link href=#start Start
                link href=#syntax Syntax
              section #start
                h1 Start here
                p SoloHTML compiles indentation into HTML5.
                button secondary href=#syntax See syntax
              section #syntax
                h2 Common tags
                list
                  item page — full document
                  item hero — big intro band
                  item section / row / card — layout
                  item button / list / item — UI bits
              footer
                p Compiler + Studio by SoloGem
            """),
        new("profile", "Profile Card", "Personal card layout.",
            """
            page Profile
              title SoloGem
              section
                card center
                  brand SoloGem
                  h2 Building easy languages
                  p SoloC for code. SoloHTML for pages.
                  button primary href=https://github.com/MintyLiteralGod/soloc GitHub
            """),
    ];
}
