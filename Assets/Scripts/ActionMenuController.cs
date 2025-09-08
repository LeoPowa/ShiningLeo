// ActionMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ActionMenuController : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;       // Panel raíz del menú
    public Button btnAttack;
    public Button btnMagic;
    public Button btnItem;
    public Button btnStay;

    [Header("Input")]
    public InputActionAsset input;
    public string actionMap = "Gameplay";
    public string cancelAction = "Cancel";

    [Header("SFX (opcional)")]
    public AudioSource uiAudio;
    public AudioClip openClip, selectClip, cancelClip, moveClip;

    Unit currentUnit;
    System.Action onEndTurn;
    FreeMoveController freeMove;

    InputAction aCancel;
    bool isOpen;

    void Awake()
    {
        if (panel) panel.SetActive(false);
    }

    void OnEnable()
    {
        var map = input.FindActionMap(actionMap, true);
        aCancel = map.FindAction(cancelAction, true);
    }

    public void Open(Unit unit, System.Action onEndTurnCallback, FreeMoveController freeMoveRef)
    {
        currentUnit = unit;
        onEndTurn = onEndTurnCallback;
        freeMove = freeMoveRef;

        isOpen = true;
        if (panel) panel.SetActive(true);

        // foco UI
        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        EventSystem.current.SetSelectedGameObject(btnAttack.gameObject);

        // sonidos
        if (uiAudio && openClip) uiAudio.PlayOneShot(openClip);

        // listeners
        btnAttack.onClick.AddListener(OnAttack);
        btnMagic.onClick.AddListener(OnMagic);
        btnItem.onClick.AddListener(OnItem);
        btnStay.onClick.AddListener(OnStay);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        if (panel) panel.SetActive(false);

        btnAttack.onClick.RemoveListener(OnAttack);
        btnMagic.onClick.RemoveListener(OnMagic);
        btnItem.onClick.RemoveListener(OnItem);
        btnStay.onClick.RemoveListener(OnStay);
    }

    void Update()
    {
        if (!isOpen) return;

        // cancelar menú → volver a mover libremente
        if (aCancel.WasPerformedThisFrame())
        {
            if (uiAudio && cancelClip) uiAudio.PlayOneShot(cancelClip);
            Close();
            freeMove.ResumeFromMenu();
        }
    }

    void PlaySelect() { if (uiAudio && selectClip) uiAudio.PlayOneShot(selectClip); }

    void OnAttack()
    {
        PlaySelect();
        // TODO: aquí abrirías selección de objetivo (por ahora, cerrar y volver a mover o terminar flujo)
        Close();
        freeMove.ResumeFromMenu(); // de momento vuelve al movimiento tras ver el menú
        // Alternativa: pasar a modo "targeting" inmediatamente si quieres.
    }

    void OnMagic()
    {
        PlaySelect();
        // TODO: abrir submenú de hechizos
        Close();
        freeMove.ResumeFromMenu();
    }

    void OnItem()
    {
        PlaySelect();
        // TODO: abrir inventario de la unidad
        Close();
        freeMove.ResumeFromMenu();
    }

    void OnStay()
    {
        PlaySelect();
        Close();
        // Termina turno (como SF2)
        onEndTurn?.Invoke();
    }
}
