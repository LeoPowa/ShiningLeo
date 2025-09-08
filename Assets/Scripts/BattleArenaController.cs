using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class BattleArenaController : MonoBehaviour
{
    [Header("Root")]
    public Transform arenaRoot;

    [Header("Static Scene Anchors (si no usas prefab de entorno)")]
    public Transform attackerAnchorInScene;
    public Transform defenderAnchorInScene;

    [Header("Environments (opcional)")]
    public List<BattleEnvironmentSet> environments = new();
    public BattleEnvironmentSet fallbackEnvironment;

    [Header("Cameras (Cinemachine 3)")]
    public CinemachineCamera vcamMap;
    public CinemachineCamera vcamBattle;
    public int mapPriority = 10;
    public int battlePriority = 20;

    [Header("UI (opcional)")]
    public TextMeshProUGUI banner;   // arrastra un TMP en overlay de batalla

    GameObject currentEnv;
    Transform attackerAnchor, defenderAnchor;
    readonly System.Collections.Generic.List<GameObject> spawned = new();

    public void Prepare(TerrainKind kind)
    {
        Cleanup();

        var env = environments.Find(e => e && e.kind == kind) ?? fallbackEnvironment;
        if (env && env.environmentPrefab)
        {
            currentEnv = Instantiate(env.environmentPrefab, arenaRoot ? arenaRoot : transform);
            attackerAnchor = FindDeep(currentEnv.transform, "AttackerAnchor");
            defenderAnchor = FindDeep(currentEnv.transform, "DefenderAnchor");
        }

        attackerAnchor ??= attackerAnchorInScene;
        defenderAnchor ??= defenderAnchorInScene;

        if (!attackerAnchor || !defenderAnchor)
        {
            if (!currentEnv)
            {
                currentEnv = new GameObject("BattleEnvironment_TEMP");
                currentEnv.transform.SetParent(arenaRoot ? arenaRoot : transform, false);
            }
            attackerAnchor = new GameObject("AttackerAnchor").transform;
            defenderAnchor = new GameObject("DefenderAnchor").transform;
            attackerAnchor.SetParent(currentEnv.transform, false);
            defenderAnchor.SetParent(currentEnv.transform, false);
            attackerAnchor.localPosition = new Vector3(-1.5f, 0, 0);
            defenderAnchor.localPosition = new Vector3(1.5f, 0, 0);
        }

        // Cámara batalla ON
        if (vcamBattle) vcamBattle.Priority = battlePriority;
        if (vcamMap) vcamMap.Priority = mapPriority;
    }

    public GameObject SpawnDisplayFor(Unit unit, bool isAttacker)
    {
        if (!unit) return null;
        var parent = isAttacker ? attackerAnchor : defenderAnchor;

        GameObject src = unit.displayPrefab ? unit.displayPrefab : unit.gameObject;
        var go = Instantiate(src, parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        if (!unit.displayPrefab)
        {
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>())
                if (!(mb is Animator)) mb.enabled = false;
        }

        go.transform.rotation = Quaternion.Euler(0, isAttacker ? -90f : 90f, 0f);
        spawned.Add(go);
        return go;
    }

    public IEnumerator ShowBanner(string text, float hold)
    {
        if (banner)
        {
            banner.gameObject.SetActive(true);
            banner.text = text;
        }
        yield return new WaitForSeconds(hold);
        if (banner) banner.gameObject.SetActive(false);
    }

    public void Cleanup()
    {
        for (int i = 0; i < spawned.Count; i++) if (spawned[i]) Destroy(spawned[i]);
        spawned.Clear();

        if (currentEnv) Destroy(currentEnv);
        currentEnv = null;
        attackerAnchor = defenderAnchor = null;

        // Cámara mapa ON
        if (vcamBattle) vcamBattle.Priority = mapPriority;
        if (vcamMap) vcamMap.Priority = battlePriority;
    }

    Transform FindDeep(Transform root, string name)
    {
        if (!root) return null;
        var t = root.Find(name);
        if (t) return t;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r) return r;
        }
        return null;
    }
}
