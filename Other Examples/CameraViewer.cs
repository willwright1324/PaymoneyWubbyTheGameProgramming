using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraViewer : MonoBehaviour {
    /* The camera this is attached to must be a child of CameraViews, disabled, and in its list to be used in an intro.
     * The camera can also be used seperately in an InteractableController the same as the other cameras (Must also have a cameraLookTime).
     * To test the camera view seperately disable CameraViews and CameraOperator and remove it as a child from CameraViews.
     */
    public string fileName; // File to read from.

    [HideInInspector] public bool canStart;
    [HideInInspector] public bool isFinished;
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public Vector3 startRot;
    [HideInInspector] public float startFov;

    [HideInInspector] public string path = "CameraOperator/CameraData/";
    [HideInInspector] public string[] preloadedLines;
    [HideInInspector] public string[] lines;
    int lineNum = 0;
    Camera cam;
    float currentTime;
    
    void Start() {
        cam = GetComponent<Camera>();
        lines = preloadedLines;
        if (lines.Length == 0) {
            TextAsset file = Resources.Load(path + fileName) as TextAsset;
            lines = file.text.Split("\n"[0]);
            canStart = true;
        }
    }

    private void Update() {
        if (!canStart)
            return;

        currentTime += Time.deltaTime;
        int index = (int)(currentTime * 1000);

        lineNum++;
        if (lineNum >= lines.Length - 1) {
            isFinished = true;
            return;
        }

        string[] data = lines[lineNum].Split('|');

        while (int.Parse(data[0]) < index && lineNum < lines.Length - 1) {
            lineNum++;
            if (lineNum < lines.Length - 1)
                data = lines[lineNum].Split('|');
        }

        string[] posData = data[1].Split(',');
        Vector3 pos = new Vector3(float.Parse(posData[0]), float.Parse(posData[1]), float.Parse(posData[2]));

        string[] rotData = data[2].Split(',');
        Vector3 rot = new Vector3(float.Parse(rotData[0]), float.Parse(rotData[1]), float.Parse(rotData[2]));

        float fov = float.Parse(data[3]);

        transform.position = pos;
        transform.eulerAngles = rot;
        cam.fieldOfView = fov;
    }
}
