using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public int width = 50;
    public int height = 50;
    public string seed;
    public bool useRandomSeed = true;
    [Range(0, 100)] public int randomFillPercent = 45;
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    private int[,] map;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();

        for (int i = 0; i < 10; i++)
        {
            SmoothMap();
        }

        GenerateMesh();
    }

    void RandomFillMap()
    {
        if (useRandomSeed) seed = Time.time.ToString();

        System.Random rng = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1; // Стены по краям
                }
                else
                {
                    map[x, y] = (rng.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);
                if (neighbourWallTiles > 5) map[x, y] = 1;  // Становится стеной, если соседей > 5
                else if (neighbourWallTiles < 3) map[x, y] = 0;  // Становится полом, если соседей < 3
            }
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                    wallCount += map[neighbourX, neighbourY];
                else
                    wallCount++; // Края карты считаем стенами
        return wallCount;
    }

    void GenerateMesh()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x, 0, y);
                if (map[x, y] == 1)
                    Instantiate(wallPrefab, pos, Quaternion.identity);
                else
                    Instantiate(floorPrefab, pos, Quaternion.identity);
            }
    }

    void RemoveSmallCaves(int threshold)
    {
        List<HashSet<Vector2Int>> caveRegions = GetRegions(0);  // Ищем области пола

        foreach (var region in caveRegions)
        {
            if (region.Count < threshold)  // Если комната слишком маленькая
            {
                foreach (var tile in region)
                {
                    map[tile.x, tile.y] = 1;  // Заполняем стенами
                }
            }
        }
    }

    List<HashSet<Vector2Int>> GetRegions(int tileType)
    {
        List<HashSet<Vector2Int>> regions = new List<HashSet<Vector2Int>>();
        bool[,] visited = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!visited[x, y] && map[x, y] == tileType)
                {
                    HashSet<Vector2Int> newRegion = new HashSet<Vector2Int>();
                    Queue<Vector2Int> queue = new Queue<Vector2Int>();
                    queue.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (queue.Count > 0)
                    {
                        Vector2Int tile = queue.Dequeue();
                        newRegion.Add(tile);

                        for (int nx = tile.x - 1; nx <= tile.x + 1; nx++)
                        {
                            for (int ny = tile.y - 1; ny <= tile.y + 1; ny++)
                            {
                                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny] && map[nx, ny] == tileType)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }
                    }
                    regions.Add(newRegion);
                }
            }
        }

        return regions;
    }
}