using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace LC_ScrapOutside.Patches;

[HarmonyPatch]
public static class SpawnScrap
{
    static int currentIndex = 0;
    static float luck = 0f;

    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.waitForScrapToSpawnToSync))]
    [HarmonyPrefix]
    public static void WaitForScrapToSpawn_Prefix(ref NetworkObjectReference[] spawnedScrap, ref int[] scrapValues)
    {
        bool configFound = false;
        for (int i = 0; i < LC_ScrapOutside.configurations.Count; i++)
        {
            if (LC_ScrapOutside.configurations[i].name == RoundManager.Instance.currentLevel.PlanetName)
            {
                if (LC_ScrapOutside.configurations[i].configEnabled) currentIndex = i;
                else currentIndex = 0;
                configFound = true;
                LC_ScrapOutside.Logger.LogInfo($"Configuration found for {RoundManager.Instance.currentLevel.PlanetName}! {(LC_ScrapOutside.configurations[i].configEnabled ? "Using custom configurations for this moon." : "Custom configuration for this moon is disabled, using defaults instead.")}");
                break;
            }
        }
        if (!configFound)
        {
            LC_ScrapOutside.Logger.LogWarning($"Configuration for {RoundManager.Instance.currentLevel.PlanetName} was not found! Using defaults...");
            currentIndex = 0;
        }
        if (LC_ScrapOutside.configurations[currentIndex].chanceToSpawn <= 0f)
        {
            LC_ScrapOutside.Logger.LogInfo("Chance for scrap to spawn outside was set to 0, no scrap outside will spawn.");
            return;
        }

        if (LC_ScrapOutside.luckyRoll.Value || LC_ScrapOutside.configurations[currentIndex].scaleScrapByLuck) luck = CalculateLuckValue();
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 69420 - 67);
        if (random.NextDouble() >= LC_ScrapOutside.configurations[currentIndex].chanceToSpawn)
        {
            LC_ScrapOutside.Logger.LogInfo($"Unlucky! No scrap will spawn due to ChanceToSpawn: {LC_ScrapOutside.configurations[currentIndex].chanceToSpawn} being lower than the chosen random value.");
            return;
        }

        if (LC_ScrapOutside.configurations[currentIndex].randomVarianceMin > LC_ScrapOutside.configurations[currentIndex].randomVarianceMax)
        {
            LC_ScrapOutside.Logger.LogWarning("ExtraVarianceMin was smaller than ExtraVarianceMax. Please readjust your configuration.");
            LC_ScrapOutside.configurations[currentIndex].randomVarianceMin = LC_ScrapOutside.configurations[currentIndex].randomVarianceMax;
            LC_ScrapOutside.Logger.LogWarning("ExtraVarianceMin was replaced with ExtraVarianceMax to avoid errors.");
        }

        if (LC_ScrapOutside.configurations[currentIndex].minScrapToSpawn > LC_ScrapOutside.configurations[currentIndex].maxScrapToSpawn)
        {
            LC_ScrapOutside.Logger.LogWarning("MinScrapToSpawn was smaller than MaxScrapToSpawn. Please readjust your configuration.");
            LC_ScrapOutside.configurations[currentIndex].minScrapToSpawn = LC_ScrapOutside.configurations[currentIndex].maxScrapToSpawn;
            LC_ScrapOutside.Logger.LogWarning($"MinScrapToSpawn for {LC_ScrapOutside.configurations[currentIndex].name} was replaced with MaxScrapToSpawn to avoid errors.");
        }

        if (LC_ScrapOutside.configurations[currentIndex].randomScrapMin > LC_ScrapOutside.configurations[currentIndex].randomScrapMax)
        {
            LC_ScrapOutside.Logger.LogWarning("RandomScrapMin was smaller than RandomScrapMax. Please readjust your configuration.");
            LC_ScrapOutside.configurations[currentIndex].randomScrapMin = LC_ScrapOutside.configurations[currentIndex].randomScrapMax;
            LC_ScrapOutside.Logger.LogWarning($"RandomScrapMin for {LC_ScrapOutside.configurations[currentIndex].name} was replaced with RandomScrapMax to avoid errors.");
        }

        int amount = GetScrapSpawnAmount();

        if (amount <= 0)
        {
            LC_ScrapOutside.Logger.LogInfo("No scrap to spawn outside.");
            return;
        }

        LC_ScrapOutside.Logger.LogInfo($"Spawning {amount} scrap objects outside.");

        List<Item> ScrapToSpawn = SelectScrap(amount);
        if (ScrapToSpawn.Count == 0)
        {
            LC_ScrapOutside.Logger.LogWarning("No scrap was selected. Outside scrap cannot be spawned.");
            return;
        }
        List<int> ScrapValues = [];

        List<NetworkObjectReference> ScrapNetworkObjects = [];

        List<Vector3> Nodes = [];
        float maxRadius = 0;
        switch (LC_ScrapOutside.configurations[currentIndex].spawnPositions)
        {
            case SpawnPositions.OutsideNodes:
                Nodes = [.. GameObject.FindGameObjectsWithTag("OutsideAINode").Select(n => n.transform.position)];
                break;
            case SpawnPositions.NodesNearEntrances:
                maxRadius = LC_ScrapOutside.configurations[currentIndex].spawnRadiusAroundEntrances * LC_ScrapOutside.configurations[currentIndex].spawnRadiusAroundEntrances;
                Nodes = [.. GameObject.FindGameObjectsWithTag("OutsideAINode").Select(n => n.transform.position)];
                List<Vector3> Entrances = [.. GameObject.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None).Where(e => e.isEntranceToBuilding).Select(e => e.transform.position)];
                Nodes = [.. Nodes.Where(node => Entrances.Any(e => (node - e).sqrMagnitude <= maxRadius))];
                break;
            case SpawnPositions.NodesNearMain:
                maxRadius = LC_ScrapOutside.configurations[currentIndex].spawnRadiusAroundEntrances * LC_ScrapOutside.configurations[currentIndex].spawnRadiusAroundEntrances;
                Nodes = [.. GameObject.FindGameObjectsWithTag("OutsideAINode").Select(n => n.transform.position)];
                EntranceTeleport[] EntranceTeleports = GameObject.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
                for (int i = 0; i < EntranceTeleports.Length; i++)
                {
                    if (EntranceTeleports[i].entranceId == 0)
                    {
                        Nodes = [.. Nodes.Where(node => (node - EntranceTeleports[i].transform.position).sqrMagnitude <= maxRadius)];
                        break;
                    }
                }
                break;
            case SpawnPositions.CustomList:
                Nodes = ParseCoordinateList(LC_ScrapOutside.configurations[currentIndex].customPositions);
                break;
        }

        if (Nodes.Count <= 0)
        {
            LC_ScrapOutside.Logger.LogWarning("No outside nodes found. Outside scrap cannot be spawned.");
            return;
        }

        if (LC_ScrapOutside.announceInChat.Value && HUDManager.Instance) HUDManager.Instance.AddTextToChatOnServer($"Spawned {amount} scrap outside!");

        for (int i = 0; i < ScrapToSpawn.Count; i++)
        {
            Vector3 pos = new();
            if (LC_ScrapOutside.configurations[currentIndex].spawnRadius > 0f)
            {
                pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(Nodes[UnityEngine.Random.Range(0, Nodes.Count)], 10f, RoundManager.Instance.navHit, random);
            }
            else
            {
                pos = Nodes[UnityEngine.Random.Range(0, Nodes.Count)];
            }
            GameObject obj = UnityEngine.Object.Instantiate(ScrapToSpawn[i].spawnPrefab, pos + Vector3.up * ScrapToSpawn[i].verticalOffset, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
            GrabbableObject grabobj = obj.GetComponent<GrabbableObject>();
            grabobj.transform.rotation = Quaternion.Euler(grabobj.itemProperties.restingRotation);
            grabobj.fallTime = 0.0f;
            ScrapValues.Add((int)(UnityEngine.Random.Range(ScrapToSpawn[i].minValue, ScrapToSpawn[i].maxValue) * RoundManager.Instance.scrapValueMultiplier));
            grabobj.scrapValue = ScrapValues[ScrapValues.Count - 1];
            NetworkObject netobj = obj.GetComponent<NetworkObject>();
            netobj.Spawn();
            ScrapNetworkObjects.Add(netobj);
        }

        List<NetworkObjectReference> newSpawnedScrap = [.. spawnedScrap];
        List<int> newScrapValues = [.. scrapValues];

        for (int i = 0; i < ScrapNetworkObjects.Count; i++)
        {
            newSpawnedScrap.Add(ScrapNetworkObjects[i]);
            newScrapValues.Add(ScrapValues[i]);
        }
        spawnedScrap = [.. newSpawnedScrap];
        scrapValues = [.. newScrapValues];
    }

    internal static List<Item> SelectScrap(int amount)
    {
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 64208);

        List<Item> scrapToSpawn = [];
        List<int> scrapWeights = [];

        switch (LC_ScrapOutside.configurations[currentIndex].scrapType)
        {
            case ScrapType.FromMoon:
                for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
                {
                    scrapWeights.Add(RoundManager.Instance.currentLevel.spawnableScrap[i].rarity);
                }
                for (int i = 0; i < amount; i++)
                {
                    scrapToSpawn.Add(RoundManager.Instance.currentLevel.spawnableScrap[RoundManager.Instance.GetRandomWeightedIndex([.. scrapWeights], random)].spawnableItem);
                }
                break;
            case ScrapType.CustomList:
                List<WeightedItem> spawnableItemsList = ParseItemWeights(LC_ScrapOutside.configurations[currentIndex].scrapToSpawn);
                for (int i = 0;i < spawnableItemsList.Count; i++)
                {
                    scrapWeights.Add(spawnableItemsList[i].Rarity);
                }
                for (int i = 0;i < amount; i++)
                {
                    Item? item = GetItem(spawnableItemsList[RoundManager.Instance.GetRandomWeightedIndex([.. scrapWeights], random)].Name);
                    if (item == null)
                    {
                        LC_ScrapOutside.Logger.LogError("Item reference was null. Continuing...");
                        continue;
                    }
                    scrapToSpawn.Add(item);
                }
                break;
        }

        return scrapToSpawn;
    }

    internal static int GetScrapSpawnAmount()
    {
        float result = 0;
        switch (LC_ScrapOutside.configurations[currentIndex].scrapCountAlgorithm)
        {
            case Algorithm.Static:
                result = GetStaticScrapAmount();
                break;

            case Algorithm.Random:
                result = GetRandomScrapAmount();
                break;

            case Algorithm.Dynamic:
                result = GetDynamicScrapAmount();
                break;
        }
        if (LC_ScrapOutside.luckyRoll.Value)
        {
            if (luck > 0)
            {
                LC_ScrapOutside.Logger.LogDebug("Luck value was above 0. LuckyRoll will be checked.");
                System.Random random = new(StartOfRound.Instance.randomMapSeed + 6892);
                if (Mathf.Clamp(luck, 0, LC_ScrapOutside.maxLuckyLuck.Value) * LC_ScrapOutside.maxLuckyChance.Value / LC_ScrapOutside.maxLuckyLuck.Value > random.NextDouble())
                {
                    LC_ScrapOutside.Logger.LogDebug("Luck check passed! Scrap amount will be multiplied.");
                    result *= LC_ScrapOutside.luckyRollMultiplier.Value;
                    LC_ScrapOutside.Logger.LogDebug($"Scrap amount after LuckyRoll: {result}");
                }
                else
                {
                    LC_ScrapOutside.Logger.LogDebug("No LuckyRoll this time!");
                }
            }
            else
            {
                LC_ScrapOutside.Logger.LogDebug("Luck value was not above 0. LuckyRoll check skipped.");
            }
        }
        return Mathf.RoundToInt(result);
    }

    internal static int GetStaticScrapAmount()
    {
        return LC_ScrapOutside.configurations[currentIndex].staticScrapToSpawn >= 0 ? LC_ScrapOutside.configurations[currentIndex].staticScrapToSpawn : 0;
    }

    internal static int GetRandomScrapAmount()
    {
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 64914);
        return random.Next(LC_ScrapOutside.configurations[currentIndex].randomScrapMin, LC_ScrapOutside.configurations[currentIndex].randomScrapMax + 1);
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    internal static float GetDynamicScrapAmount()
    {
        float multiplier = 1f;
        LC_ScrapOutside.Logger.LogDebug($"Multiplier at Start: {multiplier}");

        if (LC_ScrapOutside.configurations[currentIndex].scaleScrapByLuck)
        {
            multiplier *= (luck * LC_ScrapOutside.configurations[currentIndex].luckMultiplier) + 1;
            LC_ScrapOutside.Logger.LogDebug($"Multiplier after luckBasedScrapMultiplier: {multiplier}");
        }
        if (LC_ScrapOutside.configurations[currentIndex].scaleScrapByQuota)
        {
            multiplier *= Mathf.Pow(LC_ScrapOutside.configurations[currentIndex].inverseQuotaBasedMultiplier ? (float)LC_ScrapOutside.configurations[currentIndex].baselineQuotaValue / TimeOfDay.Instance.profitQuota : (float)TimeOfDay.Instance.profitQuota / LC_ScrapOutside.configurations[currentIndex].baselineQuotaValue, LC_ScrapOutside.configurations[currentIndex].quotaValueMultiplierScalar);
            LC_ScrapOutside.Logger.LogDebug($"Multiplier after quotaBasedScrapMultiplier: {multiplier}");
        }
        if (LC_ScrapOutside.configurations[currentIndex].scaleScrapByMoonScrapAmount)
        {
            float averageScrapCount = (RoundManager.Instance.currentLevel.maxScrap + RoundManager.Instance.currentLevel.minScrap) / 2;
            multiplier *= Mathf.Pow(averageScrapCount / LC_ScrapOutside.configurations[currentIndex].baselineMoonScrapAmount, LC_ScrapOutside.configurations[currentIndex].moonScrapAmountDifferenceScalar);
            LC_ScrapOutside.Logger.LogDebug($"Multiplier after moonScrapAmountBasedMultiplier: {multiplier}");
        }
        if (LC_ScrapOutside.configurations[currentIndex].scaleScrapByWeather)
        {
            Dictionary<string, float>? weatherMultipliers = GetWeatherMultipliersDict();
            if (weatherMultipliers != null)
            {
                if (weatherMultipliers.TryGetValue(RoundManager.Instance.currentLevel.currentWeather.ToString().ToLowerInvariant(), out float weatherMultiplier))
                    multiplier *= weatherMultiplier;
                else LC_ScrapOutside.Logger.LogWarning($"Couldn't get weather multiplier for {RoundManager.Instance.currentLevel.currentWeather.ToString().ToLower()}. weathername:multiplier not found in config.");
            }
            LC_ScrapOutside.Logger.LogDebug($"Multiplier after weatherBasedMultiplier: {multiplier}");
        }
        if (LC_ScrapOutside.configurations[currentIndex].applyRandomVariance)
        {
            multiplier *= GetExtraVariance();
            LC_ScrapOutside.Logger.LogDebug($"Multiplier after extraVariance: {multiplier}");
        }
        return Mathf.Clamp(LC_ScrapOutside.configurations[currentIndex].dynamicBaseScrapAmount * multiplier, LC_ScrapOutside.configurations[currentIndex].minScrapToSpawn, LC_ScrapOutside.configurations[currentIndex].maxScrapToSpawn);
    }

    internal static float GetExtraVariance()
    {
        System.Random random = new(StartOfRound.Instance.randomMapSeed + 35121);
        return ((float)random.NextDouble() * (LC_ScrapOutside.configurations[currentIndex].randomVarianceMax - LC_ScrapOutside.configurations[currentIndex].randomVarianceMin)) + LC_ScrapOutside.configurations[currentIndex].randomVarianceMin;
    }

    internal static Dictionary<string, float>? GetWeatherMultipliersDict()
    {
        try
        {
            Dictionary<string, float> dict = [];
            string[] pairs = LC_ScrapOutside.configurations[currentIndex].weatherMultipliers.Split(',');
            if (pairs.Length <= 0)
            {
                return null;
            }
            foreach (string pair in pairs)
            {
                string[] elements = pair.Split(":");
                if (elements.Length <= 1)
                {
                    continue;
                }
                if (float.TryParse(elements[1].Trim(), out float weatherMultiplier))
                {
                    dict.Add(elements[0].Trim().ToLowerInvariant(), weatherMultiplier);
                }
            }
            return dict;
        } catch (Exception e)
        {
            LC_ScrapOutside.Logger.LogError("Failed to get weather multipliers: " + e);
            return null;
        }
    }

    internal static float CalculateLuckValue()
    {
        if (LC_ScrapOutside.useInternalLuckValue.Value) return TimeOfDay.Instance.luckValue;
        float luck = 0f;
        List<int> furnitureIds = [];

        AutoParentToShip[] array = UnityEngine.Object.FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None);
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
            {
                furnitureIds.Add(array[i].unlockableID);
            }
        }
        for (int j = 0; j < furnitureIds.Count; j++)
        {
            if (furnitureIds[j] > StartOfRound.Instance.unlockablesList.unlockables.Count)
            {
                LC_ScrapOutside.Logger.LogWarning($"'Lucky' furniture with id {furnitureIds[j]} exceeded the unlockables list size; skipping");
            }
            luck = Mathf.Clamp(luck + StartOfRound.Instance.unlockablesList.unlockables[furnitureIds[j]].luckValue, -1f, 10f);
        }
        LC_ScrapOutside.Logger.LogDebug($"Luck calculated: {luck}");
        return luck;
    }

    private static List<WeightedItem> ParseItemWeights(string s)
    {
        List<WeightedItem> items = [];

        try 
        {
            string[] temp = s.Split(',');
            for (int i = 0;i < temp.Length; i++)
            {
                string[] temp2 = temp[i].Split(":");
                WeightedItem item = new()
                {
                    Name = temp2[0],
                    Rarity = int.Parse(temp2[1])
                };
                items.Add(item);
            }
        }catch (Exception e)
        {
            LC_ScrapOutside.Logger.LogError("An exception occured while parsing item weights! Make sure your configuration for custom scrap spawns is correct. " + e.Message);
        }

        return items;
    }

    private static Item? GetItem(string s)
    {
        for (int i = 0;i < StartOfRound.Instance.allItemsList.itemsList.Count; i++)
        {
            if (s == StartOfRound.Instance.allItemsList.itemsList[i].itemName)
            {
                return StartOfRound.Instance.allItemsList.itemsList[i];
            }
        }

        LC_ScrapOutside.Logger.LogError($"Couldn't match string '{s}' to any item. Make sure you've written the name of the item correctly.");

        return null;
    }

    private static List<Vector3> ParseCoordinateList(string s)
    {
        List<Vector3> coordinates = [];
        try
        {
            string[] temp = s.Split(';');
            for (int i = 0; i < temp.Length; i++)
            {
                coordinates.Add(ParseVector3FromString(temp[i]));
            }
        }
        catch (Exception e)
        {
            LC_ScrapOutside.Logger.LogError("Something went wrong while trying to parse list of coordinates. " + e.Message);
        }
        return coordinates;
    }

    private static Vector3 ParseVector3FromString(string s)
    {
        try
        {
            string[] temp = s.Split(',');
            return new Vector3(float.Parse(temp[0].Trim(), CultureInfo.InvariantCulture), float.Parse(temp[1].Trim(), CultureInfo.InvariantCulture), float.Parse(temp[2].Trim(), CultureInfo.InvariantCulture));
        }
        catch (IndexOutOfRangeException e)
        {
            LC_ScrapOutside.Logger.LogError("Specified string could not be parsed for a Vector3. Please use the correct format. Example: '-14.6,4.6,0' " + e.Message);
        }
        catch (ArgumentOutOfRangeException e)
        {
            LC_ScrapOutside.Logger.LogError("Specified string could not be parsed for a Vector3. Please use the correct format. Example: '-14.6,4.6,0' " + e.Message);
        }
        catch (ArgumentNullException e)
        {
            LC_ScrapOutside.Logger.LogError("ArgumentNullException while trying to parse Vector3. " + e.Message);
        }
        catch (FormatException e)
        {
            LC_ScrapOutside.Logger.LogError("FormatException while trying to parse Vector3. " + e.Message);
        }
        return new Vector3();
    }

    internal struct WeightedItem
    {
        public string Name;
        public int Rarity;
    }
}
