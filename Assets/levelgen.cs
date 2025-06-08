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
        return sectionInstance;


    }

    void SpawnEndCap(ExitPoint exit)
    {
        if (endCapPrefabs.Count == 0) return;

        var endPrefab = endCapPrefabs[random.Next(endCapPrefabs.Count)];
        var endInstance = Instantiate(endPrefab, exit.transform.position, exit.transform.rotation, sectionsContainer);
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