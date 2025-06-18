using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularLevelGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public bool randomizeSeed = true;
    public int seed;
    public Transform sectionsContainer;
    public int maxLevelSize = 20;
    public int maxBranchLength = 5;

    [Header("Prefabs")]
    public List<SectionPrefab> sectionPrefabs = new List<SectionPrefab>();
    public List<GameObject> endCapPrefabs = new List<GameObject>();

    [Header("Generation Rules")]
    public List<GenerationRule> specialRules = new List<GenerationRule>();
    public List<string> initialSectionTags = new List<string>() { "start" };

    private List<SectionInstance> spawnedSections = new List<SectionInstance>();
    private System.Random random;

    [Header("Enemies")]
    public List<GameObject> enemyPrefabs; // список префабов врагов
    [Range(0, 100)] public int enemySpawnChance = 100; // шанс появления врагов
    public Vector2Int enemiesPerRoom = new Vector2Int(1, 3); // диапазон количества врагов


    void Start()
    {
        if (generateOnStart) GenerateLevel();
    }

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        ClearLevel();

        random = randomizeSeed ? new System.Random() : new System.Random(seed);

        var startSection = GetSectionByTags(initialSectionTags);
        if (startSection == null)
        {
            Debug.LogError("No start section found with tags: " + string.Join(",", initialSectionTags));
            return;
        }

        sectionPrefabs.Remove(startSection);

        var startInstance = SpawnSection(startSection, Vector3.zero, Quaternion.identity, null);
        ProcessSection(startInstance, 0);

    }

    Vector3 GetRandomPointInBounds(BoxCollider box)
    {
        Vector3 center = box.transform.position + box.center;
        Vector3 size = box.size;

        Vector3 randomOffset = new Vector3(
            Random.Range(-size.x / 2f, size.x / 2f),
            0,
            Random.Range(-size.z / 2f, size.z / 2f)
        );

        return center + box.transform.rotation * randomOffset;
    }

    void SpawnEnemiesInRoom(GameObject room)
    {
        if (enemyPrefabs.Count == 0) return;

        var spawnZones = room.GetComponentsInChildren<BoxCollider>()
            .Where(c => c.isTrigger && c.gameObject.name.Contains("EnemySpawnZone"))
            .ToList();

        if (spawnZones.Count == 0) return;

        if (random.Next(100) > enemySpawnChance) return;

        int enemyCount = random.Next(enemiesPerRoom.x, enemiesPerRoom.y + 1);

        List<Vector3> usedPositions = new List<Vector3>();
        float minDistance = 1.0f; // минимальное расстояние между врагами

        int attemptsPerEnemy = 10;

        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPosition = Vector3.zero;
            bool validPositionFound = false;

            for (int attempt = 0; attempt < attemptsPerEnemy; attempt++)
            {
                var zone = spawnZones[random.Next(spawnZones.Count)];
                spawnPosition = GetRandomPointInBounds(zone);

                bool tooClose = usedPositions.Any(pos => Vector3.Distance(pos, spawnPosition) < minDistance);

                if (!tooClose)
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound)
            {
                Debug.LogWarning("Не удалось найти свободное место для врага после нескольких попыток.");
                continue;
            }

            usedPositions.Add(spawnPosition);
            GameObject enemyPrefab = enemyPrefabs[random.Next(enemyPrefabs.Count)];
            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, room.transform);
        }
    }


    void ProcessSection(SectionInstance section, int branchOrder)
    {
        if (branchOrder >= maxBranchLength)
        {
            return;
        }

        foreach (var exit in section.exits)
        {
            if (random.Next(100) < section.prefab.deadEndChance || spawnedSections.Count >= maxLevelSize)
            {
                SpawnEndCap(exit);
                continue;
            }

            bool isLastInBranch = branchOrder + 1 >= maxBranchLength;

            var nextSection = GetNextSection(section.prefab.createsTags, exit.customTags, isLastInBranch);
            if (nextSection == null) continue;

            GameObject newRoomGO = Instantiate(nextSection.prefab);

            Transform entrancePoint = newRoomGO.GetComponent<SectionMeta>().entrancePoint;
            Transform exitPoint = exit.transform;

            Quaternion targetRotation = exitPoint.rotation * Quaternion.Inverse(entrancePoint.rotation);
            newRoomGO.transform.rotation = targetRotation;

            Vector3 entranceWorldPos = entrancePoint.position;
            Vector3 roomOffset = newRoomGO.transform.position - entranceWorldPos;
            newRoomGO.transform.position = exitPoint.position + roomOffset;

            var sectionInstance = new SectionInstance
            {
                prefab = nextSection,
                instance = newRoomGO,
                exits = newRoomGO.GetComponentsInChildren<ExitPoint>().ToList()
            };

            if (exit != null)
            {
                exit.connectedSection = sectionInstance;
            }

            spawnedSections.Add(sectionInstance);
            SpawnEnemiesInRoom(newRoomGO);
            ProcessSection(sectionInstance, branchOrder + 1);
           
        }
    }



    SectionPrefab GetNextSection(List<string> createsTags, List<string> exitTags, bool mustBeRoom = false)
    {
        var effectiveTags = exitTags != null && exitTags.Count > 0 ? exitTags : createsTags;

        var possibleSections = sectionPrefabs
            .Where(s => s.tags.Intersect(effectiveTags).Any())
            .Where(s => CheckRules(s.tags))
            .Where(s => !mustBeRoom || s.isRoom) // если это конец ветки — берем только комнаты
            .ToList();

        if (possibleSections.Count == 0) return null;

        return possibleSections[random.Next(possibleSections.Count)];
    }


    bool CheckRules(List<string> tags)
    {
        foreach (var rule in specialRules)
        {
            if (tags.Contains(rule.tag))
            {
                int currentCount = spawnedSections.Count(s => s.prefab.tags.Contains(rule.tag));
                if (currentCount >= rule.maxAmount) return false;
            }
        }
        return true;
    }

    SectionPrefab GetSectionByTags(List<string> tags)
    {
        return sectionPrefabs.FirstOrDefault(s => s.tags.Intersect(tags).Any());
    }

    SectionInstance SpawnSection(SectionPrefab prefab, Vector3 position, Quaternion rotation, ExitPoint parentExit)
    {
        var instance = Instantiate(prefab.prefab, position, rotation, sectionsContainer);
        var sectionInstance = new SectionInstance
        {
            prefab = prefab,
            instance = instance,
            exits = instance.GetComponentsInChildren<ExitPoint>().ToList()
        };

        if (parentExit != null)
        {
            parentExit.connectedSection = sectionInstance;
        }
        Debug.Log("Spawned section: " + prefab.prefab.name + " at " + position);

        spawnedSections.Add(sectionInstance);
        SpawnEnemiesInRoom(instance);
        return sectionInstance;


    }

    void SpawnEndCap(ExitPoint exit)
    {
        if (endCapPrefabs.Count == 0) return;

        var endPrefab = endCapPrefabs[random.Next(endCapPrefabs.Count)];
        var endInstance = Instantiate(endPrefab, exit.transform.position, exit.transform.rotation, sectionsContainer);
        endInstance.transform.localPosition = exit.transform.position;
        endInstance.transform.localRotation = exit.transform.rotation;
        exit.connectedSection = null;
    }


    void ClearLevel()
    {
        if (sectionsContainer != null)
        {
            foreach (Transform child in sectionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        spawnedSections.Clear();
    }

   


    public class SectionInstance
    {
        public SectionPrefab prefab;
        public GameObject instance;
        public List<ExitPoint> exits;


    }

    [System.Serializable]
    public class SectionPrefab
    {

        public GameObject prefab;
        public List<string> tags = new List<string>();
        public List<string> createsTags = new List<string>();
        [Range(0, 100)] public int deadEndChance = 10;
        public bool isRoom = false;

    }



    [System.Serializable]
    public class GenerationRule
    {
        public string tag;
        public int minAmount;
        public int maxAmount;
    }
}