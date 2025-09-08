// WeaponDef.cs
using UnityEngine;

[CreateAssetMenu(menuName = "SF/WeaponDef")]
public class WeaponDef : ScriptableObject
{
    public string displayName;         // "Short Spear", "Bronze Lance", "Robin Arrow"
    public WeaponGroup group;
    public int attackBonus;            // +ATK del arma
    public int minRange = 1;           // típico SF2: Spears 1-2, Bows 2 o 2-3
    public int maxRange = 1;

    // restricciones (además del grupo)
    public ClassDef[] allowedClassOverrides;    // si quieres excepciones

    // efectos especiales
    public string onUseSpell;           // "Blaze 2", "Bolt 2", ...
    public bool isCursed;
    public bool twoHanded;
}
