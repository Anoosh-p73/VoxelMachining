using System.Collections.Generic;
using UnityEngine;

public class VoxelChunk : MonoBehaviour
{
    public Vector3Int ChunkSize = new Vector3Int(16, 16, 16); // 16x16x16 grid
    public Voxel[,,] Voxels;

    void Awake()
    {
        InitializeVoxels();
    }

    void InitializeVoxels()
    {
        Voxels = new Voxel[ChunkSize.x, ChunkSize.y, ChunkSize.z];
        // Fill the chunk with active voxels (for testing)
        for (int x = 0; x < ChunkSize.x; x++)
        {
            for (int y = 0; y < ChunkSize.y; y++)
            {
                for (int z = 0; z < ChunkSize.z; z++)
                {
                    Voxels[x, y, z] = new Voxel
                    {
                        IsActive = true,
                        Position = new Vector3Int(x, y, z),
                        Color = Color.white
                    };
                }
            }
        }
        GenerateMesh(); 
    }

    void GenerateMesh()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        // Generate cubes for each active voxel
        for (int x = 0; x < ChunkSize.x; x++)
        {
            for (int y = 0; y < ChunkSize.y; y++)
            {
                for (int z = 0; z < ChunkSize.z; z++)
                {
                    if (Voxels[x, y, z].IsActive)
                    {
                        CreateCube(new Vector3(x, y, z), vertices, triangles);
                    }
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    void CreateCube(Vector3 position, List<Vector3> vertices, List<int> triangles)
    {
        // Define cube vertices relative to the voxel position
        Vector3[] cubeVertices = {
            new Vector3(0, 0, 0), new Vector3(1, 0, 0),
            new Vector3(1, 1, 0), new Vector3(0, 1, 0),
            new Vector3(0, 1, 1), new Vector3(1, 1, 1),
            new Vector3(1, 0, 1), new Vector3(0, 0, 1)
        };

        // Define cube triangles (12 triangles total)
        int[] cubeTriangles = {
            0, 2, 1, 0, 3, 2, // Front face
            2, 3, 4, 2, 4, 5, // Top face
            1, 2, 5, 1, 5, 6, // Right face
            0, 7, 4, 0, 4, 3, // Left face
            5, 4, 7, 5, 7, 6, // Back face
            0, 6, 7, 0, 1, 6  // Bottom face
        };

        int vertexOffset = vertices.Count;
        foreach (Vector3 vertex in cubeVertices)
        {
            vertices.Add(position + vertex);
        }
        foreach (int triangle in cubeTriangles)
        {
            triangles.Add(vertexOffset + triangle);
        }
    }
    public void CarveVoxel(Vector3Int position)
    {
        if (IsPositionInChunk(position))
        {
            Voxels[position.x, position.y, position.z].IsActive = false;
            GenerateMesh(); // Update the mesh
        }
    }

    bool IsPositionInChunk(Vector3Int position)
    {
        return position.x >= 0 && position.x < ChunkSize.x &&
               position.y >= 0 && position.y < ChunkSize.y &&
               position.z >= 0 && position.z < ChunkSize.z;
    }

    public List<List<Vector3Int>> FindConnectedComponents()
    {
        bool[,,] visited = new bool[ChunkSize.x, ChunkSize.y, ChunkSize.z];
        List<List<Vector3Int>> components = new List<List<Vector3Int>>();

        for (int x = 0; x < ChunkSize.x; x++)
        {
            for (int y = 0; y < ChunkSize.y; y++)
            {
                for (int z = 0; z < ChunkSize.z; z++)
                {
                    if (Voxels[x, y, z].IsActive && !visited[x, y, z])
                    {
                        List<Vector3Int> component = FloodFill(new Vector3Int(x, y, z), visited);
                        components.Add(component);
                    }
                }
            }
        }
        return components;
    }

    List<Vector3Int> FloodFill(Vector3Int start, bool[,,] visited)
    {
        List<Vector3Int> component = new List<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(start);
        visited[start.x, start.y, start.z] = true;

        Vector3Int[] directions = {
            Vector3Int.up, Vector3Int.down,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.forward, Vector3Int.back
        };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            component.Add(current);

            foreach (Vector3Int dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (IsPositionInChunk(neighbor) &&
                    Voxels[neighbor.x, neighbor.y, neighbor.z].IsActive &&
                    !visited[neighbor.x, neighbor.y, neighbor.z])
                {
                    visited[neighbor.x, neighbor.y, neighbor.z] = true;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return component;
    }

    public void SplitIntoObjects()
    {
        List<List<Vector3Int>> components = FindConnectedComponents();
        if (components.Count <= 1) return; // No split detected

        foreach (List<Vector3Int> component in components)
        {
            // Create a new chunk for this component
            GameObject newChunkObj = new GameObject("Split Chunk");
            VoxelChunk newChunk = newChunkObj.AddComponent<VoxelChunk>();
            newChunk.ChunkSize = ChunkSize;
            newChunk.Voxels = new Voxel[ChunkSize.x, ChunkSize.y, ChunkSize.z];

            // Copy only the voxels in this component
            foreach (Vector3Int pos in component)
            {
                newChunk.Voxels[pos.x, pos.y, pos.z] = Voxels[pos.x, pos.y, pos.z];
            }

            // Deactivate voxels in the original chunk
            foreach (Vector3Int pos in component)
            {
                Voxels[pos.x, pos.y, pos.z].IsActive = false;
            }

            newChunk.GenerateMesh();
            newChunk.transform.position = transform.position;
        }

        GenerateMesh(); // Update the original chunk
    }
}
