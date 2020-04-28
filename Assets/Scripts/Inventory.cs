using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Inventory : MonoBehaviour {
    // The inventory stores the owned relics and currency
    public static Inventory Instance;

    public int currency = 0;
    [Header("Relics")]
    [SerializeField] private List<Relic> relics;

    [Header("UI")]
    [SerializeField] private GameObject relicShowcase;
    [SerializeField] private Image relicIcon;
    [SerializeField] private TextMeshProUGUI relicName;
    [SerializeField] private TextMeshProUGUI relicDescription;
    [SerializeField] private TextMeshProUGUI relicContinue;

    [HideInInspector] public bool paused = false;

    private PlayerInput playerInput;
    private string relicContinueDefault;

    [HideInInspector] public string signText = "";
    [HideInInspector] public Vector2 signPosition;
    public TextMeshProUGUI signTextUI;
    public RectTransform signCanvas;

    private float camSize = 5.625f;

    private Regex inputRegex = new Regex(@"\$(.+)\$");

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            enabled = false;
        }
    }

    void Start() {
        playerInput = GetComponent<PlayerInput>();
        relicContinueDefault = relicContinue.text;
    }

    void LateUpdate() {
        if (signText != "") {
            signCanvas.gameObject.SetActive(true);
            signTextUI.text = signText;
            signCanvas.position = signPosition + Vector2.up * 2.5f;

            signText = "";
            signPosition = Vector2.zero;
        } else {
            signCanvas.gameObject.SetActive(false);
        }
    }

    public Relic GetRelic(string name) {
        return relics.Find(r => r.name == name);
    }

    public bool HasRelic(string name) {
        return GetRelic(name).owned;
    }

    public void GainRelic(string name) {
        GetRelic(name).owned = true;

        Relic relic = GetRelic(name);
        relicShowcase.SetActive(true);
        relicIcon.sprite = relic.icon;
        relicName.text = relic.name;
        relicDescription.text = ConvertControlInString(relic.description);
        relicContinue.text = ConvertControlInString(relicContinueDefault);

        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 0f, 0.75f).SetEase(Ease.InCubic).SetUpdate(true);
        relicShowcase.GetComponent<CanvasGroup>().DOFade(1f, 0.75f).SetEase(Ease.InCubic).SetUpdate(true);
        paused = true;
    }

    public string ConvertControlInString(string original) {
        Match match = inputRegex.Match(original);
        if (match.Success) {
            InputAction action = playerInput.currentActionMap.FindAction(match.Groups[1].Value);
            return inputRegex.Replace(original, action.GetBindingDisplayString());
        }

        return original;
    }

    public void OnJumpInput(InputAction.CallbackContext ctx) {
        if (ctx.performed && ctx.ReadValue<float>() <= 0.5f && paused) {
            StartCoroutine(Unpause());
        }
    }

    public void ChangeCameraSize(float newSize) {
        if (camSize != newSize) {
            camSize = newSize;
            Camera.main.DOOrthoSize(newSize, 0.5f).SetEase(Ease.InOutCubic);
        }
    }

    IEnumerator Unpause() {
        DOTween.CompleteAll();
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        relicShowcase.GetComponent<CanvasGroup>().DOFade(0f, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        paused = false;
        yield return new WaitForSeconds(0.1f);
        relicShowcase.SetActive(false);
    }
}
