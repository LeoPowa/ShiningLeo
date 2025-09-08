using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyAIController : MonoBehaviour
{
    public enum Behavior { Passive, Guard, Aggressive, Patrol }

    [Header("Refs")]
    public GridSystem grid;
    public Pathfinder pathfinder;

    [Header("AI")]
    public Behavior behavior = Behavior.Aggressive;
    public float thinkDelay = 0.15f;
    public float aggroRadius = 999f; // Manhattan/aprox mundo

    // runtime
    Unit self;
    UnitMotor motor;
    int enemyLayer;

    void Awake()
    {
        self = GetComponent<Unit>();
        motor = GetComponent<UnitMotor>();
        if (!motor) motor = gameObject.AddComponent<UnitMotor>();

        if (!grid) grid = FindObjectOfType<GridSystem>(true);
        if (!pathfinder) pathfinder = FindObjectOfType<Pathfinder>(true);

        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    // API: la llama TurnController en turno enemigo
    public IEnumerator TakeTurn(System.Action<Unit, Unit> onAttack, System.Action onDone)
    {
        // guards duros
        if (!self || !grid || !pathfinder)
        {
            Debug.LogError($"EnemyAIController ({name}): faltan refs. self:{self} grid:{grid} path:{pathfinder}");
            onDone?.Invoke(); yield break;
        }

        // 1) Recolecta blancos (unidades del jugador)
        var players = FindObjectsOfType<Unit>()
            .Where(u => u != null && u.gameObject.layer != enemyLayer)
            .ToList();

        if (players.Count == 0) { onDone?.Invoke(); yield break; }

        // 2) Elige objetivo segun dificultad media (más cercano)
        Unit target = ChooseTarget(players);
        if (!target) { onDone?.Invoke(); yield break; }

        // 3) ¿Ya estoy a rango para atacar?
        var myNode = grid.GetNodeFromWorld(self.transform.position);
        var tgtNode = grid.GetNodeFromWorld(target.transform.position);

        int rMin = Mathf.Max(1, self.weapon ? self.weapon.minRange : 1);
        int rMax = Mathf.Max(rMin, self.weapon ? self.weapon.maxRange : 1);
        int dist = Mathf.Abs(myNode.x - tgtNode.x) + Mathf.Abs(myNode.z - tgtNode.z);

        if (dist >= rMin && dist <= rMax)
        {
            // atacar “en seco” de momento
            onAttack?.Invoke(self, target);
            onDone?.Invoke();
            yield break;
        }

        // 4) Moverse hacia el objetivo: elige la mejor casilla alcanzable que acerque
        //    (usamos mismo reachable que el FreeMove)
        bool[,] occ = new bool[grid.cellsX, grid.cellsZ]; // de momento no marcamos ocupación dura
        var reachable = pathfinder.ReachableWithParents(myNode, self.MOV, self.moveKind, occ);
        if (reachable.set == null || reachable.set.Count == 0)
        {
            onDone?.Invoke(); yield break;
        }

        // entre todas las alcanzables, busca la que minimiza dist. a target
        GridSystem.Node best = myNode;
        int bestD = Manhattan(myNode, tgtNode);

        foreach (var n in reachable.set)
        {
            // saltar bloqueadas o la del propio origen
            if (n.blocked) continue;
            int d = Manhattan(n, tgtNode);
            if (d < bestD)
            {
                bestD = d;
                best = n;
            }
        }

        // 5) reconstruir path desde myNode -> best con el diccionario parent
        var path = ReconstructPath(reachable.parent, myNode, best);
        if (path.Count <= 1)
        {
            // no hay movimiento útil
            onDone?.Invoke(); yield break;
        }

        // 6) Moverse
        yield return motor.MoveAlong(path);

        // 7) Intentar atacar si ahora entro en rango
        myNode = grid.GetNodeFromWorld(self.transform.position);
        dist = Mathf.Abs(myNode.x - tgtNode.x) + Mathf.Abs(myNode.z - tgtNode.z);
        if (dist >= rMin && dist <= rMax)
        {
            onAttack?.Invoke(self, target);
        }

        onDone?.Invoke();
    }

    Unit ChooseTarget(List<Unit> allPlayers)
    {
        if (allPlayers == null || allPlayers.Count == 0) return null;

        var myN = grid.GetNodeFromWorld(transform.position);

        // “media dificultad”: más cercano (Manhattan). Nada de HP, ni healer-focus todavía.
        return allPlayers
            .OrderBy(u =>
            {
                var n = grid.GetNodeFromWorld(u.transform.position);
                return Mathf.Abs(n.x - myN.x) + Mathf.Abs(n.z - myN.z);
            })
            .FirstOrDefault();
    }

    static int Manhattan(GridSystem.Node a, GridSystem.Node b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);

    List<GridSystem.Node> ReconstructPath(Dictionary<GridSystem.Node, GridSystem.Node> parent,
                                          GridSystem.Node start, GridSystem.Node goal)
    {
        var path = new List<GridSystem.Node>();
        var cur = goal;
        path.Add(cur);
        while (cur != start && parent.TryGetValue(cur, out var p))
        {
            cur = p;
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }
}
