using System.Collections.Generic;
using UnityEngine;

public class BSPGenerator : MonoBehaviour
{
    public int dungeonWidth = 100;
    public int dungeonHeight = 100;
    public int minRoomSize = 5;
    public int maxRoomSize = 15;
    public GameObject roomPrefab;
    public GameObject corridorPrefab;

    private List<BoundsInt> rooms = new List<BoundsInt>();

    void Start()
    {
        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        BoundsInt dungeonBounds = new BoundsInt(0, 0, 0, dungeonWidth, dungeonHeight, 0);
        List<BoundsInt> roomRegions = SplitBounds(dungeonBounds);

        foreach (var room in roomRegions)
        {
            CreateRoom(room);
        }

        ConnectRooms();
    }

    List<BoundsInt> SplitBounds(BoundsInt space)
    {
        List<BoundsInt> regions = new List<BoundsInt>();
        Queue<BoundsInt> queue = new Queue<BoundsInt>();
        queue.Enqueue(space);

        while (queue.Count > 0)
        {
            BoundsInt current = queue.Dequeue();
            if (current.size.x <= maxRoomSize && current.size.z <= maxRoomSize)
            {
                regions.Add(current);
                continue;
            }

            bool splitVertically = Random.Range(0, 2) == 0;
            if (current.size.x > current.size.z * 1.5f)
                splitVertically = true;
            else if (current.size.z > current.size.x * 1.5f)
                splitVertically = false;

            if (splitVertically)
            {
                int splitX = Random.Range(minRoomSize, current.size.x - minRoomSize);
                queue.Enqueue(new BoundsInt(current.x, current.y, current.z, splitX, current.size.y, current.size.z));
                queue.Enqueue(new BoundsInt(current.x + splitX, current.y, current.z, current.size.x - splitX, current.size.y, current.size.z));
            }
            else
            {
                int splitZ = Random.Range(minRoomSize, current.size.z - minRoomSize);
                queue.Enqueue(new BoundsInt(current.x, current.y, current.z, current.size.x, current.size.y, splitZ));
                queue.Enqueue(new BoundsInt(current.x, current.y, current.z + splitZ, current.size.x, current.size.y, current.size.z - splitZ));
            }
        }

        return regions;
    }

    void CreateRoom(BoundsInt bounds)
    {
        int roomWidth = Mathf.Min(bounds.size.x, maxRoomSize);
        int roomHeight = Mathf.Min(bounds.size.z, maxRoomSize);
        Vector3 roomCenter = new Vector3(bounds.x + roomWidth / 2, 0, bounds.z + roomHeight / 2);
        Instantiate(roomPrefab, roomCenter, Quaternion.identity);
        rooms.Add(bounds);
    }

    void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector3 prevCenter = new Vector3(rooms[i - 1].x + rooms[i - 1].size.x / 2, 0, rooms[i - 1].z + rooms[i - 1].size.z / 2);
            Vector3 currentCenter = new Vector3(rooms[i].x + rooms[i].size.x / 2, 0, rooms[i].z + rooms[i].size.z / 2);

            // Горизонтальный коридор
            if (Random.Range(0, 2) == 0)
            {
                Vector3 corridorPos = new Vector3((prevCenter.x + currentCenter.x) / 2, 0, prevCenter.z);
                Instantiate(corridorPrefab, corridorPos, Quaternion.identity);
            }
            // Вертикальный коридор
            else
            {
                Vector3 corridorPos = new Vector3(currentCenter.x, 0, (prevCenter.z + currentCenter.z) / 2);
                Instantiate(corridorPrefab, corridorPos, Quaternion.identity);
            }
        }
    }
}