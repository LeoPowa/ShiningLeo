using UnityEngine;

[CreateAssetMenu(menuName = "SF2/Battle Environment Set")]
public class BattleEnvironmentSet : ScriptableObject
{
    public TerrainKind kind = TerrainKind.Default;
    public GameObject environmentPrefab; // Debe contener "AttackerAnchor" y "DefenderAnchor" como hijos
}
