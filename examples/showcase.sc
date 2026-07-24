// ============================================================
//  SoloC Showcase — "SoloGem Arena"
//  A mini RPG sim that flexes almost everything SoloC can do.
//  Made by SoloGem · open source (MIT)
// ============================================================

using Math;

// --- Helpers -------------------------------------------------

fn banner(string title) {
    Console.WriteLine("");
    Console.WriteLine("========================================");
    Console.WriteLine("  " + title);
    Console.WriteLine("========================================");
}

fn clamp(int value, int lo, int hi): int {
    return max(lo, min(value, hi));
}

fn percent(int part, int whole): int {
    if (whole == 0) {
        return 0;
    }
    return (part * 100) / whole;
}

fn powerLevel(int attack, int defense, int luck): int {
    // Mix of arithmetic + Math module
    var score = attack * 3 + defense * 2 + luck;
    return abs(score) + max(1, luck);
}

// --- Domain classes -----------------------------------------

class Fighter {
    string name = "Unknown";
    int hp = 100;
    int maxHp = 100;
    int attack = 10;
    int defense = 5;
    int luck = 3;
    int xp = 0;
    int level = 1;

    void Setup(string n, int a, int d, int l) {
        this.name = n;
        this.attack = a;
        this.defense = d;
        this.luck = l;
        this.maxHp = 80 + a * 4 + d * 3;
        this.hp = this.maxHp;
        this.xp = 0;
        this.level = 1;
    }

    bool IsAlive() {
        return this.hp > 0;
    }

    int Power() {
        return powerLevel(this.attack, this.defense, this.luck);
    }

    void Heal(int amount) {
        this.hp = clamp(this.hp + amount, 0, this.maxHp);
    }

    int TakeHit(int rawDamage) {
        var mitigated = max(1, rawDamage - this.defense);
        // luck can shave a little damage
        if (this.luck > 5) {
            mitigated = max(1, mitigated - 1);
        }
        this.hp = clamp(this.hp - mitigated, 0, this.maxHp);
        return mitigated;
    }

    void GainXp(int amount) {
        this.xp = this.xp + amount;
        while (this.xp >= this.level * 20) {
            this.xp = this.xp - this.level * 20;
            this.level = this.level + 1;
            this.attack = this.attack + 2;
            this.defense = this.defense + 1;
            this.luck = this.luck + 1;
            this.maxHp = this.maxHp + 10;
            this.hp = this.maxHp;
            print("*** LEVEL UP!", this.name, "is now level", this.level, "***");
        }
    }

    void Status() {
        print(
            this.name,
            "| Lv", this.level,
            "| HP", this.hp, "/", this.maxHp,
            "(" + percent(this.hp, this.maxHp) + "%)",
            "| ATK", this.attack,
            "| DEF", this.defense,
            "| LCK", this.luck,
            "| PWR", this.Power()
        );
    }
}

class Arena {
    int round = 0;
    int heroWins = 0;
    int foeWins = 0;

    void Fight(Fighter hero, Fighter foe) {
        this.round = this.round + 1;
        banner("ROUND " + this.round + ": " + hero.name + " vs " + foe.name);

        hero.Status();
        foe.Status();
        print("");

        var turn = 1;
        while (hero.IsAlive() && foe.IsAlive() && turn <= 12) {
            // Hero strike
            var heroHit = max(1, hero.attack + hero.luck - 1);
            // critical if luck is strong this turn
            if (hero.luck >= turn) {
                heroHit = heroHit * 2;
                print("  >> CRITICAL from", hero.name + "!");
            }
            var dmgToFoe = foe.TakeHit(heroHit);
            print("  ", hero.name, "hits for", dmgToFoe, "→", foe.name, "HP", foe.hp);

            if (!foe.IsAlive()) {
                print("  ", foe.name, "is defeated!");
                this.heroWins = this.heroWins + 1;
                hero.GainXp(15 + foe.level * 5);
                hero.Heal(8);
                return;
            }

            // Foe strike
            var foeHit = max(1, foe.attack + (foe.luck / 2));
            var dmgToHero = hero.TakeHit(foeHit);
            print("  ", foe.name, "hits for", dmgToHero, "→", hero.name, "HP", hero.hp);

            if (!hero.IsAlive()) {
                print("  ", hero.name, "falls!");
                this.foeWins = this.foeWins + 1;
                return;
            }

            turn = turn + 1;
        }

        // Timeout — higher remaining HP wins
        if (hero.hp >= foe.hp) {
            print("  Time! Judges award the match to", hero.name);
            this.heroWins = this.heroWins + 1;
            hero.GainXp(10);
        } else {
            print("  Time! Judges award the match to", foe.name);
            this.foeWins = this.foeWins + 1;
        }
    }

    void Scoreboard(string heroName) {
        banner("ARENA SCOREBOARD");
        print(heroName, "wins:", this.heroWins);
        print("Foe wins:", this.foeWins);
        var total = this.heroWins + this.foeWins;
        print("Win rate:", percent(this.heroWins, total) + "%");
        print("Rounds fought:", this.round);
    }
}

// --- Tournament data (arrays) --------------------------------

var foeNames = ["Pyro Imp", "Stone Golem", "Shadow Fox", "Iron Warden", "Crystal Drake"];
var foeAtk = [8, 12, 10, 14, 16];
var foeDef = [3, 10, 4, 9, 7];
var foeLck = [6, 1, 8, 2, 5];

// --- Main adventure ------------------------------------------

banner("SOLOGEM ARENA — SoloC Showcase");
Console.WriteLine("An advanced demo of SoloC: modules, functions,");
Console.WriteLine("classes, arrays, loops, math, and drama.");
Console.WriteLine("Made by SoloGem · MIT open source");

var hero = new Fighter();
hero.Setup("Kael", 11, 6, 4);

var arena = new Arena();

print("");
print("Hero enters the arena:");
hero.Status();
print("Roster size:", foeNames.Length, "challengers");

for (var i = 0; i < foeNames.Length; i = i + 1) {
    if (hero.IsAlive()) {
        var foe = new Fighter();
        foe.Setup(foeNames[i], foeAtk[i], foeDef[i], foeLck[i]);

        // Scale later foes a bit
        if (i >= 2) {
            foe.attack = foe.attack + i;
            foe.defense = foe.defense + (i / 2);
            foe.maxHp = foe.maxHp + i * 5;
            foe.hp = foe.maxHp;
        }

        arena.Fight(hero, foe);

        // Between-round recovery scaled by luck
        if (hero.IsAlive()) {
            var potion = 5 + hero.luck + i;
            hero.Heal(potion);
            print("  (restored", potion, "HP between rounds)");
        }
    } else {
        print("The hero cannot continue...");
    }
}

arena.Scoreboard(hero.name);

banner("FINAL HERO STATE");
hero.Status();

if (hero.IsAlive() && arena.heroWins >= 3) {
    Console.WriteLine("");
    Console.WriteLine("★ LEGENDARY CLEAR — SoloGem Arena conquered! ★");
    Console.WriteLine("SoloC just ran classes, arrays, Math, fns, and loops.");
} else {
    if (hero.IsAlive()) {
        print("Solid run. Train harder and try again!");
    } else {
        print("Defeat is data. Refactor your strategy and re-run!");
    }
}

banner("Showcase complete — now go build something wild.");
