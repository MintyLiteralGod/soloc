namespace SoloCss.Studio;

public sealed record SoloCssDemo(string Id, string Title, string Blurb, string Source);

public static class SoloCssDemoCatalog
{
    public static IReadOnlyList<SoloCssDemo> All { get; } =
    [
        new("hello", "Hello theme", "Vars + a few friendly rules.",
            """
            vars
              brand #0f2a22
              accent #d8ff3e
              ink #102018
              paper #f4fff8

            body
              margin 0
              font "Segoe UI", system-ui, sans-serif
              color $ink
              background $paper
              line 1.5

            .hero
              padding 4rem
              background linear-gradient(145deg, $brand, $accent)
              color $paper

              h1
                size clamp(2.4rem, 7vw, 4rem)
                margin 0.4rem 0 0.8rem

            .button
              display inline-block
              pad 0.75rem 1.25rem
              background $accent
              color $brand
              radius 0.55rem
              no-underline
              weight 700
            """),

        new("cards", "Card grid", "Nested rules + responsive media.",
            """
            vars
              ink #122018
              mist #eef8f2
              line rgba(16,32,24,0.12)

            .row
              display grid
              columns repeat(auto-fit, minmax(220px, 1fr))
              gap 1rem
              pad 2rem

            .card
              background white
              border 1px solid $line
              radius 1rem
              pad 1.25rem
              shadow 0 12px 30px rgba(16,32,24,0.06)

              h3
                margin 0 0 0.4rem
                size 1.15rem

              p
                margin 0
                color #4d675a

            media max-width 640px
              .row
                pad 1rem
                gap 0.75rem
            """),

        new("nav", "Nav + utilities", "Shortcuts like flex, center, bold.",
            """
            vars
              brand #16352c
              accent #3dffc2

            .nav
              flex
              align center
              justify space-between
              pad 1rem 1.5rem
              background $brand
              color white

              .logo
                bold
                letter -0.03em

              a
                color white
                no-underline
                margin 0 0.65rem

            .pill
              display inline-block
              pad 0.35rem 0.75rem
              background $accent
              color #102018
              radius 999px
              size 0.85rem
              weight 700
            """),
    ];
}
