using Raylib_cs;

namespace TinyRts.Gameplay;

public sealed record UnitDefinition(
    UnitType Type,
    Faction Faction,
    string DisplayName,
    int OreCost,
    float TrainTime,
    float MaxHealth,
    float Speed,
    float Radius,
    float Damage,
    float AttackRange,
    float AttackInterval,
    int VisionRange,
    bool IsWorker,
    Color BodyColor);

public static class UnitCatalog
{
    public static UnitDefinition Get(UnitType type)
    {
        return type switch
        {
            UnitType.HumanWorker => new UnitDefinition(
                type, Faction.Human, "Pioneer", 50, 4.0f, 65, 8.4f, 0.55f, 4, 1.1f, 0.85f, 7, true,
                new Color(210, 225, 245, 255)),

            UnitType.HumanVanguard => new UnitDefinition(
                type, Faction.Human, "Vanguard", 85, 5.5f, 115, 7.2f, 0.68f, 12, 5.2f, 0.9f, 8, false,
                new Color(58, 128, 220, 255)),

            UnitType.HumanRanger => new UnitDefinition(
                type, Faction.Human, "Ranger", 105, 6.2f, 82, 7.8f, 0.58f, 9, 8.0f, 1.15f, 9, false,
                new Color(104, 176, 224, 255)),

            UnitType.OrcWorker => new UnitDefinition(
                type, Faction.Orc, "Laborer", 50, 4.0f, 75, 8.0f, 0.6f, 5, 1.1f, 0.9f, 7, true,
                new Color(92, 170, 74, 255)),

            UnitType.OrcBrute => new UnitDefinition(
                type, Faction.Orc, "Brute", 90, 5.8f, 135, 6.9f, 0.74f, 15, 1.45f, 1.05f, 8, false,
                new Color(178, 70, 48, 255)),

            UnitType.OrcRaider => new UnitDefinition(
                type, Faction.Orc, "Raider", 115, 6.4f, 105, 8.3f, 0.66f, 11, 4.8f, 0.78f, 9, false,
                new Color(205, 112, 58, 255)),

            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
