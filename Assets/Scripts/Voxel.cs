using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Voxel
{
    public bool IsActive; // Whether the voxel exists
    public Vector3Int Position; // Grid position
    public Color Color; // Optional: For visualization
}
