using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleTargeting : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset input;
    public string actionMap = "Gameplay";
    public string moveAction = "Move";
    public string confirmAction = "Confirm";
    public string cancelAction = "Cancel";

    [Header("Refs")]
    public GridSystem grid;
    public TargetCursor cursorPrefab;
    public BattleHUD enemyHud;           // opcional
    public PulseHighlighter highlighter; // pinta el anillo de ataque
    public Transform cursorParent;       // opcional

    [Header("Timing")]
    public float repeatDelay = 0.2f;
    public float repeatRate = 0.08f;

    // callbacks
    public System.Action<Unit, Unit> OnTargetConfirmed; // (attacker, target)
    public System.Action OnCancelled;
    public System.Action<string> OnMessage;             // "No opponent there."

    // runtime
    Unit attacker;
    GridSystem.Node origin;
    List<Unit> targets = new();
    int index = -1;
    TargetCursor cursor;
    bool active;
    bool noTargetsMode;   // <-- NUEVO: modo “no hay enemigos”
    float nextInput;

    InputAction aMove, aConfirm, aCancel;

    // -------- API --------
    public void Begin(Unit attackerUnit)
    {
        if (!grid || !attackerUnit)
        {
            OnMessage?.Invoke("Targeting: grid/unit missing.");
            Cancel();
            return;
        }
        var node = grid.GetNodeFromWorld(attackerUnit.transform.position);
        Begin(attackerUnit, node);
    }

    public void Begin(Unit attackerUnit, GridSystem.Node originNode)
    {
        attacker = attackerUnit;
        origin = originNode;

        if (!attacker || attacker.weapon == null)
        {
            OnMessage?.Invoke("No weapon equipped.");
            Cancel();
            return;
        }

        // Anillo Manhattan min..max como SF2
        int rMin = Mathf.Max(1, attacker.weapon.minRange);
        int rMax = Mathf.Max(rMin, attacker.weapon.maxRange);
        if (highlighter) highlighter.ShowAttackAreaFrom(origin, rMin, rMax);

        BuildTargets();

        var map = input.FindActionMap(actionMap, true);
        aMove = map.FindAction(moveAction, true);
        aConfirm = map.FindAction(confirmAction, true);
        aCancel = map.FindAction(cancelAction, true);

        active = true;
        noTargetsMode = false;
        nextInput = 0f;

        if (targets.Count == 0)
        {
            // Mantén el highlight visible y espera entrada del jugador
            noTargetsMode = true;
            SetupCursor();           // opcional; lo dejamos oculto
            if (cursor) cursor.Show(false);
            OnMessage?.Invoke("No opponent there.");
            return;
        }

        // Sólo si hay objetivos instanciamos/mostramos cursor
        SetupCursor();
        Select(0);
    }

    void Update()
    {
        if (!active) return;

        if (noTargetsMode)
        {
            // Espera Confirm/Cancel para cerrar, manteniendo el highlight hasta entonces
            if (aConfirm.WasPerformedThisFrame() || aCancel.WasPerformedThisFrame())
            {
                End();
                OnCancelled?.Invoke();
                OnMessage?.Invoke(null);
            }
            return;
        }

        Vector2 mv = aMove.ReadValue<Vector2>();
        if (mv.sqrMagnitude > 0.2f)
        {
            if (Time.time >= nextInput)
            {
                Vector2Int dir = Mathf.Abs(mv.x) > Mathf.Abs(mv.y)
                    ? new Vector2Int(mv.x > 0 ? 1 : -1, 0)
                    : new Vector2Int(0, mv.y > 0 ? 1 : -1);

                MoveSelection(dir);
                nextInput = nextInput == 0f ? Time.time + repeatDelay : Time.time + repeatRate;
            }
        }
        else nextInput = 0f;

        if (aConfirm.WasPerformedThisFrame() && index >= 0 && index < targets.Count)
        {
            var tgt = targets[index];
            End();
            OnTargetConfirmed?.Invoke(attacker, tgt);
        }

        if (aCancel.WasPerformedThisFrame())
        {
            Cancel();
        }
    }

    // -------- Internos --------
    void BuildTargets()
    {
        targets.Clear();
        int rMin = Mathf.Max(1, attacker.weapon.minRange);
        int rMax = Mathf.Max(rMin, attacker.weapon.maxRange);
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        bool attackerIsEnemy = attacker.gameObject.layer == enemyLayer;
        foreach (var u in FindObjectsOfType<Unit>())
        {
            if (u == attacker) continue;
            if (!u.IsAlive) continue;
            // Filtrar por bando opuesto: si atacante es enemigo, buscamos jugadores; si atacante es jugador, buscamos enemigos.
            if (attackerIsEnemy)
            {
                if (u.gameObject.layer == enemyLayer) continue; // saltar otros enemigos
            }
            else
            {
                if (u.gameObject.layer != enemyLayer) continue; // saltar jugadores (queremos solo enemigos)
            }
            // Ahora 'u' es potencial objetivo. Verificar rango Manhattan
            var n = grid.GetNodeFromWorld(u.transform.position);
            int dist = Mathf.Abs(n.x - origin.x) + Mathf.Abs(n.z - origin.z);
            if (dist >= rMin && dist <= rMax)
                targets.Add(u);
        }
        // Ordenar objetivos por distancia y ángulo (opcional, para elegir el más cercano primero)
        targets = targets
            .OrderBy(u => Mathf.Abs(grid.GetNodeFromWorld(u.transform.position).x - origin.x)
                        + Mathf.Abs(grid.GetNodeFromWorld(u.transform.position).z - origin.z))
            .ThenBy(u => Vector3.SignedAngle(Vector3.forward,
                                            (u.transform.position - attacker.transform.position).normalized,
                                             Vector3.up))
            .ToList();
    }


    int Manhattan(Unit u)
    {
        var n = grid.GetNodeFromWorld(u.transform.position);
        return Mathf.Abs(n.x - origin.x) + Mathf.Abs(n.z - origin.z);
    }

    float AngleFromAttacker(Unit u)
    {
        var a = attacker.transform.position;
        var b = u.transform.position;
        var dir = (b - a); dir.y = 0;
        return Vector3.SignedAngle(Vector3.forward, dir.normalized, Vector3.up);
    }

    void SetupCursor()
    {
        if (!cursor && cursorPrefab)
            cursor = Instantiate(cursorPrefab, cursorParent ? cursorParent : transform);
        if (cursor) cursor.Show(false);
    }

    void Select(int newIndex)
    {
        if (targets.Count == 0)
        {
            if (cursor) cursor.Show(false);
            return;
        }

        index = Mathf.Clamp(newIndex, 0, targets.Count - 1);
        var t = targets[index];
        var n = grid.GetNodeFromWorld(t.transform.position);

        if (cursor)
        {
            cursor.SnapToCell(n.worldPos, grid.cellSize);
            cursor.Show(true);
        }

        if (enemyHud)
        {
            enemyHud.Show(true);
            enemyHud.SetCurrent(t);
            enemyHud.OnIdle();
        }
    }

    void MoveSelection(Vector2Int dir)
    {
        if (targets.Count <= 1) return;

        var aPos = attacker.transform.position;
        int best = index;
        float bestDot = -999f;
        Vector3 desired = new Vector3(dir.x, 0, dir.y).normalized;

        for (int i = 0; i < targets.Count; i++)
        {
            if (i == index) continue;
            var v = (targets[i].transform.position - aPos); v.y = 0;
            float dot = Vector3.Dot(desired, v.normalized);
            if (dot > bestDot) { bestDot = dot; best = i; }
        }

        Select(best);
    }

    void Cancel()
    {
        End();
        OnCancelled?.Invoke();
        OnMessage?.Invoke(null);
    }

    void End()
    {
        active = false;
        noTargetsMode = false;
        if (cursor) cursor.Show(false);
        if (enemyHud) enemyHud.Show(false);
        if (highlighter) highlighter.ClearAll();
    }
}
