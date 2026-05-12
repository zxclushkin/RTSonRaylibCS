using Raylib_cs;

namespace TinyRts.Gameplay;

public enum Faction
{
    Human,
    Orc
}

public enum UnitType
{
    HumanWorker,
    HumanVanguard,
    HumanRanger,
    OrcWorker,
    OrcBrute,
    OrcRaider
}

public enum BuildingType
{
    HumanTownHall,
    OrcGreatHall,
    HumanBarracks,
    OrcWarHut
}

public enum ResourceType
{
    Ore
}

public sealed record FactionDefinition(
    Faction Faction,
    UnitType WorkerType,
    BuildingType MainBuildingType,
    BuildingType BasicProductionBuilding,
    string WorkerName,
    string MainBuildingName,
    string ProductionBuildingName,
    Color PrimaryColor,
    Color SecondaryColor);

public static class FactionCatalog
{
    public static FactionDefinition Get(Faction faction)
    {
        return faction switch
        {
            Faction.Human => new FactionDefinition(
                Faction.Human,
                UnitType.HumanWorker,
                BuildingType.HumanTownHall,
                BuildingType.HumanBarracks,
                "Pioneer",
                "Frontier Hall",
                "Barracks",
                new Color(58, 128, 220, 255),
                new Color(210, 225, 245, 255)),

            Faction.Orc => new FactionDefinition(
                Faction.Orc,
                UnitType.OrcWorker,
                BuildingType.OrcGreatHall,
                BuildingType.OrcWarHut,
                "Laborer",
                "Clan Hold",
                "War Hut",
                new Color(178, 70, 48, 255),
                new Color(92, 170, 74, 255)),

            _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null)
        };
    }
}
