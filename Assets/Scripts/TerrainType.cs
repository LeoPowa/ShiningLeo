using UnityEngine;

[CreateAssetMenu(menuName = "SF2/Terrain Type")]
public class TerrainType : ScriptableObject
{
    [Range(0, 1f)] public float landEffect; // 0.30 = 30%
    [Header("Coste por tipo de unidad (1 = normal)")]
    public float walkerCost = 1f;
    public float cavalryCost = 1f;
    public float flierCost = 1f;
    public float aquaticCost = 1f;
    public string displayName = "Road"; // añade este campo

    public float GetCost(MoveKind kind) => kind switch
    {
        MoveKind.Walker => walkerCost,
        MoveKind.Cavalry => cavalryCost,
        MoveKind.Flier => flierCost,
        MoveKind.Aquatic => aquaticCost,
        _ => 1f
    };
}
