#if (UNITY_EDITOR) 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEditor.AssetImporters;

public class CameraOperator : MonoBehaviour {
    /* If the CameraOperator object is enabled, the playermovement/UI/CameraViews will be disabled.
     * Controls initially set for Xbox One Controller on Windows 10.
     */

    public string fileName; // File to save to.
    public bool invertVerticalRotation;
    public bool alwaysRecordWhenNotMoving; // False: If the camera is not moving it will pause
                                           // recording unless the designated button is held
    public bool useSmoothCamera = true; // False: Uses exact input instead of deceleration
    public float movementSensitivity = 1f;
    public float rotationSensitivity = 1f;
    public float zoomSensitivity = 1f;
    public float movementDeceleration = 1f;
    public float rotationDeceleration = 1f;
    float movementX;
    float movementY;
    float movementZ;
    float rotationX;
    float rotationY;
    float[] sensitivities = new float[3];

    enum CameraState { READY, RECORDING, PAUSED, SAVING }
    CameraState camState = CameraState.READY;
    enum SaveState { PROMPT, CHECKING, SAVING, SAVED }
    SaveState saveState = SaveState.PROMPT;
    Text controlsMenu;
    Text controls;
    Text status;
    Text time;
    Text save;

    Camera cam;
    string path = "Assets/Resources/CameraOperator/CameraData/";
    int verticalRotation;
    float currentTime;
    float initialFOV;
    bool pause;
    List<string> data;
    
    void Start() {
        Camera.main.GetComponent<AudioListener>().enabled = false;
        GlobalController.Instance.SetPlayerMovement(false);
        GlobalController.Instance.CameraControllerScript.enabled = false;
        GameObject cv = GameObject.Find("CameraViews");
        if (cv != null)
            cv.SetActive(false);

        cam = GetComponentInChildren<Camera>();
        initialFOV = cam.fieldOfView;
        data = new List<string>();

        controlsMenu = GameObject.Find("CameraOperator/OperatorCanvas/ControlsMenu").GetComponent<Text>();
        controls     = GameObject.Find("CameraOperator/OperatorCanvas/ControlsMenu/Controls").GetComponent<Text>();
        status       = GameObject.Find("CameraOperator/OperatorCanvas/Status").GetComponent<Text>();
        time         = GameObject.Find("CameraOperator/OperatorCanvas/Time").GetComponent<Text>();
        save         = GameObject.Find("CameraOperator/OperatorCanvas/Save").GetComponent<Text>();
        save.enabled = false;

        verticalRotation = invertVerticalRotation ? 1 : -1;
        sensitivities[0] = movementSensitivity * 15;
        sensitivities[1] = rotationSensitivity * 45;
        sensitivities[2] = zoomSensitivity * 10;

        Invoke("SceneSetup", 0.1f);
    }
    void SceneSetup() { GlobalController.Instance.SetUIs(false); }

    void Update() {
        if (Input.GetButtonDown("Controller Back")) {
            controls.enabled = !controls.enabled;
            controlsMenu.text = controls.enabled ? "Controls: (Back - Hide)" : "Controls: (Back - Show)";
        }

        if (camState == CameraState.READY || camState == CameraState.RECORDING) {
            if (useSmoothCamera) {
                movementX = GetInputValue(Input.GetAxis("Horizontal"), sensitivities[0], movementX, movementDeceleration);
                movementY = GetInputValue(Input.GetAxis("Controller Trigger"), sensitivities[0], movementY, movementDeceleration);
                movementZ = GetInputValue(Input.GetAxis("Vertical"), sensitivities[0], movementZ, movementDeceleration);

                rotationX = GetInputValue(Input.GetAxis("Mouse Y"), sensitivities[1], rotationX, rotationDeceleration);
                rotationY = GetInputValue(Input.GetAxis("Mouse X"), sensitivities[1], rotationY, rotationDeceleration);
            }
            else {
                movementX = Input.GetAxis("Horizontal") * sensitivities[0];
                movementY = Input.GetAxis("Controller Trigger") * sensitivities[0];
                movementZ = Input.GetAxis("Vertical") * sensitivities[0];

                rotationX = Input.GetAxis("Mouse Y") * sensitivities[1];
                rotationY = Input.GetAxis("Mouse X") * sensitivities[1];
            }

            transform.Translate(new Vector3(movementX, movementY, movementZ) * Time.deltaTime, Space.Self);
            transform.Rotate(new Vector3(rotationX, 0, 0) * Time.deltaTime * verticalRotation, Space.Self);
            transform.Rotate(new Vector3(0, rotationY, 0) * Time.deltaTime, Space.World);

            if (Input.GetButton("Controller Left Bumper") && Input.GetButton("Controller Right Bumper")) {
                float remainder = cam.fieldOfView - initialFOV;

                if (Mathf.Abs(remainder) < 0.1f) cam.fieldOfView = initialFOV;
                else                             cam.fieldOfView += Time.deltaTime * sensitivities[2] * (remainder < 0 ? 1 : -1);
            }
            else {
                if (Input.GetButton("Controller Left Bumper"))  cam.fieldOfView += Time.deltaTime * sensitivities[2];
                if (Input.GetButton("Controller Right Bumper")) cam.fieldOfView -= Time.deltaTime * sensitivities[2];
            }

            if (camState == CameraState.READY) {
                status.text = "Status: Ready To Record";

                if (Input.GetButtonDown("Controller Start")) {
                    camState = CameraState.RECORDING;
                    return;
                }
            }

            if (camState == CameraState.RECORDING) {
                if (Input.GetButtonDown("Controller Start")) {
                    camState = CameraState.SAVING;
                    saveState = SaveState.PROMPT;
                }

                if (!alwaysRecordWhenNotMoving) {
                    pause = (movementX == 0
                          && movementY == 0
                          && movementZ == 0
                          && rotationX == 0
                          && rotationY == 0
                          && !Input.GetButton("Controller Left Bumper")
                          && !Input.GetButton("Controller Right Bumper")
                          && !Input.GetButton("Fire1"));
                }

                if (!pause)
                    WriteData();

                status.text = pause ? "Status: Paused" : "Status: Recording";
            }
        }
        if (camState == CameraState.SAVING) {
            status.text = "Status: Not Recording";

            if (saveState == SaveState.PROMPT) {
                if (!save.enabled) {
                    save.enabled = true;
                    save.text = "Save?\nA - Save\nX - Discard\nB - Cancel";
                }

                if (Input.GetButtonDown("Fire1")) {
                    save.enabled = false;
                    saveState = SaveState.CHECKING;
                    return;
                }
                else {
                    if (Input.GetButtonDown("Fire2")) {
                        save.enabled = false;
                        camState = CameraState.RECORDING;
                        return;
                    }
                    else {
                        if (Input.GetButtonDown("Fire3")) {
                            save.enabled = false;
                            camState = CameraState.READY;
                            currentTime = 0;
                            time.text = "Elapsed Time: 0.00";
                            data = new List<string>();
                            return;
                        }
                    }
                }
            }

            if (saveState == SaveState.CHECKING) {
                if (!save.enabled) {
                    save.enabled = true;

                    if (File.Exists(path + fileName + ".txt"))
                        save.text = fileName + " already exists.\nA - Save New\nB - Overwrite";
                    else
                        saveState = SaveState.SAVING;
                }

                if (Input.GetButtonDown("Fire1")) {
                    int end = 0;
                    while (File.Exists(path + fileName + ".txt")) {
                        if (int.TryParse(fileName.Substring(fileName.Length - 1), out int n)) {
                            end = n;
                            fileName = fileName.Substring(0, fileName.Length - 1);
                        }
                        fileName += (end + 1) + "";
                    }
                    saveState = SaveState.SAVING;
                    return;
                }
                else {
                    if (Input.GetButtonDown("Fire2"))
                        saveState = SaveState.SAVING;
                }
            }

            if (saveState == SaveState.SAVING) {
                save.text = "Saving...";
                SaveData();
                saveState = SaveState.SAVED;
                return;
            }

            if (saveState == SaveState.SAVED) {
                save.text = "File saved as " + fileName + ".\n A - OK";
                if (Input.GetButtonDown("Fire1")) {
                    save.enabled = false;
                    camState = CameraState.READY;
                    currentTime = 0;
                    time.text = "Elapsed Time: 0.00";
                    data = new List<string>();
                    return;
                }
            }
        }
    }

    float GetInputValue(float input, float sensitivity, float inputValue, float inputDeceleration) {
        if (input != 0)
            inputValue = Mathf.Lerp(inputValue, input * sensitivity, Time.deltaTime * inputDeceleration);
        else {
            if (inputValue != 0) {
                inputValue = Mathf.Lerp(inputValue, 0, Time.deltaTime * inputDeceleration);
                if (Mathf.Abs(inputValue - 0) < 0.5f)
                    inputValue = 0;
            }
        }
        return inputValue;
    }

    void WriteData() {
        if (data.Count == 0)
            data.Add("");
        currentTime += Time.deltaTime;
        time.text = "Elapsed Time: " + Mathf.Round(currentTime * 100) / 100.0f;

        Vector3 pos = transform.position;
        Vector3 rot = transform.eulerAngles;

        int index = (int)(currentTime * 1000);

        data.Add(index + "|" + pos.x + "," + pos.y + "," + pos.z + "|" + rot.x + "," + rot.y + "," + rot.z + "|" + cam.fieldOfView);
    }

    void SaveData() {
        data[0] += currentTime;
        StreamWriter sw = new StreamWriter(path + fileName + ".txt");
        foreach (string s in data)
            sw.WriteLine(s);
        sw.Close();
        UnityEditor.AssetDatabase.Refresh();
    }
}
#endif