using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InteractableController : MonoBehaviour {
    /* All fields are to be set in the inspector.
     * You may ommit any areas by making their size 0.
     * If there is only one time length when using multiple cameras they will all use that one, otherwise the size must be the same for each.
     * The same goes for activation offsets.
     * Offsets are relative to the one before it in the list, not the start of the camera sequence.
     * Objects that are disabled at start up will be "spawned in".
     * Group objects must have the same objects in each AsGroup section as each other.
     */ 

    public enum ActivationType { PLAYER_HIT, PLAYER_PROMPT, DIALOGUE_ONLY }
    [Header("General")]
    public ActivationType activationType = ActivationType.PLAYER_HIT; // Interaction trigger zone must be added/adjusted depending on prompt or hit.
    public int groupID; // Objects with the same ID (except 0) need to all be activated in order to execute anything with AsGroup.
    public bool silentInteraction; // True: Will activate the interactions without playing an animation or sound on the activated object.
    public bool triggersMultipleTimes;
    public AudioClip soundEffect;
    public Collider trigger;

    [System.Serializable]
    public class Interaction {
        [Header("Activation")]
        public bool alsoActivateSpawnedObjects;
        public GameObject[] objectsToActivate = new GameObject[1];
        public float[] objectActivationOffsetsInSeconds;
        public Camera cameraToActivateAt;
        [Header("Activation As Group")]
        public bool alsoActivateSpawnedObjectsAsGroup;
        public GameObject[] objectsToActivateAsGroup;
        public float[] objectActivationOffsetsInSecondsAsGroup;
        public Camera cameraToActivateAtAsGroup;
    }
    public Interaction[] interactions = new Interaction[1];

    [Header("Cameras")]
    public Camera[] camerasToUse;
    public float[] cameraLookTimesInSeconds = { 4.5f };
    [Header("Cameras As Group")]
    public Camera[] camerasToUseAsGroup;
    public float[] cameraLookTimesInSecondsAsGroup = { 4.5f };

    [Header("Dialogue")]
    public ChainTrigger[] triggersThatActivateThis;
    public ChainTrigger[] triggersToActivate;
    
    Camera currentCamera;
    float currentCameraLookTime;
    [HideInInspector]
    public bool isActivated;
    bool canActivate = true;
    bool groupFinished;
    bool interactionFinished;
    private Animator anim;
    private AudioSource audioSource;
    
    public static List<GameObject> interactableList;
    List<EnemyAI> enemies;

    void Start() {
        audioSource = gameObject.GetComponent<AudioSource>();
        anim = gameObject.GetComponent<Animator>();
        if (audioSource != null)
            audioSource.clip = soundEffect;

        if (groupID != 0) {
            if (interactableList == null)
                interactableList = new List<GameObject>();
            interactableList.Add(gameObject);
        }
        
        int index = SceneManager.GetActiveScene().buildIndex;
        if (interactions.Length > 0) {
            foreach (Interaction inter in interactions) {
                if (inter.objectsToActivate.Length > 0) {
                    foreach (GameObject go in inter.objectsToActivate) {
                        if (!go.activeSelf) {
                            if (GlobalController.Instance.gameData.levelObjects != null && GlobalController.Instance.gameData.levelObjects[index].Contains(go.name))
                                go.SetActive(true);
                            else
                                continue;
                        }
                        Animator a = go.GetComponent<Animator>();
                        if (a != null && a.parameters != null) {
                            foreach (AnimatorControllerParameter acp in a.parameters) {
                                if (acp.name == "Interact")
                                    a.SetBool("Interact", GlobalController.Instance.gameData.levelObjects != null && GlobalController.Instance.gameData.levelObjects[index].Contains(go.name));
                            }
                        }
                    }
                }
                if (inter.objectsToActivateAsGroup.Length > 0) {
                    foreach (GameObject go in inter.objectsToActivateAsGroup) {
                        if (!go.activeSelf) {
                            if (GlobalController.Instance.gameData.levelObjects != null && GlobalController.Instance.gameData.levelObjects[index].Contains(go.name))
                                go.SetActive(true);
                            else
                                continue;
                        }
                        Animator a = go.GetComponent<Animator>();
                        if (a != null && a.parameters != null) {
                            foreach (AnimatorControllerParameter acp in a.parameters) {
                                if (acp.name == "Interact")
                                    a.SetBool("Interact", GlobalController.Instance.gameData.levelObjects != null && GlobalController.Instance.gameData.levelObjects[index].Contains(go.name));
                            }
                        }
                    }
                }
            }
        }

        if (triggersThatActivateThis.Length > 0)
            DialogueController.instance.interactionList.Add(gameObject);
    }
    private void OnDisable() {
        if (interactableList != null)
            interactableList = null;
    }

    public void OnTriggerStay(Collider other) {
        if (isActivated && !canActivate)
            return;

        if (activationType == ActivationType.PLAYER_PROMPT) {
            if (other.gameObject.tag == "Player") {
                GlobalController.Instance.promptUI.SetActive(true);
                if (Input.GetButton("Interact")) {
                    GlobalController.Instance.promptUI.SetActive(false);
                    Activate();
                }
            }
        }
        else {
            if (activationType == ActivationType.PLAYER_HIT) {
                if (other.gameObject.tag == "PlayerAttack")
                    Activate();
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        if (activationType == ActivationType.PLAYER_PROMPT)
            GlobalController.Instance.promptUI.SetActive(false);
    }

    public void DisableAnim() {
        anim.SetBool("Interact", false);
        audioSource.Stop();
    }

    public void Activate() {
        int index = SceneManager.GetActiveScene().buildIndex;
        bool startActivated = GlobalController.Instance.gameData.levelObjects != null && GlobalController.Instance.gameData.levelObjects[index].Contains(gameObject.name);

        if (interactionFinished && triggersMultipleTimes) {
            canActivate = true;
            groupFinished = false;
            interactionFinished = false;
        }

        if (isActivated && !canActivate)
            return;

        isActivated = true;
        canActivate = false;
        if (!startActivated)
            GlobalController.Instance.AddObjectToList(gameObject.name);

        if (!silentInteraction) {
            Animator a = GetComponent<Animator>();
            if (a != null && a.parameters != null) {
                foreach (AnimatorControllerParameter acp in a.parameters) {
                    if (acp.name == "Interact")
                        a.SetBool("Interact", true);
                }
            }
            if (audioSource != null && !startActivated)
                audioSource.Play();
        }

        if (trigger != null)
            trigger.enabled = false;
        
        StartChainOnInteraction scoi = GetComponent<StartChainOnInteraction>();
        if (scoi != null)
            scoi.StopAllCoroutines();

        if (triggersToActivate.Length > 0) {
            foreach (ChainTrigger ct in triggersToActivate)
                ct.triggered = true;
        }

        if (!startActivated) {
            if (groupID != 0) {
                if (IsGroupActivated()) {
                    StartCoroutine(EnableGroupInteraction());
                    return;
                }
                else {
                    // Notify player of other activators somehow?
                }
            }
            EnableInteraction();
        }
    }

    bool IsGroupActivated() {
        if (groupID == 0)
            return false;

        foreach (GameObject go in interactableList) {
            InteractableController ic = go.GetComponent<InteractableController>();
            if (ic.groupID == groupID && !ic.isActivated)
                return false;
        }
        return true;
    }

    IEnumerator EnableGroupInteraction() {
        if (interactions.Length > 0) {
            foreach (Interaction inter in interactions) {
                if (inter.objectsToActivateAsGroup.Length > 0)
                    StartCoroutine(ActivateObjects(inter.alsoActivateSpawnedObjectsAsGroup, inter.objectsToActivateAsGroup, 
                                                   inter.objectActivationOffsetsInSecondsAsGroup, inter.cameraToActivateAtAsGroup));
            }
        }
        if (camerasToUseAsGroup.Length > 0)          
            StartCoroutine(CameraSequence(camerasToUseAsGroup, cameraLookTimesInSecondsAsGroup));
        else
            groupFinished = true;

        yield return new WaitUntil(() => groupFinished);

        EnableInteraction();
    }

    void EnableInteraction() {
        if (interactions.Length > 0) {
            foreach (Interaction inter in interactions) {
                if (inter.objectsToActivate.Length > 0)
                    StartCoroutine(ActivateObjects(inter.alsoActivateSpawnedObjects, inter.objectsToActivate, 
                                                   inter.objectActivationOffsetsInSeconds, inter.cameraToActivateAt));
            }
        }
        if (camerasToUse.Length > 0)
            StartCoroutine(CameraSequence(camerasToUse, cameraLookTimesInSeconds));
        else
            interactionFinished = true;
    }

    IEnumerator ActivateObjects(bool alsoActivateSpawnedObjects, GameObject[] objectsToActivate, 
                                float[] objectActivationOffsetsInSeconds, Camera cameraToActivateAt) {

        if (cameraToActivateAt != null) {
            yield return new WaitUntil(() => cameraToActivateAt == currentCamera);

            float activationWaitTime = 0;
            if (objectActivationOffsetsInSeconds.Length == 0)
                activationWaitTime = currentCameraLookTime / 2;
            else {
                float totalOffsetTime = 0;
                foreach (float f in objectActivationOffsetsInSeconds)
                    totalOffsetTime += f;

                activationWaitTime = (currentCameraLookTime - totalOffsetTime) / 2;
                if (activationWaitTime < 0)
                    activationWaitTime = 0;
            }
            
            yield return new WaitForSeconds(activationWaitTime);
        }

        for (int i = 0; i < objectsToActivate.Length; i++) {
            int offsetLength = objectActivationOffsetsInSeconds.Length;
            float offset = offsetLength == 0 || i == 0 ? 0 : (offsetLength == 1 ? objectActivationOffsetsInSeconds[0] : objectActivationOffsetsInSeconds[i]);

            yield return new WaitForSeconds(offset);

            GameObject go = objectsToActivate[i];
            GlobalController.Instance.AddObjectToList(go.name);

            if (!go.activeSelf) {
                if (triggersMultipleTimes) {
                    objectsToActivate[i] = Instantiate(go, go.transform);
                    objectsToActivate[i].transform.position = go.transform.position;
                    objectsToActivate[i].transform.SetParent(go.transform.parent);
                }
                go.SetActive(true);
                //Do particle and other spawn stuff later
            }

            if (go.activeSelf || alsoActivateSpawnedObjects) {
                Animator a = go.GetComponent<Animator>();
                if (a != null && a.parameters != null) {
                    foreach (AnimatorControllerParameter acp in a.parameters) {
                        if (acp.name == "Interact")
                            a.SetBool("Interact", true);
                    }
                }
            }

            if (go.tag == "Enemy") {
                if (enemies == null)
                    enemies = new List<EnemyAI>();

                EnemyAI eai = go.GetComponent<EnemyAI>();
                enemies.Add(eai);
                eai.enabled = false;
            }
        }
    }

    IEnumerator CameraSequence(Camera[] camerasToUse, float[] cameraLookTimesInSeconds) {
        int fadeState = 0;

        GlobalController.Instance.SetPlayerMovement(false);
        GlobalController.Instance.canPause = false;

        if (fadeState == 0) {
            GlobalController.Instance.DoFade(true);
            yield return new WaitUntil(() => GlobalController.Instance.fadeActive);

            Camera.main.GetComponent<AudioListener>().enabled = false;
            GlobalController.Instance.SetUIs(false);
        }

        GlobalController.Instance.SetPlayerMovement(false);
        for (int i = 0; i < camerasToUse.Length; i++) {
            camerasToUse[i].gameObject.SetActive(true);
            CameraViewer cv = camerasToUse[i].GetComponent<CameraViewer>();

            int j = cameraLookTimesInSeconds.Length == 1 ? 0 : i;

            fadeState = 1;
            GlobalController.Instance.DoFade(false);

            yield return new WaitUntil(() => !GlobalController.Instance.fadeActive && fadeState == 1);

            fadeState = 2;
            currentCamera = camerasToUse[i];
            currentCameraLookTime = cv == null ? cameraLookTimesInSeconds[j] : float.Parse(cv.lines[0]);

            yield return new WaitForSeconds(currentCameraLookTime);

            GlobalController.Instance.DoFade(true);

            yield return new WaitUntil(() => GlobalController.Instance.fadeActive && fadeState == 2);

            camerasToUse[i].gameObject.SetActive(false);
        }
        Camera.main.GetComponent<AudioListener>().enabled = true;

        if (!IsGroupActivated() || groupFinished) {
            fadeState = 3;
            GlobalController.Instance.SetUIs(true);
            GlobalController.Instance.DoFade(false);

            yield return new WaitUntil(() => !GlobalController.Instance.fadeActive && fadeState == 3);

            GlobalController.Instance.SetPlayerMovement(true);
            GlobalController.Instance.canPause = true;

            if (enemies != null) {
                foreach (EnemyAI eai in enemies)
                    eai.enabled = true;
                enemies = null;
            }

            if (trigger != null)
                trigger.enabled = true;

            canActivate = triggersMultipleTimes;
            currentCamera = null;
            currentCameraLookTime = 0;
            interactionFinished = true;
        }
        groupFinished = true;
    }
}
