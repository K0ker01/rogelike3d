using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class BSPDungeonGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    [Range(0, 1)] public float splitRandomness = 0.25f;

    [Header("Leaf Settings")]
    public int minLeafSize = 20;
    public int maxLeafSize = 30;
    public int minRoomSize = 5;
    public int maxRoomSize = 15;

    [Header("Visualization")]
    public Material dungeonMaterial;
    public bool drawGizmos = true;
    public Color leafColor = new Color(0, 1, 0, 0.25f);
    public Color roomColor = new Color(1, 0, 0, 0.5f);
    public Color corridorColor = Color.blue;

    private List<Leaf> leaves = new List<Leaf>();
    private int[,] dungeonMap;
    private List<Rect> rooms = new List<Rect>();
    private List<Rect> corridors = new List<Rect>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private class Leaf
    {
        public int x, y, width, height;
        public Leaf leftChild, rightChild;
        public Rect room;
        public Rect corridor;

        public Leaf(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool IsLeaf => leftChild == null && rightChild == null;

        public bool Split(int minSize, int maxSize, float randomness)
        {
            if (!IsLeaf) return false;

            // Определяем направление разделения
            bool splitHorizontally = Random.value > 0.5f;

            // Учитываем соотношение сторон
            if (width > height && (float)width / height >= 1.25f)
                splitHorizontally = false;
            else if (height > width && (float)height / width >= 1.25f)
                splitHorizontally = true;

            // Проверяем возможность разделения
            int maxSplit = (splitHorizontally ? height : width) - minSize;
            if (maxSplit <= minSize) return false;

            // Добавляем элемент случайности
            if (Random.value < randomness) splitHorizontally = !splitHorizontally;

            int split = Random.Range(minSize, maxSplit);

            if (splitHorizontally)
            {
                leftChild = new Leaf(x, y, width, split);
                rightChild = new Leaf(x, y + split, width, height - split);
            }
            else
            {
                leftChild = new Leaf(x, y, split, height);
                rightChild = new Leaf(x + split, y, width - split, height);
            }

            return true;
        }

        public void CreateRooms(int minRoomSize, int maxRoomSize)
        {
            if (!IsLeaf)
            {
                if (leftChild != null) leftChild.CreateRooms(minRoomSize, maxRoomSize);
                if (rightChild != null) rightChild.CreateRooms(minRoomSize, maxRoomSize);

                if (leftChild != null && rightChild != null)
                {
                    CreateCorridorBetweenRooms(leftChild.GetRoom(), rightChild.GetRoom());
                }
            }
            else
            {
                // Создаем комнату с отступами от краев листа
                int roomWidth = Mathf.Clamp(Random.Range(minRoomSize, maxRoomSize), minRoomSize, width - 2);
                int roomHeight = Mathf.Clamp(Random.Range(minRoomSize, maxRoomSize), minRoomSize, height - 2);

                int roomX = Random.Range(1, width - roomWidth - 1);
                int roomY = Random.Range(1, height - roomHeight - 1);

                room = new Rect(x + roomX, y + roomY, roomWidth, roomHeight);
            }
        }

        private void CreateCorridorBetweenRooms(Rect leftRoom, Rect rightRoom)
        {
            // Соединяем центры комнат
            Vector2 leftCenter = leftRoom.center;
            Vector2 rightCenter = rightRoom.center;

            // Определяем направление коридора (L-образный)
            if (Random.value > 0.5f)
            {
                // Горизонтальный, затем вертикальный
                corridor = new Rect(
                    (int)leftCenter.x, (int)leftCenter.y,
                    (int)(rightCenter.x - leftCenter.x), 1
                );

                if (rightCenter.y > leftCenter.y)
                {
                    corridor = new Rect(
                        (int)rightCenter.x, (int)leftCenter.y,
                        1, (int)(rightCenter.y - leftCenter.y)
                    );
                }
                else
                {
                    corridor = new Rect(
                        (int)rightCenter.x, (int)rightCenter.y,
                        1, (int)(leftCenter.y - rightCenter.y)
                    );
                }
            }
            else
            {
                // Вертикальный, затем горизонтальный
                if (rightCenter.y > leftCenter.y)
                {
                    corridor = new Rect(
                        (int)leftCenter.x, (int)leftCenter.y,
                        1, (int)(rightCenter.y - leftCenter.y)
                    );
                }
                else
                {
                    corridor = new Rect(
                        (int)leftCenter.x, (int)rightCenter.y,
                        1, (int)(leftCenter.y - rightCenter.y)
                    );
                }

                corridor = new Rect(
                    (int)leftCenter.x, (int)rightCenter.y,
                    (int)(rightCenter.x - leftCenter.x), 1
                );
            }
        }

        public Rect GetRoom()
        {
            if (room != Rect.zero) return room;

            Rect leftRoom = Rect.zero;
            Rect rightRoom = Rect.zero;

            if (leftChild != null) leftRoom = leftChild.GetRoom();
            if (rightChild != null) rightRoom = rightChild.GetRoom();

            if (leftRoom == Rect.zero && rightRoom == Rect.zero)
                return Rect.zero;
            if (rightRoom == Rect.zero)
                return leftRoom;
            if (leftRoom == Rect.zero)
                return rightRoom;

            return Random.value > 0.5f ? leftRoom : rightRoom;
        }
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        if (dungeonMaterial != null)
        {
            meshRenderer.material = dungeonMaterial;
        }
    }

    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        ClearDungeon();
        InitializeDungeonMap();
        CreateBSPTree();
        CreateRoomsAndCorridors();
        GenerateDungeonMesh();
    }

    private void ClearDungeon()
    {
        leaves.Clear();
        rooms.Clear();
        corridors.Clear();

        if (meshFilter.sharedMesh != null)
        {
            DestroyImmediate(meshFilter.sharedMesh);
        }
    }

    private void InitializeDungeonMap()
    {
        dungeonMap = new int[dungeonWidth, dungeonHeight];
    }

    private void CreateBSPTree()
    {
        Leaf root = new Leaf(0, 0, dungeonWidth, dungeonHeight);
        leaves.Add(root);

        bool didSplit = true;
        while (didSplit)
        {
            didSplit = false;
            foreach (Leaf leaf in leaves.ToArray())
            {
                if (leaf.IsLeaf)
                {
                    if (leaf.width > maxLeafSize ||
                        leaf.height > maxLeafSize ||
                        Random.value > splitRandomness)
                    {
                        if (leaf.Split(minLeafSize, maxLeafSize, splitRandomness))
                        {
                            leaves.Add(leaf.leftChild);
                            leaves.Add(leaf.rightChild);
                            didSplit = true;
                        }
                    }
                }
            }
        }
    }

    private void CreateRoomsAndCorridors()
    {
        if (leaves.Count == 0) return;

        leaves[0].CreateRooms(minRoomSize, maxRoomSize);

        // Собираем все комнаты и коридоры
        CollectRoomsAndCorridors(leaves[0]);

        // Заполняем карту dungeonMap
        foreach (Rect room in rooms)
        {
            FillRect(room, 1); // 1 - пол комнаты
        }

        foreach (Rect corridor in corridors)
        {
            FillRect(corridor, 1); // 1 - пол коридора
        }
    }

    private void CollectRoomsAndCorridors(Leaf leaf)
    {
        if (leaf.IsLeaf)
        {
            if (leaf.room != Rect.zero)
            {
                rooms.Add(leaf.room);
            }
        }
        else
        {
            if (leaf.leftChild != null) CollectRoomsAndCorridors(leaf.leftChild);
            if (leaf.rightChild != null) CollectRoomsAndCorridors(leaf.rightChild);

            if (leaf.corridor != Rect.zero)
            {
                corridors.Add(leaf.corridor);
            }
        }
    }

    private void FillRect(Rect rect, int value)
    {
        for (int x = (int)rect.x; x < rect.x + rect.width; x++)
        {
            for (int y = (int)rect.y; y < rect.y + rect.height; y++)
            {
                if (x >= 0 && x < dungeonWidth && y >= 0 && y < dungeonHeight)
                {
                    dungeonMap[x, y] = value;
                }
            }
        }
    }

    private void GenerateDungeonMesh()
    {
        MeshGenerator meshGenerator = new MeshGenerator();
        meshGenerator.GenerateMesh(dungeonMap, 1);
        meshFilter.sharedMesh = meshGenerator.CreateMesh();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || leaves == null) return;

        // Рисуем листья
        Gizmos.color = leafColor;
        foreach (Leaf leaf in leaves)
        {
            if (leaf.IsLeaf)
            {
                Vector3 center = new Vector3(leaf.x + leaf.width / 2f, 0, leaf.y + leaf.height / 2f);
                Vector3 size = new Vector3(leaf.width, 0.1f, leaf.height);
                Gizmos.DrawCube(center, size);
            }
        }

        // Рисуем комнаты
        Gizmos.color = roomColor;
        foreach (Rect room in rooms)
        {
            Vector3 center = new Vector3(room.x + room.width / 2f, 0, room.y + room.height / 2f);
            Vector3 size = new Vector3(room.width, 0.2f, room.height);
            Gizmos.DrawCube(center, size);
        }

        // Рисуем коридоры
        Gizmos.color = corridorColor;
        foreach (Rect corridor in corridors)
        {
            Vector3 center = new Vector3(corridor.x + corridor.width / 2f, 0, corridor.y + corridor.height / 2f);
            Vector3 size = new Vector3(corridor.width, 0.15f, corridor.height);
            Gizmos.DrawCube(center, size);
        }
    }
}

public class MeshGenerator
{
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    public void GenerateMesh(int[,] map, float squareSize)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] == 1)
                {
                    // Добавляем квадрат для каждой ячейки карты
                    AddSquare(x, y, squareSize);
                }
            }
        }
    }

    private void AddSquare(int x, int y, float size)
    {
        Vector3 center = new Vector3(x * size, 0, y * size);
        float halfSize = size / 2f;

        // 4 вершины квадрата
        Vector3[] squareVertices = new Vector3[4]
        {
            center + new Vector3(-halfSize, 0, -halfSize),
            center + new Vector3(-halfSize, 0, halfSize),
            center + new Vector3(halfSize, 0, halfSize),
            center + new Vector3(halfSize, 0, -halfSize)
        };

        // Добавляем вершины
        int vertexIndex = vertices.Count;
        vertices.AddRange(squareVertices);

        // Добавляем треугольники (2 треугольника на квадрат)
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);

        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);

        // UV координаты
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(1, 0));
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}