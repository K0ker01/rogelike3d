using System.Collections.Generic;
using UnityEngine;

public class SectionExit : MonoBehaviour
{
    [Header("Connection Settings")]
    [Tooltip("����������� ���� ��� ����� ������")]
    public List<string> overrideTags = new List<string>();

    [Tooltip("������������ ���� ��� ��������� ������")]
    public List<string> requiredTags = new List<string>();

    [Range(0, 100), Tooltip("����������� ������ ����� ������")]
    public int selectionWeight = 100;

    [Header("Debug")]
    [Tooltip("��������� ������ (������ ��� �������)")]
    public LevelSection connectedSection;

    // ������������ � ���������
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawLine(transform.position,
                       transform.position + transform.forward * 0.5f);

        if (overrideTags.Count > 0 || requiredTags.Count > 0)
        {
            string label = "";
            if (overrideTags.Count > 0)
                label += "Override: " + string.Join(",", overrideTags) + "\n";
            if (requiredTags.Count > 0)
                label += "Requires: " + string.Join(",", requiredTags);

            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, label);
        }
    }
#endif
}