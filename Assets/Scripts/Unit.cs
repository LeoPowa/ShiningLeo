using System;
using System.Linq;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Datos (SO)")]
    public UnitDef def;          // Arr�stralo en el prefab
    public ClassDef classDef;    // Clase actual (por defecto, def.baseClass)
    public WeaponDef weapon;     // Arma equipada (por defecto, def.startingWeapon)

    [Header("Estado runtime")]
    public int level = 1;

    // >>> Mantengo estos nombres porque otros scripts ya los usan <<<
    public int MOV { get; private set; }           // le�do por FreeMoveController
    public MoveKind moveKind { get; private set; } // le�do por Pathfinder/Grid

    [Header("Animator (opcional)")]
    public Animator animator;
    public string movingBool = "isMoving";

    // === NUEVO: Estad�sticas runtime que piden otros sistemas (HP/MP) ===
    [Header("HP/MP (runtime)")]
    [SerializeField] int maxHP = 12;
    [SerializeField] int maxMP = 8;
    [SerializeField] int hp = -1;  // -1 = sin inicializar
    [SerializeField] int mp = -1;

    public int MaxHP => maxHP;
    public int MaxMP => maxMP;
    public int HP { get => hp; private set => hp = value; }
    public int MP { get => mp; private set => mp = value; }
    public bool IsAlive => HP > 0;

    // === NUEVO: Prefab visual para la Battle Arena (opcional) ===
    [Header("Battle Display (opcional)")]
    public GameObject displayPrefab;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();

        // Clase & nivel desde el UnitDef
        if (!classDef && def) classDef = def.baseClass;
        if (def) level = Mathf.Max(1, def.startLevel);

        RecalcFromClass();

        // Arma inicial
        if (!weapon && def && def.startingWeapon) Equip(def.startingWeapon);

        // Inicializa HP/MP si estaban sin setear
        EnsureRuntimeInitialized();
    }

    /// <summary>Inicializa los valores runtime si no estaban asignados.</summary>
    public void EnsureRuntimeInitialized()
    {
        if (HP < 0) HP = Mathf.Max(1, MaxHP);
        if (MP < 0) MP = Mathf.Max(0, MaxMP);
    }

    /// <summary>Relee MOV, moveKind y valida arma seg�n la clase actual.</summary>
    public void RecalcFromClass()
    {
        if (classDef)
        {
            moveKind = classDef.moveKind;
            MOV = classDef.baseMOV;

            if (def && def.baseMOVOverride > 0)
                MOV = def.baseMOVOverride;

            // Si el arma actual no es v�lida para la nueva clase, la soltamos
            if (weapon && !CanEquip(weapon))
                weapon = null;
        }
        else
        {
            // Defaults seguros
            moveKind = MoveKind.Ground;
            MOV = 6;
        }
    }

    /// <summary>�La clase actual puede equipar esta arma?</summary>
    public bool CanEquip(WeaponDef w)
    {
        if (!w || !classDef) return false;

        // �Grupo permitido por la clase?
        bool allowedByGroup = classDef.allowedWeaponGroups != null &&
                              classDef.allowedWeaponGroups.Contains(w.group);

        if (allowedByGroup) return true;

        // �Excepci�n expl�cita en el arma?
        if (w.allowedClassOverrides != null && w.allowedClassOverrides.Length > 0)
            return Array.Exists(w.allowedClassOverrides, c => c == classDef);

        return false;
    }

    /// <summary>Equipa si es v�lido.</summary>
    public bool Equip(WeaponDef w)
    {
        if (!CanEquip(w)) return false;
        weapon = w;
        return true;
    }

    /// <summary>Promoci�n a otra clase (resetea nivel a 1 estilo SF2).</summary>
    public void Promote(ClassDef target)
    {
        if (!target) return;
        classDef = target;
        level = 1;
        RecalcFromClass();
    }

    /// <summary>Hook para el Animator (lo llama el motor de movimiento).</summary>
    public void SetMoving(bool value)
    {
        if (animator && !string.IsNullOrEmpty(movingBool))
            animator.SetBool(movingBool, value);
    }

    // ===== API DE COMBATE QUE NECESITAN TUS CONTROLADORES =====

    /// <summary>Aplica da�o y devuelve el da�o efectivo.</summary>
    public int TakeDamage(int amount)
    {
        int dmg = Mathf.Max(0, amount);
        HP = Mathf.Max(0, HP - dmg);
        // TODO: eventos / VFX / actualizaci�n de HUD si procede
        return dmg;
    }

    /// <summary>Cura y clampa a MaxHP.</summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        HP = Mathf.Min(MaxHP, HP + amount);
    }

    /// <summary>Intenta gastar MP. Devuelve true si pudo.</summary>
    public bool SpendMP(int amount)
    {
        if (amount <= 0) return true;
        if (MP < amount) return false;
        MP -= amount;
        return true;
    }

    /// <summary>Prefab visual para la arena de batalla (fallback: este mismo GO).</summary>
    public GameObject GetDisplayPrefab()
    {
        return displayPrefab ? displayPrefab : gameObject;
    }
}
