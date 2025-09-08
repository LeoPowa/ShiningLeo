using System.Collections.Generic;
using UnityEngine;

public class PulseHighlighter : MonoBehaviour
{
    [Header("Refs")]
    public GridSystem grid;           // tu GridSystem
    public GameObject markerPrefab;   // TilePulse.prefab

    [Header("Ajustes visuales")]
    public float yOffset = 0.02f;

    [Header("Colores")]
    public Color reachableColor = new Color(0.3f, 0.8f, 1f, 0.5f);   // azul (movimiento)
    public Color pathColor = new Color(1f, 0.85f, 0.2f, 0.6f);  // (si quisieras ruta)
    public Color attackColor = new Color(1f, 0.35f, 0.35f, 0.6f); // rojo (ataque)

    readonly List<GameObject> pool = new();
    readonly List<GameObject> actives = new();

    // ---------------- API pública ----------------

    /// <summary>Oculta todos los marcadores activos (mantiene el pool).</summary>
    public void ClearAll()
    {
        foreach (var go in actives) go.SetActive(false);
        actives.Clear();
    }

    /// <summary>Pinta el área alcanzable de movimiento (sustituye lo que hubiera).</summary>
    public void ShowReachable(HashSet<GridSystem.Node> nodes)
    {
        ClearAll();
        if (nodes == null) return;
        foreach (var n in nodes)
            PlaceMarker(n.worldPos, reachableColor);
    }

    /// <summary>Pinta una lista de nodos con color de ruta (no limpia lo anterior).</summary>
    public void ShowPath(List<GridSystem.Node> path)
    {
        if (path == null) return;
        foreach (var n in path)
            PlaceMarker(n.worldPos, pathColor);
    }

    /// <summary>
    /// Pinta el anillo Manhattan de alcance de ATTACK entre min..max desde un origen (ignora obstáculos, como en SF2).
    /// Útil para espadas (1..1), lanzas (1..2), arcos (2..3), etc.
    /// </summary>
    public void ShowAttackAreaFrom(GridSystem.Node origin, int minRange, int maxRange, bool clearFirst = true)
    {
        if (grid == null) return;
        if (clearFirst) ClearAll();

        // Seguridad
        int min = Mathf.Max(0, minRange);
        int max = Mathf.Max(min, maxRange);

        for (int x = 0; x < grid.cellsX; x++)
            for (int z = 0; z < grid.cellsZ; z++)
            {
                var n = grid.nodes[x, z];
                int d = Mathf.Abs(n.x - origin.x) + Mathf.Abs(n.z - origin.z);
                if (d >= min && d <= max)
                    PlaceMarker(n.worldPos, attackColor);
            }
    }

    /// <summary>Pinta un conjunto concreto de nodos de ataque (por si prefieres precalcular y pasar la lista).</summary>
    public void ShowAttackNodes(IEnumerable<GridSystem.Node> nodes, bool clearFirst = true)
    {
        if (nodes == null) return;
        if (clearFirst) ClearAll();
        foreach (var n in nodes)
            PlaceMarker(n.worldPos, attackColor);
    }

    // ---------------- Internos ----------------

    GameObject GetFromPool()
    {
        foreach (var go in pool) if (!go.activeSelf) { go.SetActive(true); return go; }
        var inst = Instantiate(markerPrefab, transform);
        pool.Add(inst);
        return inst;
    }

    void PlaceMarker(Vector3 world, Color c)
    {
        var go = GetFromPool();
        go.transform.position = new Vector3(world.x, world.y + yOffset, world.z);
        go.transform.localScale = new Vector3(grid.cellSize, grid.cellSize, 1f);

        var m = go.GetComponent<TilePulseMarker>();
        if (m)
        {
            m.SetColor(c);
            m.SetPhase(Random.value * 6.2831f); // desfase para no pulsar todos a la vez
        }
        actives.Add(go);
    }
}
