using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuController : MonoBehaviour {
    public GameObject confirmation;

    void Start() {
        GlobalController.Instance.Init();
        GlobalController.Instance.canPause = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void MenuPlay() {
        GlobalController.Instance.MenuPlay();
        GlobalController.Instance.canPause = true;
    }
    public void QuitGame() {
        GlobalController.Instance.QuitGame();
    }
    public void DeleteSave() {
        confirmation.SetActive(true);
    }
    public void YesDelete() {
        GlobalController.Instance.DeleteSave();
        confirmation.SetActive(false);
    }
    public void NoDelete() {
        confirmation.SetActive(false);
    }
}
