// UnitDef.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SF/UnitDef")]
public class UnitDef : ScriptableObject
{
    public string unitName;            // "Chester"
    public ClassDef baseClass;         // KNTE (humano con lanza)
    public int startLevel = 1;
    public int baseMOVOverride = -1;   // -1 = usa el de la clase
    public WeaponDef startingWeapon;

    // growths (placeholders; luego los afinamos)
    public AnimationCurve hpGrowth, mpGrowth, atkGrowth, defGrowth, agiGrowth;

    // hechizos aprendidos (para MAGE/PRST, etc.)
    public SpellLearn[] spells;
}
[System.Serializable] public struct SpellLearn { public string spellCode; public int level; public bool afterPromotion; }
