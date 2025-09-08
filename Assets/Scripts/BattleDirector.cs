using System;
using System.Collections;
using UnityEngine;

public class BattleDirector : MonoBehaviour
{
    [Header("Refs")]
    public GridSystem grid;
    public BattleArenaController arena;
    public BattleTargeting targeting;

    [Header("UI (stub simple)")]
    [Tooltip("Texto temporal para la secuencia de batalla (sustituir por UI real).")]
    public TMPro.TextMeshProUGUI battleStubText;   // opcional, puede ir en Overlay

    Unit _attacker;
    Action _endTurn;
    Action _reopenMenu;

    // ---- PUBLIC API ----

    /// <summary>Abre el selector de objetivo. No ataca todavía.</summary>
    public void BeginTargeting(Unit attacker, Action onFinished, Action onCancelled)
    {
        _attacker = attacker;
        _endTurn = onFinished;
        _reopenMenu = onCancelled;

        if (!attacker || !grid || !targeting)
        {
            Debug.LogError("BattleDirector: faltan refs (attacker/grid/targeting).");
            _endTurn?.Invoke();
            return;
        }

        // Callbacks del selector
        targeting.OnCancelled = () =>
        {
            // Vuelve al menú de acción
            _reopenMenu?.Invoke();
        };

        targeting.OnTargetConfirmed = (atk, tgt) =>
        {
            // Lanza secuencia de batalla stub
            StartCoroutine(ExecuteBattle(atk, tgt));
        };

        targeting.OnMessage = (msg) =>
        {
            if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
        };

        // Mostrar casillas/objetivos desde la posición actual del atacante
        targeting.Begin(attacker);
    }

    /// <summary>Secuencia de batalla mínima (cambio cámara + mensaje + pausa).</summary>
    public IEnumerator ExecuteBattle(Unit attacker, Unit defender, float stubSeconds = 1.2f, Action onFinished = null)
    {
        if (!arena)
        {
            Debug.LogError("BattleDirector: arena null");
            _endTurn?.Invoke();
            onFinished?.Invoke();      // <- llama al callback aunque falle, para no colgar turnos
            yield break;
        }

        // Terreno: coge el del defensor si existe; si no, del atacante
        var posRef = defender ? defender.transform.position : attacker.transform.position;
        var kind = DetectTerrainKind(posRef);

        // Cambiar a entorno/cámara de batalla
        arena.Prepare(kind);

        // Mensaje tipo SF2
        string atkName = attacker ? attacker.name : "???";
        string defName = defender ? defender.name : "???";
        string msg = defender ? $"{atkName} attacks {defName}!" : $"{atkName} attacks!";
        ShowBattleStub(msg, true);

        // Pequeña pausa (simula cinemática de batalla)
        yield return new WaitForSeconds(stubSeconds);

        ShowBattleStub("", false);

        // Volver a cámara de mapa
        arena.Cleanup();

        // Notifica a quien espera (TurnController/IA)
        onFinished?.Invoke();

        // Fin de turno del atacante (en SF2 el turno termina tras atacar)
        _endTurn?.Invoke();
    }


    // ---- Helpers ----

    TerrainKind DetectTerrainKind(Vector3 worldPos)
    {
        if (Physics.Raycast(worldPos + Vector3.up * 2f, Vector3.down, out var hit, 10f))
        {
            var tag = hit.collider.GetComponentInParent<BattleTerrainTag>();
            if (tag) return tag.kind;
        }
        return TerrainKind.Default;
    }

    void ShowBattleStub(string text, bool show)
    {
        if (!battleStubText) return;
        battleStubText.gameObject.SetActive(show);
        battleStubText.text = text;
    }
}
