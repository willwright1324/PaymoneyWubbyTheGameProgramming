using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestTracker : MonoBehaviour {
    public List<GameObject> objects;
    public ChainTrigger[] triggers;
    public static Dictionary<string, List<ChainTrigger>> activateInScene = new Dictionary<string, List<ChainTrigger>>();

    void Start() {
        triggers = Resources.LoadAll<ChainTrigger>("ChainResources/Triggers");
        SaveChainData.Save("OriginalChainData");
    }
    private void OnDisable() { SaveChainData.Load("OriginalChainData"); }

    public ChainTrigger GetTrigger(string triggerName) {
        foreach (ChainTrigger ct in triggers) {
            if (ct.name == triggerName)
                return ct;
        }
        return null;
    }

    public bool IsTriggerActivated(string triggerName) {
        ChainTrigger ct = GetTrigger(triggerName);
        if (ct != null)
            return ct.triggered;
        return false;
    }
    public bool HasTriggerActivated(string triggerName) {
        ChainTrigger ct = GetTrigger(triggerName);
        if (ct != null)
            return ct.hasTriggered;
        return false;
    }

    public void ActivateTrigger(string triggerName) {
        ChainTrigger ct = GetTrigger(triggerName);
        if (ct != null)
            ActivateTrigger(ct);
    }
    public void ActivateTrigger(ChainTrigger trigger) {
        if (trigger.questTriggered && (trigger.triggerCount >= trigger.triggerLimit && trigger.triggerLimit != 0))
            return;

        trigger.triggered = trigger.hasTriggered = true;

        if (trigger.sceneToActivateIn != null && trigger.sceneToActivateIn != "") {
            if (activateInScene.ContainsKey(trigger.sceneToActivateIn)) {
                List<ChainTrigger> ctList = activateInScene[trigger.sceneToActivateIn];

                if (trigger.sceneToActivateIn != SceneManager.GetActiveScene().name) {
                    if (ctList.Contains(trigger))
                        return;
                    else {
                        ctList.Add(trigger);
                        return;
                    }
                }
                else
                    ctList.Remove(trigger);
            }
            else {
                List<ChainTrigger> ctList = new List<ChainTrigger> { trigger };
                activateInScene.Add(trigger.sceneToActivateIn, ctList);

                if (trigger.sceneToActivateIn != SceneManager.GetActiveScene().name)
                    return;
            }
        }
        
        string[] objectNames = trigger.objectsToActivate;
        Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();
        if (objectNames != null) {
            foreach (string s in objectNames) {
                foreach (GameObject go in this.objects) {
                    if (go.name == s) {
                        objects.Add(s, go);
                        break;
                    }
                }
            }
        }
        
        switch (trigger.name) {
            case "ShowTrainInHub":
                objects["TrainFront"].SetActive(true);
                break;
        }
        
        trigger.triggered = trigger.questTriggered = (++trigger.triggerCount >= trigger.triggerLimit && trigger.triggerLimit != 0);
    }
}
