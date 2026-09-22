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
        SelectableLevel[] levels = __instance.levels;

        for (int i = 0;i < levels.Length; i++)
        {
            if (!levels[i].spawnEnemiesAndScrap) continue;
            LC_ScrapOutside.configurations.Add(new Configuration(
                levels[i].PlanetName,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Enable Config", true, "Should this configuration be used for the moon? Uses defaults if false.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Scrap Count Algorithm", Algorithm.Dynamic, "Which method to use for calculating scrap amount.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Chance To Spawn", 1.0f, new ConfigDescription("Chance for outside scrap to spawn. 0 for never, 1 for always", new AcceptableValueRange<float>(0f, 1f))).Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Static Scrap Amount", 6, "Amount of scrap to spawn outside. Applies if static algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Minimum", 4, "Minimum amount of scrap to spawn. Inclusive. This should always be lower than Maximum. Applies if random algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Maximum", 8, "Maximum amount of scrap to spawn. Inclusive. Applies if random algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Base Scrap Amount", 6f, "Base amount of scrap to spawn. This and all other settings below apply if dynamic algorithm is selected.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Use Furniture Luck", true, "Should scrap amount scale by luck value?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Furniture Luck Multiplier", 2f, "How much is furniture luck scaled by for the multiplication?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Use Quota Value", true, "Should scrap amount scale by quota value?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Baseline Quota Value", 1000, "The quota value baseline. Values below are decrease scrap amount, values above increase it.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Quota Value Multiplier Scalar", 0.5f, "How effective is the multiplier? Multiplier^Scalar = Final Multiplier. 1.0 makes this not affect it. Lower values reduce the effect, higher values strengthen it.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Inverse Quota Based Multiplier", false, "Should scrap amount decrease as quota increases?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Use Moon Scrap Amount", true, "Should scrap amount be based on how many items there are on the moon on average?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Baseline Moon Scrap Amount", 18f, "The baseline item count where the scrap amount is decreased if the moon average item count is lower, and increased if its higher.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Moon Scrap Amount Difference Scalar", 0.5f, "How effective should the moon scrap amount multiplier be?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Use Weather", true, "Should weathers affect the scrap amount?").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Weather Multipliers", "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5", "Weather Multipliers. Format: weathername:float_value,weathername:float_value").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Min Scrap To Spawn", 1, "Minimum amount of scrap to spawn. Make sure this is lower than maximum scrap to spawn.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Max Scrap To Spawn", 250, "Maximum amount of scrap to spawn.").Value,
                LC_ScrapOutside.Instance.Config.Bind($"Moon: {levels[i].PlanetName}", "Apply Extra Variance", true, "Should extra variance from general settings be applied to this moon?").Value
                ));
        }
    }
}
