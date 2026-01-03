using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class OSMNode
{
    public ulong ID;
    public Vector3 LocalPosition; // OBJモデル座標系 (X, Y=0, Z)
    public List<ulong> Neighbors = new List<ulong>(); // 隣接ノードID

    public OSMNode(ulong id, Vector3 pos)
    {
        ID = id;
        LocalPosition = pos;
    }
}