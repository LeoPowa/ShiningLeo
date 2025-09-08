using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class TilePulseMarker : MonoBehaviour
{
    Renderer rend;
    MaterialPropertyBlock mpb;

    [ColorUsage(false, true)]
    public Color color = new Color(0.3f, 0.8f, 1f, 0.5f);
    [Range(0f, 6.2831f)] public float phase = 0f;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        if (phase == 0f) phase = Random.value * 6.2831f; // desfase para que no pulsen a la vez
        Apply();
    }

    public void SetColor(Color c) { color = c; Apply(); }
    public void SetPhase(float p) { phase = p; Apply(); }

    public void Apply()
    {
        mpb.SetColor("_Color", color);
        mpb.SetFloat("_Offset", phase);
        rend.SetPropertyBlock(mpb);
    }
}
