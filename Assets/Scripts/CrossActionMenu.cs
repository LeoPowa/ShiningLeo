using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CrossActionMenu : MonoBehaviour
{
    [Header("Canvas raíz (obligatorio)")]
    public Canvas canvas; // Overlay

    [Header("Botones en cruz (sin panel contenedor)")]
    public Button btnUp;    // ATTACK
    public Button btnLeft;  // MAGIC
    public Button btnRight; // ITEM
    public Button btnDown;  // STAY
    public RectTransform btnUpRT, btnLeftRT, btnRightRT, btnDownRT;

    [Header("Etiqueta (opcional)")]
    public TMPro.TextMeshProUGUI label; // “ATTACK”, etc.

    [Header("Placement")]
    public bool fixedBottomCenter = true;
    public float bottomMargin = 48f;
    public Vector2 offsetUp = new Vector2(0f, 36f);
    public Vector2 offsetLeft = new Vector2(-36f, 0f);
    public Vector2 offsetRight = new Vector2(36f, 0f);
    public Vector2 offsetDown = new Vector2(0f, -36f);

    [Header("Input")]
    public InputActionAsset input;
    public string actionMap = "Gameplay";
    public string confirmAction = "Confirm";
    public string cancelAction = "Cancel";

    [Header("SFX (opcional)")]
    public AudioSource uiAudio;
    public AudioClip openClip, moveClip, selectClip, cancelClip;

    [Header("Callbacks (nuevo flujo)")]
    public System.Action onAttack; // TurnController lo asigna para invocar BattleDirector
    public System.Action onMagic;
    public System.Action onItem;
    public System.Action onStay;

    Camera cam;
    InputAction aConfirm, aCancel;
    bool isOpen;
    System.Action onStayEndTurn; // mantenido para compatibilidad si quieres usarlo
    FreeMoveController freeMove;
    Unit currentUnit;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>(true) ?? FindObjectOfType<Canvas>(true);
        cam = Camera.main ?? FindAnyObjectByType<Camera>(FindObjectsInactive.Include);

        if (!btnUpRT && btnUp) btnUpRT = btnUp.transform as RectTransform;
        if (!btnLeftRT && btnLeft) btnLeftRT = btnLeft.transform as RectTransform;
        if (!btnRightRT && btnRight) btnRightRT = btnRight.transform as RectTransform;
        if (!btnDownRT && btnDown) btnDownRT = btnDown.transform as RectTransform;

        ShowAll(false);
        if (label) label.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        var map = input ? input.FindActionMap(actionMap, true) : null;
        if (map != null)
        {
            aConfirm = map.FindAction(confirmAction, true);
            aCancel = map.FindAction(cancelAction, true);
        }
        else
        {
            aConfirm = null; aCancel = null;
        }
    }

    public void Open(Unit unit, Vector3 worldPos, System.Action onStayCallback, FreeMoveController freeMoveRef)
    {
        currentUnit = unit;
        onStayEndTurn = onStayCallback; // opcional (por compatibilidad)
        freeMove = freeMoveRef;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Position(worldPos);
        ShowAll(true);
        if (label) { label.gameObject.SetActive(true); label.text = "ATTACK"; }

        isOpen = true;
        if (btnUp) EventSystem.current.SetSelectedGameObject(btnUp.gameObject);
        if (uiAudio && openClip) uiAudio.PlayOneShot(openClip);

        // listeners
        if (btnUp) btnUp.onClick.AddListener(OnAttack);
        if (btnLeft) btnLeft.onClick.AddListener(OnMagic);
        if (btnRight) btnRight.onClick.AddListener(OnItem);
        if (btnDown) btnDown.onClick.AddListener(OnStay);

        AddHoverLabel(btnUp, "ATTACK");
        AddHoverLabel(btnLeft, "MAGIC");
        AddHoverLabel(btnRight, "ITEM");
        AddHoverLabel(btnDown, "STAY");
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        ShowAll(false);
        if (label) label.gameObject.SetActive(false);

        if (btnUp) btnUp.onClick.RemoveListener(OnAttack);
        if (btnLeft) btnLeft.onClick.RemoveListener(OnMagic);
        if (btnRight) btnRight.onClick.RemoveListener(OnItem);
        if (btnDown) btnDown.onClick.RemoveListener(OnStay);

        ClearTriggers(btnUp); ClearTriggers(btnLeft); ClearTriggers(btnRight); ClearTriggers(btnDown);
    }

    void Update()
    {
        if (!isOpen || aCancel == null) return;
        if (aCancel.WasPerformedThisFrame())
        {
            if (uiAudio && cancelClip) uiAudio.PlayOneShot(cancelClip);
            Close();
            // Cancel en menú vuelve al control de movimiento (como SF2)
            if (freeMove) freeMove.ResumeFromMenu();
        }
    }

    // ---------- Helpers ----------
    void ShowAll(bool show)
    {
        if (btnUp) btnUp.gameObject.SetActive(show);
        if (btnLeft) btnLeft.gameObject.SetActive(show);
        if (btnRight) btnRight.gameObject.SetActive(show);
        if (btnDown) btnDown.gameObject.SetActive(show);
    }

    void Position(Vector3 worldPos)
    {
        if (!canvas) return;
        RectTransform canvasRect = canvas.transform as RectTransform;

        Vector2 baseLocal;
        if (fixedBottomCenter)
        {
            baseLocal = new Vector2(0f, -canvasRect.rect.height * 0.5f + bottomMargin);
        }
        else
        {
            var camToUse = cam ?? Camera.main;
            Vector3 screen = camToUse
                ? camToUse.WorldToScreenPoint(worldPos + Vector3.up * 1.0f)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : camToUse,
                out baseLocal
            );
            baseLocal += new Vector2(80f, -40f);
        }

        if (btnUpRT) btnUpRT.anchoredPosition = baseLocal + offsetUp;
        if (btnLeftRT) btnLeftRT.anchoredPosition = baseLocal + offsetLeft;
        if (btnRightRT) btnRightRT.anchoredPosition = baseLocal + offsetRight;
        if (btnDownRT) btnDownRT.anchoredPosition = baseLocal + offsetDown;

        if (label)
        {
            var lrt = label.transform as RectTransform;
            lrt.anchoredPosition = baseLocal + new Vector2(120f, 8f);
        }
    }

    void AddHoverLabel(Selectable sel, string text)
    {
        if (!sel) return;
        var trig = sel.gameObject.GetComponent<EventTrigger>();
        if (!trig) trig = sel.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
        entry.callback.AddListener(_ =>
        {
            if (label) label.text = text;
            if (uiAudio && moveClip) uiAudio.PlayOneShot(moveClip);
        });
        trig.triggers.Add(entry);
    }
    void ClearTriggers(Selectable sel)
    {
        var trig = sel ? sel.gameObject.GetComponent<EventTrigger>() : null;
        if (trig) trig.triggers.Clear();
    }

    void SelectSfx() { if (uiAudio && selectClip) uiAudio.PlayOneShot(selectClip); }

    // ----- Acciones -----
    void OnAttack()
    {
        SelectSfx();
        Close();
        onAttack?.Invoke(); // => TurnController llama a BattleDirector.BeginTargeting(...)
    }
    void OnMagic()
    {
        SelectSfx();
        Close();
        if (onMagic != null) onMagic.Invoke();
        else if (freeMove) freeMove.ResumeFromMenu();
    }
    void OnItem()
    {
        SelectSfx();
        Close();
        if (onItem != null) onItem.Invoke();
        else if (freeMove) freeMove.ResumeFromMenu();
    }
    void OnStay()
    {
        SelectSfx();
        Close();
        if (onStay != null) onStay.Invoke();
        else onStayEndTurn?.Invoke();
    }
}
