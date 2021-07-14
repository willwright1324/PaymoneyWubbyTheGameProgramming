using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoViews : MonoBehaviour {
    /* CameraViews object must be in the scene with cameras (as children that are disabled) that have CameraViewers attached
     * if you want them to be for an intro.
     * Disable CameraViews to disable the sequence when hitting play
     */

    public Camera[] camerasToUse;
    bool isLoaded;
    public bool done;

    void Start() {
        TextAsset[] datas = new TextAsset[camerasToUse.Length];

        for (int i = 0; i < camerasToUse.Length; i++) {
            CameraViewer cv = camerasToUse[i].GetComponent<CameraViewer>();
            TextAsset file = Resources.Load(cv.path + cv.fileName) as TextAsset;

            cv.preloadedLines = file.text.Split("\n"[0]);

            string[] data = cv.preloadedLines[0].Split('|');

            string[] posData = data[1].Split(',');
            cv.startPos = new Vector3(float.Parse(posData[0]), float.Parse(posData[1]), float.Parse(posData[2]));

            string[] rotData = data[2].Split(',');
            cv.startRot = new Vector3(float.Parse(rotData[0]), float.Parse(rotData[1]), float.Parse(rotData[2]));

            cv.startFov = float.Parse(data[3]);
        }
        isLoaded = true;
    }

    public void DoViewSequence() {
        StartCoroutine(ViewSequence());
        GlobalController.Instance.SetFade(true);
    }
    IEnumerator ViewSequence() {
        yield return new WaitUntil(() => isLoaded);

        GlobalController.Instance.canPause = false;
        done = false;

        GlobalController.Instance.SetPlayerMovement(false);
        GlobalController.Instance.SetUIs(false);
        Camera.main.GetComponent<AudioListener>().enabled = false;
        
        for (int i = 0; i < camerasToUse.Length; i++) {
            GameObject cam = camerasToUse[i].gameObject;
            CameraViewer cv = cam.GetComponent<CameraViewer>();
            cam.SetActive(true);
            cam.transform.position = cv.startPos;
            cam.transform.eulerAngles = cv.startRot;
            cam.GetComponent<Camera>().fieldOfView = cv.startFov;

            GlobalController.Instance.DoFade(false);

            yield return new WaitUntil(() => !GlobalController.Instance.fadeActive);

            cv.canStart = true;

            yield return new WaitUntil(() => cv.isFinished || done);
            
            GlobalController.Instance.DoFade(true);

            yield return new WaitUntil(() => GlobalController.Instance.fadeActive);

            camerasToUse[i].gameObject.SetActive(false);
            if (done)
                break;
        }
        Camera.main.GetComponent<AudioListener>().enabled = true;
        GlobalController.Instance.SetUIs(true);
        GlobalController.Instance.SetPlayerMovement(true);
        GlobalController.Instance.DoFade(false);

        yield return new WaitUntil(() => !GlobalController.Instance.fadeActive);

        GlobalController.Instance.canPause = true;
        done = true;
    }
}
