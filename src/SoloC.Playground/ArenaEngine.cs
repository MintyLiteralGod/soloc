namespace SoloC.Playground;

public sealed record ArenaBattleRequest(
    string HeroName,
    int HeroAtk,
    int HeroDef,
    int HeroLuck,
    string FoeName,
    int FoeAtk,
    int FoeDef,
    int FoeLuck);

public sealed class ArenaEngine
{
    public static ArenaBattleResult Simulate(ArenaBattleRequest request)
    {
        var hero = new Combatant(request.HeroName, request.HeroAtk, request.HeroDef, request.HeroLuck);
        var foe = new Combatant(request.FoeName, request.FoeAtk, request.FoeDef, request.FoeLuck);
        var events = new List<ArenaEvent>();
        var turn = 1;

        events.Add(new ArenaEvent("banner", $"{hero.Name} vs {foe.Name}", hero.Snapshot(), foe.Snapshot()));

        while (hero.Hp > 0 && foe.Hp > 0 && turn <= 10)
        {
            var heroHit = Math.Max(1, hero.Atk + hero.Luck - 1);
            var crit = hero.Luck >= turn;
            if (crit)
                heroHit *= 2;

            var dealt = foe.TakeHit(heroHit);
            events.Add(new ArenaEvent(
                crit ? "crit" : "hit",
                $"{hero.Name} hits {foe.Name} for {dealt}" + (crit ? " (CRIT!)" : string.Empty),
                hero.Snapshot(),
                foe.Snapshot()));

            if (foe.Hp <= 0)
            {
                events.Add(new ArenaEvent("win", $"{hero.Name} wins the bout!", hero.Snapshot(), foe.Snapshot()));
                return new ArenaBattleResult(true, hero.Name, events);
            }

            var foeHit = Math.Max(1, foe.Atk + foe.Luck / 2);
            var received = hero.TakeHit(foeHit);
            events.Add(new ArenaEvent(
                "hit",
                $"{foe.Name} hits {hero.Name} for {received}",
                hero.Snapshot(),
                foe.Snapshot()));

            if (hero.Hp <= 0)
            {
                events.Add(new ArenaEvent("lose", $"{foe.Name} wins the bout!", hero.Snapshot(), foe.Snapshot()));
                return new ArenaBattleResult(false, foe.Name, events);
            }

            turn++;
        }

        var winner = hero.Hp >= foe.Hp ? hero.Name : foe.Name;
        events.Add(new ArenaEvent("timeout", $"Time! Judges choose {winner}.", hero.Snapshot(), foe.Snapshot()));
        return new ArenaBattleResult(winner == hero.Name, winner, events);
    }

    private sealed class Combatant
    {
        public Combatant(string name, int atk, int def, int luck)
        {
            Name = name;
            Atk = atk;
            Def = def;
            Luck = luck;
            MaxHp = 80 + atk * 4 + def * 3;
            Hp = MaxHp;
        }

        public string Name { get; }
        public int Atk { get; }
        public int Def { get; }
        public int Luck { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }

        public int TakeHit(int raw)
        {
            var mitigated = Math.Max(1, raw - Def);
            if (Luck > 5)
                mitigated = Math.Max(1, mitigated - 1);
            Hp = Math.Clamp(Hp - mitigated, 0, MaxHp);
            return mitigated;
        }

        public FighterSnapshot Snapshot() => new(Name, Hp, MaxHp, Atk, Def, Luck);
    }
}

public sealed record FighterSnapshot(string Name, int Hp, int MaxHp, int Atk, int Def, int Luck);
public sealed record ArenaEvent(string Kind, string Text, FighterSnapshot Hero, FighterSnapshot Foe);
public sealed record ArenaBattleResult(bool HeroWon, string Winner, IReadOnlyList<ArenaEvent> Events);
