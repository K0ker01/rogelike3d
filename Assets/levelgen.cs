using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularLevelGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public bool generateOnStart = true; // Генерировать уровень при старте
    public bool randomizeSeed = true;   // Использовать случайный сид
    public int seed;                   // Фиксированный сид для генерации
    public Transform sectionsContainer;// Родительский объект для секций
    public int maxLevelSize = 20;      // Максимальное количество секций
    public int maxBranchLength = 5;    // Максимальная длина ветви

    [Header("Prefabs")]
    public List<SectionPrefab> sectionPrefabs = new List<SectionPrefab>(); // Префабы секций
    public List<GameObject> endCapPrefabs = new List<GameObject>();       // Префабы заглушек

    [Header("Generation Rules")]
    public List<GenerationRule> specialRules = new List<GenerationRule>(); // Специальные правила
    public List<string> initialSectionTags = new List<string>() { "start" }; // Теги стартовой секции

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

        // Инициализация генератора случайных чисел
        random = randomizeSeed ? new System.Random() : new System.Random(seed);

        // Создание стартовой секции
        var startSection = GetSectionByTags(initialSectionTags);
        if (startSection == null)
        {
            Debug.LogError("No start section found with tags: " + string.Join(",", initialSectionTags));
            return;
        }

        // Удаляем стартовый префаб из списка, чтобы он не использовался повторно
        sectionPrefabs.Remove(startSection);

        var startInstance = SpawnSection(startSection, Vector3.zero, Quaternion.identity, null);
        ProcessSection(startInstance, 0);

    }

    void ProcessSection(SectionInstance section, int branchOrder)
    {
        // Прекращаем, если достигли максимальной длины ветви или размера уровня
        if (branchOrder >= maxBranchLength) return;
        if (spawnedSections.Count >= maxLevelSize) return;

        foreach (var exit in section.exits)
        {
            // С определенной вероятностью создаем заглушку вместо новой секции
            if (random.Next(100) < section.prefab.deadEndChance || spawnedSections.Count >= maxLevelSize)
            {
                SpawnEndCap(exit);
                continue;
            }

            // Получаем следующую секцию для этого выхода
            var nextSection = GetNextSection(section.prefab.createsTags, exit.customTags);
            if (nextSection == null) continue;

            // --- СТЫКОВКА КОМНАТ ---

            // 1. Спавним новую комнату (временно, без позиции и поворота)
            GameObject newRoomGO = Instantiate(nextSection.prefab);

            Transform entrancePoint = newRoomGO.GetComponent<SectionMeta>().entrancePoint;
            Transform exitPoint = exit.transform;

            // 1. Поворот
            Quaternion deltaRotation = exitPoint.rotation * Quaternion.Inverse(entrancePoint.rotation);
            newRoomGO.transform.rotation = deltaRotation;

            // 2. Смещение
            Vector3 rotatedOffset = deltaRotation * (entrancePoint.position - newRoomGO.transform.position);
            Vector3 positionOffset = exitPoint.position - (newRoomGO.transform.position + rotatedOffset);
            newRoomGO.transform.position += positionOffset;


            // 5. Оборачиваем в SectionInstance
            var sectionInstance = new SectionInstance
            {
                prefab = nextSection,
                instance = newRoomGO,
                exits = newRoomGO.GetComponentsInChildren<ExitPoint>().ToList()
            };

            // 6. Связать выход и новую секцию
            if (exit != null)
            {
                exit.connectedSection = sectionInstance;
            }

            // 7. Сохраняем и продолжаем генерацию
            spawnedSections.Add(sectionInstance);
            ProcessSection(sectionInstance, branchOrder + 1);
        }

        Debug.Log("Processing section: " + section.instance.name + ", exits: " + section.exits.Count);
    }


    SectionPrefab GetNextSection(List<string> createsTags, List<string> exitTags)
    {
        // Объединяем теги с учетом переопределения в точке выхода
        var effectiveTags = exitTags != null && exitTags.Count > 0 ? exitTags : createsTags;

        // Фильтруем секции по тегам и проверяем правила
        var possibleSections = sectionPrefabs
            .Where(s => s.tags.Intersect(effectiveTags).Any())
            .Where(s => CheckRules(s.tags))
            .ToList();

        if (possibleSections.Count == 0) return null;

        // Выбираем случайную секцию из подходящих
        return possibleSections[random.Next(possibleSections.Count)];
    }

    bool CheckRules(List<string> tags)
    {
        // Проверяем все правила для данных тегов
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
        // Создаем экземпляр секции
        var instance = Instantiate(prefab.prefab, position, rotation, sectionsContainer);
        var sectionInstance = new SectionInstance
        {
            prefab = prefab,
            instance = instance,
            exits = instance.GetComponentsInChildren<ExitPoint>().ToList()
        };

        // Связываем с родительским выходом если есть
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
        // Создаем заглушку если есть префабы
        if (endCapPrefabs.Count == 0) return;

        var endPrefab = endCapPrefabs[random.Next(endCapPrefabs.Count)];
        var endInstance = Instantiate(endPrefab, exit.transform.position, exit.transform.rotation, sectionsContainer);
        exit.connectedSection = null;
    }

    void ClearLevel()
    {
        // Удаляем все дочерние объекты
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

    // Класс для хранения информации об экземпляре секции
    public class SectionInstance
    {
        public SectionPrefab prefab;    // Префаб секции
        public GameObject instance;     // Экземпляр на сцене
        public List<ExitPoint> exits;   // Список выходов
    }


}

[System.Serializable]
public class SectionPrefab
{
   
    public GameObject prefab;           // Префаб секции
    public List<string> tags = new List<string>();          // Теги секции
    public List<string> createsTags = new List<string>();   // Какие секции может создавать
    [Range(0, 100)] public int deadEndChance = 10;         // Шанс создания тупика (%)

  
}



[System.Serializable]
public class GenerationRule
{
    public string tag;      // Тег для применения правила
    public int minAmount;   // Минимальное количество
    public int maxAmount;   // Максимальное количество
}