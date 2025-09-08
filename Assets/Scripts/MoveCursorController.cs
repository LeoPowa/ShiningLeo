// MoveCursorController.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MoveCursorController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset input; // asigna tu .inputactions
    public string actionMap = "Gameplay";
    public string moveAction = "Move";
    public string confirmAction = "Confirm";
    public string cancelAction = "Cancel";

    [Header("Visuals")]
    public GameObject cursorPrefab;            // un círculo/quad
    public GameObject pathNodePrefab;          // esfera pequeña para vista de camino (opcional)

    [Header("Refs")]
    public GridSystem grid;
    public Pathfinder pathfinder;

    // estado turn/move
    Unit activeUnit;
    GridSystem.Node startNode;
    GridSystem.Node currentNode;
    HashSet<GridSystem.Node> reachable;
    Dictionary<GridSystem.Node, GridSystem.Node> parents;
    List<GameObject> pathViz = new();

    GameObject cursorGO;
    InputAction aMove, aConfirm, aCancel;
    float inputCooldown = 0.15f;
    float nextInputTime = 0f;

    public System.Action OnMoveConfirmed; // para abrir menú luego

    void OnEnable()
    {
        var map = input.FindActionMap(actionMap, true);
        aMove = map.FindAction(moveAction, true);
        aConfirm = map.FindAction(confirmAction, true);
        aCancel = map.FindAction(cancelAction, true);
        map.Enable();
    }
    void OnDisable() { input.FindActionMap(actionMap, true).Disable(); }

    public void BeginForUnit(Unit unit)
    {
        activeUnit = unit;
        startNode = grid.GetNodeFromWorld(unit.transform.position);
        currentNode = startNode;

        // calcula área
        bool[,] occ = new bool[grid.cellsX, grid.cellsZ];
        // marcar ocupación si tienes más unidades…
        var res = pathfinder.ReachableWithParents(startNode, unit.MOV, unit.moveKind, occ);
        reachable = res.set; parents = res.parent;

        // cursor
        if (cursorGO == null) cursorGO = Instantiate(cursorPrefab);
        cursorGO.transform.position = startNode.worldPos;

        DrawPathPreview(startNode); // path vacío (en origen)
    }

    void Update()
    {
        if (activeUnit == null) return;

        Vector2 mv = aMove.ReadValue<Vector2>();
        if (Time.time >= nextInputTime && mv.sqrMagnitude > 0.2f)
        {
            Vector2Int step = Vector2Int.zero;
            if (Mathf.Abs(mv.x) > Mathf.Abs(mv.y))
                step = new Vector2Int(mv.x > 0 ? 1 : -1, 0);
            else
                step = new Vector2Int(0, mv.y > 0 ? 1 : -1);

            TryMoveCursor(step);
            nextInputTime = Time.time + inputCooldown; // repetición discreta
        }

        if (aConfirm.WasPerformedThisFrame())
            ConfirmMovement();

        if (aCancel.WasPerformedThisFrame())
            CancelMovement();
    }

    void TryMoveCursor(Vector2Int dir)
    {
        int nx = currentNode.x + dir.x;
        int nz = currentNode.z + dir.y;
        if (!grid.InBounds(nx, nz)) return;

        var next = grid.nodes[nx, nz];
        if (!reachable.Contains(next)) return; // fuera del área
        currentNode = next;
        cursorGO.transform.position = currentNode.worldPos;
        DrawPathPreview(currentNode);
    }

    void DrawPathPreview(GridSystem.Node target)
    {
        // limpia
        foreach (var g in pathViz) Destroy(g);
        pathViz.Clear();

        var path = pathfinder.ReconstructPath(startNode, target, parents);
        if (path == null) return;

        foreach (var n in path)
        {
            var p = Instantiate(pathNodePrefab, n.worldPos + Vector3.up * 0.02f, Quaternion.identity);
            pathViz.Add(p);
        }
    }

    void ConfirmMovement()
    {
        if (currentNode == startNode) { EndPhase(false); return; }

        var path = pathfinder.ReconstructPath(startNode, currentNode, parents);
        StartCoroutine(MoveRoutine(path));
    }

    System.Collections.IEnumerator MoveRoutine(List<GridSystem.Node> path)
    {
        // desactiva input mientras se mueve
        input.FindActionMap(actionMap).Disable();
        yield return activeUnit.GetComponent<UnitMotor>().MoveAlong(path);
        input.FindActionMap(actionMap).Enable();
        EndPhase(true);
    }

    void CancelMovement()
    {
        currentNode = startNode;
        cursorGO.transform.position = currentNode.worldPos;
        DrawPathPreview(currentNode);
    }

    void EndPhase(bool moved)
    {
        // limpia visualización (cursor lo puedes dejar si quieres)
        foreach (var g in pathViz) Destroy(g);
        pathViz.Clear();
        // aquí abrirías el menú de acciones si moved == true, o directamente menú aunque no te muevas
        OnMoveConfirmed?.Invoke();
        // si quieres ocultar: cursorGO.SetActive(false);
        // luego desuscribe o espera siguiente fase
    }
}
