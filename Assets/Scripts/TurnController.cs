using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnController : MonoBehaviour
{
    [Header("Cámara y Control de Movimiento")]
    public CameraFollow camFollow;
    public FreeMoveController freeMove;

    [Header("UI")]
    public CrossActionMenu crossMenu;
    public BattleHUD battleHUD;          // HUD Land/Unit

    [Header("Director de Batalla")]
    public BattleDirector battleDirector; // Arrástralo en el inspector

    [System.Serializable]
    public class Battler
    {
        public Unit unit;
        public int AGI = 10;
        public bool isBossTwoTurns = false;
    }

    [Header("Batalla (orden de turnos)")]
    public List<Battler> party = new();

    int turnIndex = -1;
    List<Battler> initiative = new();

    [Header("Orden estilo SF2")]
    public bool useRandomFactor = true;
    [Range(0.75f, 1.05f)] public float rngMin = 0.80f;
    [Range(0.75f, 1.05f)] public float rngMax = 1.00f;

    int enemyLayer;
    Unit _active; // unidad activa del turno

    void Awake()
    {
        if (!freeMove) freeMove = FindObjectOfType<FreeMoveController>(true);
        if (!crossMenu) crossMenu = FindObjectOfType<CrossActionMenu>(true);
        if (!battleHUD) battleHUD = FindObjectOfType<BattleHUD>(true);
        if (!battleDirector) battleDirector = FindObjectOfType<BattleDirector>(true);
        if (!camFollow && Camera.main) camFollow = Camera.main.GetComponent<CameraFollow>();

        enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0) Debug.LogWarning("No existe layer 'Enemy'. Crea uno con ese nombre.");
    }

    void Start()
    {
        BuildInitiativeOrder();
        NextTurn();
    }

    void BuildInitiativeOrder()
    {
        initiative.Clear();
        foreach (var b in party)
            if (b != null && b.unit != null)
                initiative.Add(b);

        initiative.Sort((a, b) =>
        {
            float agiA = a.AGI * (useRandomFactor ? Random.Range(rngMin, rngMax) : 1f);
            float agiB = b.AGI * (useRandomFactor ? Random.Range(rngMin, rngMax) : 1f);
            return agiB.CompareTo(agiA); // descendente
        });

        turnIndex = -1;
    }

    public void NextTurn()
    {
        if (initiative.Count == 0)
        {
            BuildInitiativeOrder();
            if (initiative.Count == 0) return;
        }

        turnIndex++;
        if (turnIndex >= initiative.Count)
        {
            BuildInitiativeOrder();
            turnIndex = 0;
        }

        var activeBattler = initiative[turnIndex];
        _active = activeBattler.unit;
        if (!_active) { NextTurn(); return; }

        if (camFollow) camFollow.target = _active.transform;
        if (battleHUD) { battleHUD.SetCurrent(_active); battleHUD.OnIdle(); battleHUD.Show(true); }

        bool isEnemy = (_active.gameObject.layer == enemyLayer)
                       || _active.GetComponent<EnemyAIController>() != null;

        if (isEnemy) StartCoroutine(EnemyTurn(_active));
        else StartPlayerTurn(_active);
    }

    void StartPlayerTurn(Unit active)
    {
        if (!freeMove)
        {
            Debug.LogError("TurnController: FreeMoveController no asignado.");
            EndTurn(); return;
        }

        freeMove.enabled = true;
        freeMove.BeginForUnit(active);

        freeMove.OnStepStateChanged = moving =>
        {
            if (!battleHUD) return;
            if (moving) battleHUD.OnMoving(); else battleHUD.OnIdle();
        };

        freeMove.OnMovementConfirmed = null;
        freeMove.OnMovementConfirmed = () =>
        {
            if (!crossMenu)
            {
                Debug.LogError("TurnController: CrossActionMenu no asignado. Se salta el turno.");
                EndTurn(); return;
            }

            // callback para reabrir el menú si el jugador CANCELA el targeting
            System.Action reopenMenu = () =>
                crossMenu.Open(_active, _active.transform.position, EndTurn, freeMove);

            crossMenu.onAttack = () =>
            {
                crossMenu.Close();

                if (!battleDirector)
                {
                    Debug.LogWarning("No hay BattleDirector; retorno a FreeMove.");
                    freeMove.ResumeFromMenu();
                    return;
                }

                // firma correcta con cancel
                battleDirector.BeginTargeting(_active, EndTurn, reopenMenu);
            };

            crossMenu.onMagic = () => { crossMenu.Close(); freeMove.ResumeFromMenu(); };
            crossMenu.onItem = () => { crossMenu.Close(); freeMove.ResumeFromMenu(); };
            crossMenu.onStay = EndTurn;

            Vector3 anchor = _active.transform.position;
            crossMenu.Open(_active, anchor, EndTurn, freeMove);
        };
    }

    IEnumerator EnemyTurn(Unit enemy)
    {
        if (freeMove) { freeMove.OnMovementConfirmed = null; freeMove.enabled = false; }

        var ai = enemy.GetComponent<EnemyAIController>();
        if (!ai)
        {
            Debug.LogWarning($"EnemyTurn: {enemy.name} sin EnemyAIController. Skip.");
            yield return new WaitForSeconds(0.2f);
            EndTurn(); yield break;
        }

        bool finished = false;

        yield return ai.TakeTurn(
            onAttack: (attacker, target) =>
            {
                // lanzar combate real
                StartCoroutine(
                battleDirector.ExecuteBattle(attacker, target, onFinished: () => finished = true)
);

            },
            onDone: () => finished = true
        );

        while (!finished) yield return null;
        EndTurn();
    }

    void EndTurn()
    {
        if (freeMove) freeMove.OnMovementConfirmed = null;
        if (crossMenu)
        {
            crossMenu.onAttack = null;
            crossMenu.onMagic = null;
            crossMenu.onItem = null;
            crossMenu.onStay = null;
        }
        NextTurn();
    }
}
