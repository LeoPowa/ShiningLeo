using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeMoveController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset input;
    public string actionMap = "Gameplay";
    public string moveAction = "Move";
    public string confirmAction = "Confirm";
    public string cancelAction = "Cancel";

    [Header("Refs")]
    public GridSystem grid;
    public Pathfinder pathfinder;

    [Header("Pulse (visual del grid)")]
    public PulseHighlighter highlighter;   // solo mostrará alcanzables

    [Header("Movimiento")]
    public float inputRepeatDelay = 0.25f;
    public float inputRepeatRate = 0.10f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip footstepClip;

    // sigue igual: notifica si está moviéndose o quieto
    public System.Action<bool> OnStepStateChanged;
    public GridSystem.Node CurrentNode => currentNode;

    Unit unit;
    UnitMotor motor;

    InputAction aMove, aConfirm, aCancel;
    bool active, stepping;
    float nextInputTime;

    GridSystem.Node startNode;
    GridSystem.Node currentNode;
    HashSet<GridSystem.Node> reachable;
    Dictionary<GridSystem.Node, GridSystem.Node> parents;

    // para deshacer: pila de nodos por los que hemos pasado
    Stack<GridSystem.Node> pathStack = new();

    public System.Action OnMovementConfirmed;

    void OnEnable()
    {
        var map = input.FindActionMap(actionMap, true);
        aMove = map.FindAction(moveAction, true);
        aConfirm = map.FindAction(confirmAction, true);
        aCancel = map.FindAction(cancelAction, true);
        map.Enable();
    }
    void OnDisable()
    {
        input.FindActionMap(actionMap, true).Disable();
    }

    public void BeginForUnit(Unit activeUnit)
    {
        unit = activeUnit;
        motor = unit.GetComponent<UnitMotor>();
        if (!motor) motor = unit.gameObject.AddComponent<UnitMotor>(); // motor de movimiento de la unidad

        startNode = grid.GetNodeFromWorld(unit.transform.position);
        currentNode = startNode;

        // Calcula área alcanzable DESDE el origen (regla SF2), considerando obstáculos dinámicos
        bool[,] occ = new bool[grid.cellsX, grid.cellsZ];
        // **NUEVO:** Marcar casillas ocupadas por unidades (excepto la actual) como bloqueadas
        foreach (Unit u in FindObjectsOfType<Unit>())
        {
            if (!u.IsAlive) continue;
            if (u == unit) continue;
            var node = grid.GetNodeFromWorld(u.transform.position);
            occ[node.x, node.z] = true;
        }
        var res = pathfinder.ReachableWithParents(startNode, unit.MOV, unit.moveKind, occ);
        reachable = res.set;
        parents = res.parent;

        pathStack.Clear();
        active = true;
        stepping = false;
        nextInputTime = 0f;

        // Visual: resaltar celdas alcanzables
        if (highlighter) highlighter.ShowReachable(reachable);

        // Notificar que la unidad está "quieta" al iniciar el turno (para animaciones, etc.)
        OnStepStateChanged?.Invoke(false);
    }

    void Update()
    {
        if (!active || unit == null) return;

        // Mover un paso por pulsación (con auto-repetición)
        Vector2 mv = aMove.ReadValue<Vector2>();
        if (mv.sqrMagnitude > 0.2f)
        {
            if (Time.time >= nextInputTime && !stepping)
            {
                Vector2Int dir = Vector2Int.zero;
                if (Mathf.Abs(mv.x) > Mathf.Abs(mv.y))
                    dir = new Vector2Int(mv.x > 0 ? 1 : -1, 0);
                else
                    dir = new Vector2Int(0, mv.y > 0 ? 1 : -1);

                TryStep(dir);
                nextInputTime = nextInputTime == 0f
                    ? Time.time + inputRepeatDelay
                    : Time.time + inputRepeatRate;
            }
        }
        else
        {
            // suelta el stick/tecla → resetea el primer delay
            nextInputTime = 0f;
        }

        if (aConfirm.WasPerformedThisFrame())
            Confirm();

        if (aCancel.WasPerformedThisFrame())
            StartCoroutine(ReturnToStart());
    }

    void TryStep(Vector2Int dir)
    {
        int nx = currentNode.x + dir.x;
        int nz = currentNode.z + dir.y;
        if (!grid.InBounds(nx, nz)) return;

        var next = grid.nodes[nx, nz];

        // debe ser vecino, no bloqueado y estar en el área alcanzable original
        if (next.blocked || !reachable.Contains(next)) return;

        // permitir deshacer: si el siguiente es el anterior de la pila, pop
        if (pathStack.Count > 0 && pathStack.Peek() == next)
        {
            // paso hacia atrás
            pathStack.Pop();
            StartCoroutine(StepCoroutine(next));
            return;
        }

        // paso hacia delante normal
        pathStack.Push(currentNode);
        StartCoroutine(StepCoroutine(next));
    }

    IEnumerator StepCoroutine(GridSystem.Node target)
    {
        stepping = true;
        OnStepStateChanged?.Invoke(true);

        // mover casilla a casilla (current → target)
        var smallPath = new List<GridSystem.Node> { currentNode, target };
        if (footstepClip && audioSource)
        {
            audioSource.pitch = Random.Range(0.98f, 1.02f); // leve variación
            audioSource.PlayOneShot(footstepClip);
        }
        yield return motor.MoveAlong(smallPath);

        currentNode = target;
        stepping = false;
        OnStepStateChanged?.Invoke(false);

        // Importante: NO pintamos ruta → dejamos sólo las alcanzables
        // (si quisieras refrescar, podrías re-llamar ShowReachable(reachable), pero no hace falta)
    }

    IEnumerator ReturnToStart()
    {
        if (stepping) yield break;
        stepping = true;
        OnStepStateChanged?.Invoke(true);

        // recorre la pila al revés hasta el origen
        var backPath = new List<GridSystem.Node> { currentNode };
        while (pathStack.Count > 0)
            backPath.Add(pathStack.Pop());
        backPath.Add(startNode);

        if (backPath.Count > 1)
            yield return motor.MoveAlong(backPath);

        currentNode = startNode;
        stepping = false;
        OnStepStateChanged?.Invoke(false);

        // Visual: vuelve a mostrar solo alcanzables (por si alguien limpió algo)
        if (highlighter) highlighter.ShowReachable(reachable);
    }

    void Confirm()
    {
        if (stepping) return;
        active = false;
        OnStepStateChanged?.Invoke(false);

        // Visual: al abrir menú, limpia todo
        if (highlighter) highlighter.ClearAll();

        OnMovementConfirmed?.Invoke();
    }

    public void ResumeFromMenu()
    {
        // volver a mover libremente desde la casilla actual
        active = true;
        stepping = false;
        nextInputTime = 0f;

        // Visual: vuelve a mostrar alcanzables desde donde estés
        if (highlighter) highlighter.ShowReachable(reachable);

        OnStepStateChanged?.Invoke(false);
    }
}
