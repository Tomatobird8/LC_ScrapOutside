using HarmonyLib;
using BepInEx.Configuration;

namespace LC_ScrapOutside.Patches;
[HarmonyPatch]
internal class ConfigurationGenerator
{
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    internal static void GenerateMoonConfigurations(StartOfRound __instance)
    {
        LC_ScrapOutside.Logger.LogDebug("Generating configurations for each moon.");
        SelectableLevel[] levels = __instance.levels;

        for (int i = 0;i < levels.Length; i++)
        {
            if (!levels[i].spawnEnemiesAndScrap) continue;
            LC_ScrapOutside.configurations.Add(new Configuration(
                levels[i].PlanetName,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Enable Config", false, "Should this configuration be used for the moon? Uses defaults if false.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Scrap Count Algorithm", Algorithm.Dynamic, "Which method to use for calculating scrap amount.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Chance To Spawn", 1.0f, new ConfigDescription("Chance for outside scrap to spawn. 0 for never, 1 for always", new AcceptableValueRange<float>(0f, 1f))).Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Static: Scrap Amount", 6, "Amount of scrap to spawn outside. Applies if static algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Random: Minimum", 4, "Minimum amount of scrap to spawn. Inclusive. This should always be lower than Maximum. Applies if random algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Random: Maximum", 8, "Maximum amount of scrap to spawn. Inclusive. Applies if random algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Base Scrap Amount", 6f, "Base amount of scrap to spawn. This and all other settings below apply if dynamic algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Use Furniture Luck", true, "Should scrap amount scale by luck value?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Furniture Luck Multiplier", 2f, "How much is furniture luck scaled by for the multiplication?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Use Quota Value", true, "Should scrap amount scale by quota value?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Baseline Quota Value", 1000, "The quota value baseline. Values below are decrease scrap amount, values above increase it.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Quota Value Multiplier Scalar", 0.5f, "How effective is the multiplier? Multiplier^Scalar = Final Multiplier. 1.0 makes this not affect it. Lower values reduce the effect, higher values strengthen it.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Inverse Quota Based Multiplier", false, "Should scrap amount decrease as quota increases?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Use Moon Scrap Amount", true, "Should scrap amount be based on how many items there are on the moon on average?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Baseline Moon Scrap Amount", 18f, "The baseline item count where the scrap amount is decreased if the moon average item count is lower, and increased if its higher.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Moon Scrap Amount Difference Scalar", 0.5f, "How effective should the moon scrap amount multiplier be?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Use Weather", true, "Should weathers affect the scrap amount?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Weather Multipliers", "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5", "Weather Multipliers. Format: weathername:float_value,weathername:float_value").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Min Scrap To Spawn", 1, "Minimum amount of scrap to spawn. Make sure this is lower than maximum scrap to spawn.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Max Scrap To Spawn", 250, "Maximum amount of scrap to spawn.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Apply Extra Variance", true, "Should extra random variance multiplier be applied to this moon?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Extra Variance Minimum", 0.7f, "Extra variance multiplier minimum.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Dynamic: Extra Variance Maximum", 1.3f, "Extra variance multiplier maximum.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Scrap Types", ScrapType.FromMoon, "What type of scrap to spawn? FromMoon is anything from the moon's scrap pool, CustomList uses the list of items defined in ScrapToSpawn.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Scrap To Spawn", "", "List of scraps to spawn outside. Define name and rarity separated with a colon. Separate each entry with a comma. Example: Large axle:20,Bell:10,Gold bar:2").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Spawn Positions", SpawnPositions.OutsideNodes, "Where to spawn scrap? OutsideNodes chooses all nodes outside, NodesNearEntrances chooses outside nodes that are near all entrances, NodesNearMain chooses outside nodes that are near Main entrance, CustomList uses the Custom Spawn Positions list.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Spawn Radius", 10f, "Radius in which to search for a spot on the navmesh to spawn an item to. Set to 0 to spawn exactly on the node/custom position.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Spawn Radius Around Entrances", 40f, "Radius in which to search for nodes around an entrance to spawn items to. Only used when the respective SpawnPositions value is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Custom Spawn Positions", "", "List of 3d positions to spawn items to. Separate X Y and Z value with a comma. Separate each entry with a semicolon. Example: 5.51,-1.52,10 ; 15.1,0.67,25.62").Value
                ));
        }
    }
}
