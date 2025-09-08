using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    [Header("Autoría")]
    public int cellsX = 32;
    public int cellsZ = 32;
    public float cellSize = 1f;
    public float extraRayHeight = 10f;
    public LayerMask walkableMask; // TerrainWalkable
    public LayerMask obstacleMask; // Obstacle
    public TerrainType defaultTerrain;
    public float obstacleCheckHalfHeight = 1.0f; // alto para CheckBox (pilares, paredes)

    [System.Serializable]
    public class Node
    {
        public int x, z;
        public Vector3 worldPos;
        public bool blocked;
        public TerrainType terrain;
    }

    public Node[,] nodes { get; private set; }
    Bounds _bounds;

    void Awake()
    {
        var box = GetComponent<BoxCollider>();
        _bounds = box.bounds;
        Build();
    }

    public void Build()
    {
        nodes = new Node[cellsX, cellsZ];

        float stepX = _bounds.size.x / cellsX;
        float stepZ = _bounds.size.z / cellsZ;

        for (int x = 0; x < cellsX; x++)
            for (int z = 0; z < cellsZ; z++)
            {
                var n = new Node { x = x, z = z, terrain = defaultTerrain, blocked = true };
                // centro de celda (X,Z)
                float cx = _bounds.min.x + (x + 0.5f) * stepX;
                float cz = _bounds.min.z + (z + 0.5f) * stepZ;
                float castY = _bounds.max.y + extraRayHeight;
                Vector3 from = new Vector3(cx, castY, cz);

                // 1) Encontrar suelo (solo Walkable) para fijar altura
                if (Physics.Raycast(from, Vector3.down, out var hit, Mathf.Infinity, walkableMask))
                {
                    n.worldPos = hit.point + Vector3.up * 0.01f;
                    // tipo de terreno por marcador
                    var marker = hit.collider.GetComponentInParent<TerrainMarker>();
                    if (marker && marker.terrainType) n.terrain = marker.terrainType;

                    // 2) Chequeo de bloqueo con caja contra Obstacles
                    Vector3 halfExt = new Vector3(cellSize * 0.45f, obstacleCheckHalfHeight, cellSize * 0.45f);
                    Vector3 center = new Vector3(cx, n.worldPos.y + halfExt.y, cz);
                    n.blocked = Physics.CheckBox(center, halfExt, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore) == true ? true : false;
                    // Si no hay bloqueo → celda válida
                    if (!n.blocked) n.blocked = false;
                }
                else
                {
                    // sin suelo walkable debajo → fuera del tablero
                    n.worldPos = new Vector3(cx, _bounds.min.y, cz);
                    n.blocked = true;
                }

                nodes[x, z] = n;
            }
    }

    public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < cellsX && z < cellsZ;

    public Node GetNodeFromWorld(Vector3 world)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((world.x - _bounds.min.x) / (_bounds.size.x / cellsX)), 0, cellsX - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt((world.z - _bounds.min.z) / (_bounds.size.z / cellsZ)), 0, cellsZ - 1);
        return nodes[x, z];
    }

    public IEnumerable<Node> GetNeighbors(Node n)
    {
        int[,] dirs = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };
        for (int i = 0; i < 4; i++)
        {
            int nx = n.x + dirs[i, 0];
            int nz = n.z + dirs[i, 1];
            if (InBounds(nx, nz)) yield return nodes[nx, nz];
        }
    }

    void OnDrawGizmosSelected()
    {
        if (nodes == null) return;
        foreach (var n in nodes)
        {
            Gizmos.color = n.blocked ? new Color(1, 0, 0, 0.25f) : new Color(0, 1, 1, 0.25f);
            Gizmos.DrawCube(n.worldPos, new Vector3(cellSize, 0.02f, cellSize));
        }
    }
}
