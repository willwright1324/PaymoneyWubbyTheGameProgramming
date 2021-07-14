using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelect : MonoBehaviour {
    public GameObject menuUI;

    private void OnTriggerStay(Collider other) {
        if (!menuUI.activeSelf) {
            GlobalController.Instance.promptUI.SetActive(true);
            if (Input.GetButton("Interact")) {
                GlobalController.Instance.SetMenuMode(true);
                GlobalController.Instance.promptUI.SetActive(false);
                menuUI.SetActive(true);
                for (int i = 1; i <= GlobalController.Instance.gameData.levelsUnlocked; i++)
                    GameObject.Find("MenuUI/Level " + i).GetComponentInChildren<Text>().text = "Level " + i;
            }
        }
        else {
            if (Input.GetButton("Cancel")) {
                GlobalController.Instance.SetMenuMode(false);
                menuUI.SetActive(false);
            }
        }
    }
    private void OnTriggerExit(Collider other) {
        GlobalController.Instance.promptUI.SetActive(false);
    }

    public void SelectLevel() {
        string buttonName = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name;
        int level = int.Parse(buttonName.Substring(buttonName.Length - 1));
        
        if (GlobalController.Instance.gameData.levelsUnlocked >= level) {
            GlobalController.Instance.SetMenuMode(false);
            GlobalController.Instance.LoadScene("Level" + level);
        }
    }
}
