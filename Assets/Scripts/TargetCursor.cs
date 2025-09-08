using UnityEngine;

public class TargetCursor : MonoBehaviour
{
    public float yOffset = 0.02f;
    public float cornerInset = 0.12f;   // separación del borde (en unidades de celda)
    public float pulseSpeed = 2.0f;
    public float pulseAmount = 0.05f;   // amplitud del “latido”

    Vector3 baseScale;
    Transform tl, tr, bl, br;

    void Awake()
    {
        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        tl = transform.Find("CornerTL");
        tr = transform.Find("CornerTR");
        bl = transform.Find("CornerBL");
        br = transform.Find("CornerBR");
    }

    void Update()
    {
        // leve “latido” retro
        float p = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = baseScale * p;
    }

    public void SnapToCell(Vector3 worldCenter, float cellSize)
    {
        transform.position = new Vector3(worldCenter.x, worldCenter.y + yOffset, worldCenter.z);
        transform.localScale = new Vector3(cellSize, 1f, cellSize);
        baseScale = transform.localScale; // <-- importante para que el pulso parta del tamaño correcto

        float o = (cellSize * 0.5f) - (cornerInset * cellSize);
        if (tl) tl.localPosition = new Vector3(-o, 0, o);
        if (tr) tr.localPosition = new Vector3(o, 0, o);
        if (bl) bl.localPosition = new Vector3(-o, 0, -o);
        if (br) br.localPosition = new Vector3(o, 0, -o);
    }

    public void Show(bool v) => gameObject.SetActive(v);
}
