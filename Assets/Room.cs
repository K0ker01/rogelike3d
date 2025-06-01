using UnityEngine;
using System.Collections.Generic;

[SelectionBase] // Позволяет легко выбирать всю секцию в редакторе
public class LevelSection : MonoBehaviour
{
    [Header("Section Identification")]
    [Tooltip("Основные теги для идентификации секции")]
    public List<string> sectionTags = new List<string>();

    [Header("Connection Settings")]
    [Tooltip("Какие типы секций можно создать из этого")]
    public List<string> createsTags = new List<string>();

    [Range(0, 100), Tooltip("Шанс что выход станет тупиком")]
    public int deadEndChance = 10;

    [Header("References")]
    [Tooltip("Точка входа в секцию")]
    public Transform entrancePoint;

    [Tooltip("Все выходы из секции")]
    public List<SectionExit> exits = new List<SectionExit>();

    [Header("Bounds")]
    [Tooltip("Коллайдеры границ секции")]
    public List<Collider> boundingColliders = new List<Collider>();

    // Автоматически находим компоненты при валидации
    void OnValidate()
    {
        if (boundingColliders.Count == 0)
        {
            boundingColliders = new List<Collider>(GetComponentsInChildren<Collider>());
            boundingColliders.RemoveAll(c => c.isTrigger);
        }

        if (exits.Count == 0)
        {
            exits = new List<SectionExit>(GetComponentsInChildren<SectionExit>());
        }

        if (entrancePoint == null)
        {
            entrancePoint = transform;
        }
    }

    // Получаем общие границы всех коллайдеров
    public Bounds GetTotalBounds()
    {
        if (boundingColliders.Count == 0) return new Bounds(transform.position, Vector3.zero);

        var bounds = boundingColliders[0].bounds;
        for (int i = 1; i < boundingColliders.Count; i++)
        {
            bounds.Encapsulate(boundingColliders[i].bounds);
        }
        return bounds;
    }

    // Проверка пересечения с другой секцией
    public bool Overlaps(LevelSection other)
    {
        foreach (var colA in boundingColliders)
        {
            foreach (var colB in other.boundingColliders)
            {
                if (colA.bounds.Intersects(colB.bounds))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Визуализация в редакторе
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        foreach (var col in boundingColliders)
        {
            if (col is BoxCollider box)
            {
                Gizmos.matrix = col.transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else
            {
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }

        if (entrancePoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(entrancePoint.position, 0.25f);
            Gizmos.DrawLine(entrancePoint.position,
                           entrancePoint.position + entrancePoint.forward);
        }
    }
#endif
}