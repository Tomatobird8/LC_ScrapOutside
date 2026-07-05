using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace LC_ScrapOutside.Patches
{
    [HarmonyPatch]
    public class SpawnScrap
    {
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.waitForScrapToSpawnToSync))]
        [HarmonyPrefix]
        public static void WaitForScrapToSpawn_Prefix(ref NetworkObjectReference[] spawnedScrap, ref int[] scrapValues)
        {
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 69420 - 67);
            if (random.NextDouble() >= LC_ScrapOutside.chanceToSpawn.Value)
            {
                LC_ScrapOutside.Logger.LogInfo($"Unlucky! No scrap will spawn due to ChanceToSpawn: {LC_ScrapOutside.chanceToSpawn.Value} being lower than the chosen random value.");
                return;
            }

            if (LC_ScrapOutside.extraVarianceMin.Value > LC_ScrapOutside.extraVarianceMax.Value)
            {
                LC_ScrapOutside.Logger.LogWarning("ExtraVarianceMin was smaller than ExtraVarianceMax. Please readjust your configuration.");
                LC_ScrapOutside.extraVarianceMin.Value = LC_ScrapOutside.extraVarianceMax.Value;
                LC_ScrapOutside.extraVarianceMin.ConfigFile.Save();
                LC_ScrapOutside.Logger.LogWarning("ExtraVarianceMin was replaced with ExtraVarianceMax to avoid errors.");
            }

            if (LC_ScrapOutside.minScrapToSpawn.Value > LC_ScrapOutside.maxScrapToSpawn.Value)
            {
                LC_ScrapOutside.Logger.LogWarning("MinScrapToSpawn was smaller than MaxScrapToSpawn. Please readjust your configuration.");
                LC_ScrapOutside.minScrapToSpawn.Value = LC_ScrapOutside.maxScrapToSpawn.Value;
                LC_ScrapOutside.minScrapToSpawn.ConfigFile.Save();
                LC_ScrapOutside.Logger.LogWarning("MinScrapToSpawn was replaced with MaxScrapToSpawn to avoid errors.");
            }

            if (LC_ScrapOutside.randomScrapMin.Value > LC_ScrapOutside.randomScrapMax.Value)
            {
                LC_ScrapOutside.Logger.LogWarning("RandomScrapMin was smaller than RandomScrapMax. Please readjust your configuration.");
                LC_ScrapOutside.randomScrapMin.Value = LC_ScrapOutside.randomScrapMax.Value;
                LC_ScrapOutside.randomScrapMin.ConfigFile.Save();
                LC_ScrapOutside.Logger.LogWarning("RandomScrapMin was replaced with RandomScrapMax to avoid errors.");
            }

            int amount = GetScrapSpawnAmount();
            
            if (amount <= 0)
            {
                LC_ScrapOutside.Logger.LogInfo("No scrap to spawn outside.");
                return;
            }

            LC_ScrapOutside.Logger.LogInfo($"Spawning {amount} scrap objects outside.");

            List<Item> ScrapToSpawn = SelectScrap(amount);
            List<int> ScrapValues = [];

            List<NetworkObjectReference> ScrapNetworkObjects = [];
            List<Vector3> Nodes = [.. GameObject.FindGameObjectsWithTag("OutsideAINode").Select(n => n.transform.position)];
            if (Nodes.Count <= 0) 
            {
                LC_ScrapOutside.Logger.LogWarning("No outside nodes found. Outside scrap cannot be spawned.");
                return;
            }

            for (int i = 0; i < ScrapToSpawn.Count; i++)
            {
                Vector3 pos = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(Nodes[UnityEngine.Random.Range(0, Nodes.Count)], 10f, RoundManager.Instance.navHit, random);
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

            for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
            {
                scrapWeights.Add(RoundManager.Instance.currentLevel.spawnableScrap[i].rarity);
            }
            int[] weights = [.. scrapWeights];
            for (int i = 0; i < amount; i++)
            {
                scrapToSpawn.Add(RoundManager.Instance.currentLevel.spawnableScrap[RoundManager.Instance.GetRandomWeightedIndex(weights, random)].spawnableItem);
            }

            return scrapToSpawn;
        }

        internal static float GetExtraVariance()
        {
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 35121);
            return ((float)random.NextDouble() * (LC_ScrapOutside.extraVarianceMax.Value - LC_ScrapOutside.extraVarianceMin.Value)) + LC_ScrapOutside.extraVarianceMin.Value;
        }

        internal static int GetScrapSpawnAmount() 
        {
            switch (LC_ScrapOutside.scrapCountAlgorithm.Value)
            {
                case LC_ScrapOutside.Algorithm.Static:
                    return GetStaticScrapAmount();

                case LC_ScrapOutside.Algorithm.Random:
                    return GetRandomScrapAmount();

                case LC_ScrapOutside.Algorithm.Dynamic:
                    return Mathf.RoundToInt(GetDynamicScrapAmount());
            }
            return 0;
        }

        internal static int GetStaticScrapAmount()
        {
            return LC_ScrapOutside.staticScrapToSpawn.Value >= 0 ? LC_ScrapOutside.staticScrapToSpawn.Value : 0;
        }

        internal static int GetRandomScrapAmount()
        {
            System.Random random = new(StartOfRound.Instance.randomMapSeed + 64914);
            return random.Next(LC_ScrapOutside.randomScrapMin.Value, LC_ScrapOutside.randomScrapMax.Value + 1);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        internal static float GetDynamicScrapAmount()
        {
            float multiplier = 1f;
            LC_ScrapOutside.Logger.LogDebug($"Multiplier at Start: {multiplier}");

            if (LC_ScrapOutside.scaleScrapByLuck.Value)
            {
                multiplier *= (CalculateLuckValue() * LC_ScrapOutside.luckMultiplier.Value) + 1;
                LC_ScrapOutside.Logger.LogDebug($"Multiplier after luckBasedScrapMultiplier: {multiplier}");
            }
            if (LC_ScrapOutside.scaleScrapByQuota.Value)
            {
                multiplier *= Mathf.Pow(LC_ScrapOutside.inverseQuotaBasedMultiplier.Value ? (float)LC_ScrapOutside.baselineQuotaValue.Value / TimeOfDay.Instance.profitQuota : (float)TimeOfDay.Instance.profitQuota / LC_ScrapOutside.baselineQuotaValue.Value, LC_ScrapOutside.quotaValueMultiplierScalar.Value);
                LC_ScrapOutside.Logger.LogDebug($"Multiplier after quotaBasedScrapMultiplier: {multiplier}");
            }
            if (LC_ScrapOutside.scaleScrapByMoonScrapAmount.Value)
            {
                float averageScrapCount = (RoundManager.Instance.currentLevel.maxScrap + RoundManager.Instance.currentLevel.minScrap) / 2;
                multiplier *= Mathf.Pow(averageScrapCount / LC_ScrapOutside.baselineMoonScrapAmount.Value, LC_ScrapOutside.moonScrapAmountDifferenceScalar.Value);
                LC_ScrapOutside.Logger.LogDebug($"Multiplier after moonScrapAmountBasedMultiplier: {multiplier}");
            }
            if (LC_ScrapOutside.scaleScrapByWeather.Value) 
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
            if (LC_ScrapOutside.extraVariance.Value)
            {
                multiplier *= GetExtraVariance();
                LC_ScrapOutside.Logger.LogDebug($"Multiplier after extraVariance: {multiplier}");
            }
            return Mathf.Clamp(LC_ScrapOutside.dynamicBaseScrapAmount.Value * multiplier, LC_ScrapOutside.minScrapToSpawn.Value, LC_ScrapOutside.maxScrapToSpawn.Value);
        }

        internal static Dictionary<string, float>? GetWeatherMultipliersDict()
        {
            try
            {
                Dictionary<string, float> dict = [];
                string[] pairs = LC_ScrapOutside.weatherMultipliers.Value.Split(',');
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
            }catch (Exception e)
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
    }
}
