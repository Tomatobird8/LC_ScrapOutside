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

    public static List<Configuration> configurations = [];

    public static ConfigEntry<bool> announceInChat = null!;
    public static ConfigEntry<bool> useInternalLuckValue = null!;

    public static ConfigEntry<bool> luckyRoll = null!;
    public static ConfigEntry<float> maxLuckyLuck = null!;
    public static ConfigEntry<float> maxLuckyChance = null!;
    public static ConfigEntry<float> luckyRollMultiplier = null!;

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

        configurations.Add(new Configuration(
            "default",
            true,
            Config.Bind("default", "Scrap Count Algorithm", Algorithm.Dynamic, "Which method to use for calculating scrap amount.").Value,
            Config.Bind("default", "Chance To Spawn", 1.0f, new ConfigDescription("Chance for outside scrap to spawn. 0 for never, 1 for always", new AcceptableValueRange<float>(0f, 1f))).Value,
            Config.Bind("default", "Static: Scrap Amount", 6, "Amount of scrap to spawn outside. Applies if static algorithm is selected.").Value,
            Config.Bind("default", "Random: Minimum", 4, "Minimum amount of scrap to spawn. Inclusive. This should always be lower than Maximum. Applies if random algorithm is selected.").Value,
            Config.Bind("default", "Random: Maximum", 8, "Maximum amount of scrap to spawn. Inclusive. Applies if random algorithm is selected.").Value,
            Config.Bind("default", "Dynamic: Base Scrap Amount", 6f, "Base amount of scrap to spawn. This and all other settings below apply if dynamic algorithm is selected.").Value,
            Config.Bind("default", "Dynamic: Use Furniture Luck", true, "Should scrap amount scale by luck value?").Value,
            Config.Bind("default", "Dynamic: Furniture Luck Multiplier", 2f, "How much is furniture luck scaled by for the multiplication?").Value,
            Config.Bind("default", "Dynamic: Use Quota Value", true, "Should scrap amount scale by quota value?").Value,
            Config.Bind("default", "Dynamic: Baseline Quota Value", 1000, "The quota value baseline. Values below are decrease scrap amount, values above increase it.").Value,
            Config.Bind("default", "Dynamic: Quota Value Multiplier Scalar", 0.5f, "How effective is the multiplier? Multiplier^Scalar = Final Multiplier. 1.0 makes this not affect it. Lower values reduce the effect, higher values strengthen it.").Value,
            Config.Bind("default", "Dynamic: Inverse Quota Based Multiplier", false, "Should scrap amount decrease as quota increases?").Value,
            Config.Bind("default", "Dynamic: Use Moon Scrap Amount", true, "Should scrap amount be based on how many items there are on the moon on average?").Value,
            Config.Bind("default", "Dynamic: Baseline Moon Scrap Amount", 18f, "The baseline item count where the scrap amount is decreased if the moon average item count is lower, and increased if its higher.").Value,
            Config.Bind("default", "Dynamic: Moon Scrap Amount Difference Scalar", 0.5f, "How effective should the moon scrap amount multiplier be?").Value,
            Config.Bind("default", "Dynamic: Use Weather", true, "Should weathers affect the scrap amount?").Value,
            Config.Bind("default", "Dynamic: Weather Multipliers", "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5", "Weather Multipliers. Format: weathername:float_value,weathername:float_value").Value,
            Config.Bind("default", "Dynamic: Min Scrap To Spawn", 1, "Minimum amount of scrap to spawn. Make sure this is lower than maximum scrap to spawn.").Value,
            Config.Bind("default", "Dynamic: Max Scrap To Spawn", 250, "Maximum amount of scrap to spawn.").Value,
            Config.Bind("default", "Dynamic: Apply Extra Variance", true, "Should extra random variance multiplier be applied to this moon?").Value,
            Config.Bind("default", "Dynamic: Extra Variance Minimum", 0.7f, "Extra variance multiplier minimum.").Value,
            Config.Bind("default", "Dynamic: Extra Variance Maximum", 1.3f, "Extra variance multiplier maximum.").Value,
            Config.Bind("default", "Scrap Types", ScrapType.FromMoon, "What type of scrap to spawn? FromMoon is anything from the moon's scrap pool, CustomList uses the list of items defined in ScrapToSpawn.").Value,
            Config.Bind("default", "Scrap To Spawn", "", "List of scraps to spawn outside. Define name and rarity separated with a colon. Separate each entry with a comma. Example: Large axle:20,Bell:10,Gold bar:2").Value,
            Config.Bind("default", "Spawn Positions", SpawnPositions.OutsideNodes, "Where to spawn scrap? OutsideNodes chooses all nodes outside, NodesNearEntrances chooses outside nodes that are near all entrances, NodesNearMain chooses outside nodes that are near Main entrance, CustomList uses the Custom Spawn Positions list.").Value,
            Config.Bind("default", "Spawn Radius", 10f, "Radius in which to search for a spot on the navmesh to spawn an item to. Set to 0 to spawn exactly on the node/custom position.").Value,
            Config.Bind("default", "Spawn Radius Around Entrances", 40f, "Radius in which to search for nodes around an entrance to spawn items to. Only used when the respective SpawnPositions value is selected.").Value,
            Config.Bind("default", "Custom Spawn Positions", "", "List of 3d positions to spawn items to. Separate X Y and Z value with a comma. Separate each entry with a semicolon. Example: 5.51,-1.52,10 ; 15.1,0.67,25.62").Value
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
