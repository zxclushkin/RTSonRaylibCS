namespace TinyRts.Gameplay;

public sealed class Player
{
    public Player(Faction faction, int startingOre)
    {
        Faction = faction;
        Ore = startingOre;
    }

    public Faction Faction { get; }
    public int Ore { get; set; }
}
