// TwoFrameSprite.cs
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TwoFrameSprite : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    public Image targetImage;
    public Sprite frameA;
    public Sprite frameB;
    [Range(1f, 20f)] public float fps = 6f;

    Coroutine loop;

    void Reset() { targetImage = GetComponent<Image>(); }

    public void OnSelect(BaseEventData eventData)
    {
        if (loop == null) loop = StartCoroutine(Loop());
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Stop();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Mouse-over también selecciona (útil en PC)
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    IEnumerator Loop()
    {
        bool toggle = false;
        while (true)
        {
            targetImage.sprite = toggle ? frameB : frameA;
            toggle = !toggle;
            yield return new WaitForSeconds(1f / fps);
        }
    }

    void Stop()
    {
        if (loop != null) { StopCoroutine(loop); loop = null; }
        if (targetImage && frameA) targetImage.sprite = frameA; // vuelve al frame base
    }
}
