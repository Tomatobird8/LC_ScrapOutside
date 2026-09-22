namespace LC_ScrapOutside;
public class Configuration
{
    public string name = "";
    public bool configEnabled;
    public Algorithm scrapCountAlgorithm;
    public float chanceToSpawn;

    public int staticScrapToSpawn;

    public int randomScrapMin;
    public int randomScrapMax;

    public float dynamicBaseScrapAmount;
    public bool scaleScrapByLuck;
    public float luckMultiplier;

    public bool scaleScrapByQuota;
    public bool inverseQuotaBasedMultiplier;
    public int baselineQuotaValue;
    public float quotaValueMultiplierScalar;

    public bool scaleScrapByMoonScrapAmount;
    public float baselineMoonScrapAmount;
    public float moonScrapAmountDifferenceScalar;

    public bool scaleScrapByWeather;
    public string weatherMultipliers = "";

    public int minScrapToSpawn;
    public int maxScrapToSpawn;

    public bool applyRandomVariance;
    public float randomVarianceMin;
    public float randomVarianceMax;

    public ScrapType scrapType;
    public string scrapToSpawn = "";

    public SpawnPositions spawnPositions;
    public float spawnRadius;
    public float spawnRadiusAroundEntrances;
    public string customPositions = "";


    public Configuration(
        string name,
        bool useThis = false,
        Algorithm algorithm = Algorithm.Dynamic,
        float chance = 1.0f,
        int staticScrapAmount = 6,
        int randomScrapMin = 4,
        int randomScrapMax = 8,
        float dynamicBaseAmount = 6f,
        bool scaleByLuck = true,
        float luckMultiplier = 2f,
        bool scaleByQuota = true,
        int baselineQuotaValue = 1000,
        float quotaValueMultiplierScalar = 0.5f,
        bool inverseQuotaBasedMultiplier = false,
        bool scaleByScrapAmount = true,
        float baselineMoonScrapAmount = 18f,
        float moonScrapAmountScalar = 0.5f,
        bool useWeather = true,
        string weatherMultipliers = "none:1.0,rainy:1.1,foggy:1.2,flooded:1.3,stormy:1.4,eclipsed:1.5",
        int minScrapToSpawn = 4,
        int maxScrapToSpawn = 250,
        bool applyRandomVariance = true,
        float randomVarianceMin = 0.7f,
        float randomVarianceMax = 1.3f,
        ScrapType scrapType = ScrapType.FromMoon,
        string scrapToSpawn = "",
        SpawnPositions spawnPositions = SpawnPositions.OutsideNodes,
        float spawnRadius = 10f,
        float spawnRadiusAroundEntrances = 40f,
        string customPositions = ""
        )
    {
        this.name = name;
        configEnabled = useThis;
        scrapCountAlgorithm = algorithm;
        chanceToSpawn = chance;

        staticScrapToSpawn = staticScrapAmount;

        this.randomScrapMin = randomScrapMin;
        this.randomScrapMax = randomScrapMax;

        dynamicBaseScrapAmount = dynamicBaseAmount;

        scaleScrapByLuck = scaleByLuck;
        this.luckMultiplier = luckMultiplier;

        scaleScrapByQuota = scaleByQuota;
        this.baselineQuotaValue = baselineQuotaValue;
        this.quotaValueMultiplierScalar = quotaValueMultiplierScalar;
        this.inverseQuotaBasedMultiplier = inverseQuotaBasedMultiplier;

        scaleScrapByMoonScrapAmount = scaleByScrapAmount;
        this.baselineMoonScrapAmount = baselineMoonScrapAmount;
        moonScrapAmountDifferenceScalar = moonScrapAmountScalar;

        scaleScrapByWeather = useWeather;
        this.weatherMultipliers = weatherMultipliers;

        this.minScrapToSpawn = minScrapToSpawn;
        this.maxScrapToSpawn = maxScrapToSpawn;

        this.applyRandomVariance = applyRandomVariance;
        this.randomVarianceMin = randomVarianceMin;
        this.randomVarianceMax = randomVarianceMax;

        this.scrapType = scrapType;
        this.scrapToSpawn = scrapToSpawn;

        this.spawnPositions = spawnPositions;
        this.spawnRadius = spawnRadius;
        this.spawnRadiusAroundEntrances = spawnRadiusAroundEntrances;
        this.customPositions = customPositions;
    }
}

public enum Algorithm
{
    Static,
    Random,
    Dynamic
}

public enum ScrapType
{
    FromMoon,
    CustomList
}

public enum SpawnPositions
{
    OutsideNodes,
    NodesNearEntrances,
    NodesNearMain,
    CustomList
}
