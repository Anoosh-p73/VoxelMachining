using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeNode : MonoBehaviour
{
    public Bounds Bounds { get; private set; }
    public OctreeNode[] Children { get; private set; }
    public Voxel Voxel { get; set; }
    public bool IsLeaf => Children == null;

    public OctreeNode(Bounds bounds, int depth, int maxDepth)
    {
        Bounds = bounds;
        if (depth < maxDepth)
        {
            Children = new OctreeNode[8];
            Vector3 size = bounds.size / 2;
            for (int i = 0; i < 8; i++)
            {
                Vector3 center = bounds.min + new Vector3(
                    (i & 1) * size.x,
                    ((i >> 1) & 1) * size.y,
                    ((i >> 2) & 1) * size.z
                );
                Children[i] = new OctreeNode(new Bounds(center + size / 2, size), depth + 1, maxDepth);
            }
        }
    }
}
