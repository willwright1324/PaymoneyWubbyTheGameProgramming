using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestObject : MonoBehaviour {
    public ChainTrigger trigger;

    void Awake() {
        GlobalController.Instance.QuestTrackerScript.objects.Add(gameObject);
        gameObject.SetActive(trigger.questTriggered);
    }
}
