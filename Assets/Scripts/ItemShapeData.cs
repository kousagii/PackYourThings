using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "Item" , menuName = "Packing/ Item Shape Data")]
public class ItemShapeData : ScriptableObject
{
    public string itemName;
    public List<Vector2Int> relativeGridPositions = new List<Vector2Int>();
}
