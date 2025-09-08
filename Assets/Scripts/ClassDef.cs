// ClassDef.cs
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "SF/ClassDef")]
public class ClassDef : ScriptableObject
{
    public string code;            // "KNTE", "PLDN", "PGNT", "RNGR", "SNIP", etc.
    public string displayName;     // "Knight", "Paladin", ...
    public MoveKind moveKind;      // Knight / Ground / Flier, etc. (para costos terreno)
    public int baseMOV;            // MOV por clase (ej. KNTE 7; PLDN 7; PGNT 7)

    public WeaponGroup[] allowedWeaponGroups;   // qué grupos puede equipar
    public PromotionOption[] promotions;        // ramas desde ESTA clase
}

[Serializable]
public class PromotionOption
{
    public ClassDef target;
    public int minLevel = 20;
    public PromoItem requiredItem = PromoItem.None; // PegasusWing, WarriorPride, etc.
    public string notes;
}

