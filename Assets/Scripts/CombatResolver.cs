using UnityEngine;

public struct CombatOutcome
{
    public bool attackerHits;
    public int attackerDamage;
    public bool defenderCounters;
    public bool defenderHits;
    public int defenderDamage;
}

public class CombatResolver : MonoBehaviour
{
    [Header("Valores por defecto (hasta tener stats reales)")]
    public int defaultDamage = 5;

    public CombatOutcome Resolve(Unit attacker, Unit defender)
    {
        // TODO: Integrar fórmulas SF2 (ATK-DEF, hit/evade, crit, doble, contra)
        var outcome = new CombatOutcome
        {
            attackerHits = true,
            attackerDamage = Mathf.Max(1, defaultDamage),
            defenderCounters = false,
            defenderHits = false,
            defenderDamage = 0
        };
        return outcome;
    }

    public void ApplyOutcome(Unit attacker, Unit defender, CombatOutcome o)
    {
        if (o.attackerHits && defender) defender.TakeDamage(o.attackerDamage);
        if (o.defenderHits && attacker) attacker.TakeDamage(o.defenderDamage);
    }
}
