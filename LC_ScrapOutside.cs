using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;

namespace LC_ScrapOutside;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class LC_ScrapOutside : BaseUnityPlugin
{
    public static LC_ScrapOutside Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    internal static List<Configuration> configurations = new List<Configuration>();

    public static ConfigEntry<bool> announceInChat = null!;
    public static ConfigEntry<bool> useInternalLuckValue = null!;

    public static ConfigEntry<bool> luckyRoll = null!;
    public static ConfigEntry<float> maxLuckyLuck = null!;
    public static ConfigEntry<float> maxLuckyChance = null!;
    public static ConfigEntry<float> luckyRollMultiplier = null!;

    public static ConfigEntry<float> extraVarianceMin = null!;
    public static ConfigEntry<float> extraVarianceMax = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        announceInChat = Config.Bind("General", "Announce In Chat", true, "When scrap is spawned outside, annouce the amount spawned in chat.");
        useInternalLuckValue = Config.Bind("General", "Use Internal Luck Value", false, "Instead of calculating the current furniture luck immediately from the currently placed furniture, should the internal luck value be used? The internal value only updates each quota after quota 2.");

        luckyRoll = Config.Bind("Luck Bonus", "Lucky Bonus", true, "Enable a bonus luck check that multiplies the amount of scrap outside if it passes. Uses furniture luck to determine the chance to succeed.");
        maxLuckyLuck = Config.Bind("Luck Bonus", "Max Luck", 0.2135f, "Furniture luck max cap. Default is all vanilla furniture without signal translator. Increase if you have mods that add more furniture with luck.");
        maxLuckyChance = Config.Bind("Luck Bonus", "Success Chance", 0.2f, new ConfigDescription("Chance of success at max luck.", new AcceptableValueRange<float>(0f, 1f)));
        luckyRollMultiplier = Config.Bind("Luck Bonus", "Scrap Multiplier", 3f, "The amount of scrap outside is multiplied by this if the luck check is successful.");
        extraVarianceMin = Config.Bind("Extra Variance", "Extra Variance Minimum", 0.7f, "Minimum multiplier for extra variance. Make sure this is lower than maximum variance multiplier.");
        extraVarianceMax = Config.Bind("Extra Variance", "Extra Variance Maximum", 1.3f, "Maximum multiplier for extra variance.");

        configurations.Add(new Configuration(
            "default",
            Config.Bind("default", "Enable Config", true, "Should this configuration be used for the moon? Uses defaults if false.").Value,
            Config.Bind("default", "Scrap Count Algorithm", Algorithm.Dynamic, "Which method to use for calculating scrap amount.").Value,
            Config.Bind("default", "Chance To Spawn", 1.0f, new ConfigDescription("Chance for outside scrap to spawn. 0 for never, 1 for always", new AcceptableValueRange<float>(0f, 1f))).Value,
            Config.Bind("default", "Static Scrap Amount", 6, "Amount of scrap to spawn outside. Applies if static algorithm is selected.").Value,
            Config.Bind("default", "Minimum", 4, "Minimum amount of scrap to spawn. Inclusive. This should always be lower than Maximum. Applies if random algorithm is selected.").Value,
            Config.Bind("default", "Maximum", 8, "Maximum amount of scrap to spawn. Inclusive. Applies if random algorithm is selected.").Value,
            Config.Bind("default", "Base Scrap Amount", 6f, "Base amount of scrap to spawn. This and all other settings below apply if dynamic algorithm is selected.").Value,
            Config.Bind("default", "Use Furniture Luck", true, "Should scrap amount scale by luck value?").Value,
            Config.Bind("default", "Furniture Luck Multiplier", 2f, "How much is furniture luck scaled by for the multiplication?").Value,
            Config.Bind("default", "Use Quota Value", true, "Should scrap amount scale by quota value?").Value,
            Config.Bind("default", "Baseline Quota Value", 1000, "The quota value baseline. Values below are decrease scrap amount, values above increase it.").Value,
            Config.Bind("default", "Quota Value Multiplier Scalar", 0.5f, "How effective is the multiplier? Multiplier^Scalar = Final Multiplier. 1.0 makes this not affect it. Lower values reduce the effect, higher values strengthen it.").Value,
            Config.Bind("default", "Inverse Quota Based Multiplier", false, "Should scrap amount decrease as quota increases?").Value,
            Config.Bind("default", "Use Moon Scrap Amount", true, "Should scrap amount be based on how many items there are on the moon on average?").Value,
            Config.Bind("default", "Baseline Moon Scrap Amount", 18f, "The baseline item count where the scrap amount is decreased if the moon average item count is lower, and increased if its higher.").Value,
            Config.Bind("default", "Moon Scrap Amount Difference Scalar", 0.5f, "How effective should the moon scrap amount multiplier be?").Value,
            Config.Bind("default", "Use Weather", true, "Should weathers affect the scrap amount?").Value,
            Config.Bind("default", "Weather Multipliers", "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5", "Weather Multipliers. Format: weathername:float_value,weathername:float_value").Value,
            Config.Bind("default", "Min Scrap To Spawn", 1, "Minimum amount of scrap to spawn. Make sure this is lower than maximum scrap to spawn.").Value,
            Config.Bind("default", "Max Scrap To Spawn", 250, "Maximum amount of scrap to spawn.").Value,
            Config.Bind("default", "Apply Extra Variance", true, "Should extra variance from general settings be applied to this moon?").Value
            ));

        Patch();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

}
