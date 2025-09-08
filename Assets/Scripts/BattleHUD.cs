using UnityEngine;
using TMPro;

public class BattleHUD : MonoBehaviour
{
    [Header("UI")]
    public Canvas canvas;
    public GameObject landPanel;      // panel arriba-izq
    public TextMeshProUGUI landText;  // "LAND 15%"
    public GameObject unitPanel;      // panel arriba-der
    public TextMeshProUGUI unitName;
    public TextMeshProUGUI unitHPMP;

    [Header("Refs")]
    public GridSystem grid;

    Unit current;
    bool forceShown;

    void Awake() { Show(false); }

    public void SetCurrent(Unit u)
    {
        current = u;
        UpdateNow();
    }

    public void Show(bool show)
    {
        if (landPanel) landPanel.SetActive(show);
        if (unitPanel) unitPanel.SetActive(show);
        forceShown = show;
    }

    public void OnIdle() { if (!forceShown) Show(true); UpdateNow(); }
    public void OnMoving() { if (!forceShown) Show(false); }

    public void UpdateNow()
    {
        if (!current || !grid) return;

        var node = grid.GetNodeFromWorld(current.transform.position);
        float le = node.terrain ? node.terrain.landEffect : 0f;
        string tname = node.terrain ? (string.IsNullOrEmpty(node.terrain.displayName) ? "LAND" : node.terrain.displayName.ToUpper()) : "LAND";
        if (landText) landText.text = $"{tname}  {Mathf.RoundToInt(le * 100f)}%";

        // Datos de prueba; luego los leeremos de tus SO
        int hp = 12, mhp = 12, mp = 8, mmp = 8;
        string uname = current.name.ToUpper();
        if (unitName) unitName.text = uname;
        if (unitHPMP) unitHPMP.text = $"HP {hp}/{mhp}   MP {mp}/{mmp}";
    }
}
