using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour {
    [Scene]
    [SerializeField] public int gameScene;

    void Start() {
        DOTween.Init(true, true, LogBehaviour.Default);
    }

    public void Play() {
        SceneManager.LoadScene(gameScene);
    }

    public void Quit() {
        Application.Quit();
    }
}
