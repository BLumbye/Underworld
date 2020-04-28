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

    IEnumerator Unpause() {
        DOTween.CompleteAll();
        DOTween.To(() => Time.timeScale, x => Time.timeScale = x, 1f, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        relicShowcase.GetComponent<CanvasGroup>().DOFade(0f, 0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
        paused = false;
        yield return new WaitForSeconds(0.1f);
        relicShowcase.SetActive(false);
    }
}
