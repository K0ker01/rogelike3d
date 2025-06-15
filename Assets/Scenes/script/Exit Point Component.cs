using UnityEngine;
using System.Collections.Generic;

public class ExitPoint : MonoBehaviour
{
    public List<string> customTags = new List<string>(); // можно оставить пустым
    public ModularLevelGenerator.SectionInstance connectedSection;
}
