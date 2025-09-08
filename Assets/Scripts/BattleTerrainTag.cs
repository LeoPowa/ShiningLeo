using UnityEngine;

public enum TerrainKind { Default, Plains, Forest, Road, Desert, Mountain, Indoors, Castle, Water }

[DisallowMultipleComponent]
public class BattleTerrainTag : MonoBehaviour
{
    public TerrainKind kind = TerrainKind.Default;
}
