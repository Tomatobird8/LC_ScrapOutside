using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace LC_ScrapOutside;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class LC_ScrapOutside : BaseUnityPlugin
{
    public static LC_ScrapOutside Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }

    public static ConfigEntry<Algorithm> scrapCountAlgorithm = null!;
    public static ConfigEntry<float> chanceToSpawn = null!;
    public static ConfigEntry<bool> announceInChat = null!;

    public static ConfigEntry<bool> luckyRoll = null!;
    public static ConfigEntry<float> maxLuckyLuck = null!;
    public static ConfigEntry<float> maxLuckyChance = null!;
    public static ConfigEntry<float> luckyRollMultiplier = null!;

    public static ConfigEntry<int> staticScrapToSpawn = null!;

    public static ConfigEntry<int> randomScrapMin = null!;
    public static ConfigEntry<int> randomScrapMax = null!;

    public static ConfigEntry<float> dynamicBaseScrapAmount = null!;
    public static ConfigEntry<bool> scaleScrapByLuck = null!;
    public static ConfigEntry<bool> useInternalLuckValue = null!;
    public static ConfigEntry<float> luckMultiplier = null!;
    public static ConfigEntry<bool> scaleScrapByQuota = null!;
    public static ConfigEntry<bool> inverseQuotaBasedMultiplier = null!;
    public static ConfigEntry<int> baselineQuotaValue = null!;
    public static ConfigEntry<float> quotaValueMultiplierScalar = null!;
    public static ConfigEntry<bool> scaleScrapByMoonScrapAmount = null!;
    public static ConfigEntry<float> baselineMoonScrapAmount = null!;
    public static ConfigEntry<float> moonScrapAmountDifferenceScalar = null!;
    public static ConfigEntry<bool> scaleScrapByWeather = null!;
    public static ConfigEntry<string> weatherMultipliers = null!;
    public static ConfigEntry<bool> extraVariance = null!;
    public static ConfigEntry<float> extraVarianceMin = null!;
    public static ConfigEntry<float> extraVarianceMax = null!;
    public static ConfigEntry<int> minScrapToSpawn = null!;
    public static ConfigEntry<int> maxScrapToSpawn = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        scrapCountAlgorithm = Config.Bind("General", "Scrap Count Algorithm", Algorithm.Dynamic, "Which method to use for calculating scrap amount.");
        announceInChat = Config.Bind("General", "Announce In Chat", true, "When scrap is spawned outside, annouce the amount spawned in chat.");
        chanceToSpawn = Config.Bind("General", "Chance To Spawn", 1.0f, new ConfigDescription("Chance for outside scrap to spawn. 0 for never, 1 for always", new AcceptableValueRange<float>(0f, 1f)));
        useInternalLuckValue = Config.Bind("General", "Use Internal Luck Value", false, "Instead of calculating the current furniture luck immediately from the currently placed furniture, should the internal luck value be used? The internal value only updates each quota after quota 2.");

        luckyRoll = Config.Bind("Luck Bonus", "Lucky Bonus", true, "Enable a bonus luck check that multiplies the amount of scrap outside if it passes. Uses furniture luck to determine the chance to succeed.");
        maxLuckyLuck = Config.Bind("Luck Bonus", "Max Luck", 0.2135f, "Furniture luck max cap. Default is all vanilla furniture without signal translator. Increase if you have mods that add more furniture with luck.");
        maxLuckyChance = Config.Bind("Luck Bonus", "Success Chance", 0.2f, new ConfigDescription("Chance of success at max luck.", new AcceptableValueRange<float>(0f, 1f)));
        luckyRollMultiplier = Config.Bind("Luck Bonus", "Scrap Multiplier", 3f, "The amount of scrap outside is multiplied by this if the luck check is successful.");

        staticScrapToSpawn = Config.Bind("Algorithms - Static", "Scrap Amount", 6, "Amount of scrap to spawn outside.");

        randomScrapMin = Config.Bind("Algorithms - Random", "Minimum", 4, "Minimum amount of scrap to spawn. Inclusive. This should always be lower than Maximum.");
        randomScrapMax = Config.Bind("Algorithms - Random", "Maximum", 8, "Maximum amount of scrap to spawn. Inclusive.");

        dynamicBaseScrapAmount = Config.Bind("Algorithms - Dynamic", "Base Scrap Amount", 6f, "Base amount of scrap to spawn.");
        scaleScrapByLuck = Config.Bind("Algorithms - Dynamic", "Use Furniture Luck", true, "Should scrap amount scale by luck value?");
        luckMultiplier = Config.Bind("Algorithms - Dynamic", "Furniture Luck Multiplier", 2f, "How much is furniture luck scaled by for the multiplication?");
        scaleScrapByQuota = Config.Bind("Algorithms - Dynamic", "Use Quota Value", true, "Should scrap amount scale by quota value?");
        baselineQuotaValue = Config.Bind("Algorithms - Dynamic", "Baseline Quota Value", 1000, "The quota value baseline. Values below are decrease scrap amount, values above increase it.");
        inverseQuotaBasedMultiplier = Config.Bind("Algorithms - Dynamic", "Inverse Quota Based Multiplier", false, "Should scrap amount decrease as quota increases?");
        quotaValueMultiplierScalar = Config.Bind("Algorithms - Dynamic", "Quota Value Multiplier Scalar", 0.5f, "How effective is the multiplier? Multiplier^Scalar = Final Multiplier. 1.0 makes this not affect it. Lower values reduce the effect, higher values strengthen it.");
        scaleScrapByMoonScrapAmount = Config.Bind("Algorithms - Dynamic", "Use Moon Scrap Amount", true, "Should scrap amount be based on how many items there are on the moon on average?");
        baselineMoonScrapAmount = Config.Bind("Algorithms - Dynamic", "Baseline Moon Scrap Amount", 18f, "The baseline item count where the scrap amount is decreased if the moon average item count is lower, and increased if its higher.");
        moonScrapAmountDifferenceScalar = Config.Bind("Algorithms - Dynamic", "Moon Scrap Amount Difference Scalar", 0.5f, "How effective should the moon scrap amount multiplier be?");
        scaleScrapByWeather = Config.Bind("Algorithms - Dynamic", "Use Weather", true, "Should weathers affect the scrap amount?");
        weatherMultipliers = Config.Bind("Algorithms - Dynamic", "Weather Multipliers", "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5", "Weather Multipliers. Format: weathername:float_value,weathername:float_value");
        extraVariance = Config.Bind("Algorithms - Dynamic", "Extra Variance Multiplier", true, "Should scrap amount be multiplied by a random multiplier between the random variance min and max?");
        extraVarianceMin = Config.Bind("Algorithms - Dynamic", "Extra Variance Minimum", 0.7f, "Minimum multiplier for extra variance. Make sure this is lower than maximum variance multiplier.");
        extraVarianceMax = Config.Bind("Algorithms - Dynamic", "Extra Variance Maximum", 1.3f, "Maximum multiplier for extra variance.");
        minScrapToSpawn = Config.Bind("Algorithms - Dynamic", "Min Scrap To Spawn", 1, "Minimum amount of scrap to spawn. Make sure this is lower than maximum scrap to spawn.");
        maxScrapToSpawn = Config.Bind("Algorithms - Dynamic", "Max Scrap To Spawn", 250, "Maximum amount of scrap to spawn.");

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

    public enum Algorithm
    {
        Static,
        Random,
        Dynamic
    }
}
