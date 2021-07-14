using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GlobalController : MonoBehaviour {
    // Globals that can be accessed anywhere through GlobalController.Instance.VariableName
    public GameObject player = null;
    public CameraController CameraControllerScript = null;
    public LedgeClimbController LedgeClimbControllerScript = null;
    public PlayerController PlayerControllerScript = null;
    public ItemManager ItemManagerScript = null;
    public Health HealthScript = null;
    public HitboxManager HitboxManagerScript = null;
    public QuestTracker QuestTrackerScript;
    public AudioController AudioControllerScript;

    // Inventory item transfer between scenes, do not access (innaccurate until scene switch)
    public int coins = 0;
    public int keys = 0;

    // Data that is saved
    [System.Serializable]
    public class GameData {
        public int saveSlot = 1;
        public bool doneTutorial;
        public int totalCoins = 0;
        public int totalKeys = 0;
        public int[][] levelKeys;
        public List<string>[] levelObjects;
        public int levelsUnlocked = 1;
    }
    public GameData gameData;

    [System.Serializable]
    public class SettingsData {
        public float volumeMaster;
        public float volumeMusic;
        public float volumeSounds;
    }
    public SettingsData settingsData;

    // Misc
    public bool canPause = true;
    public bool isPaused;
    public bool menuActive;
    public bool fadeActive;
    bool doingFade;

    public List<GameObject> otherUIs = new List<GameObject>();
    GameObject canvas;
    public GameObject pauseUI;
    public GameObject returnButton;
    public GameObject promptUI;
    public GameObject fadeUI;
    public Animator fadeAnim;
    public Image fadeImage;
    AudioSource musicManager;
    VideoPlayer videoPlayer;
    GameObject camViews;

    // Single instance of controller
    private static GlobalController instance = null;
    public static GlobalController Instance {
        get {
            if (instance == null) {
                instance = FindObjectOfType<GlobalController>();
                if (instance == null) {
                    GameObject gc = Resources.Load("GlobalController") as GameObject;
                    gc = Instantiate(gc);
                    instance = gc.GetComponent<GlobalController>();
                    DontDestroyOnLoad(gc);
                }
            }
            return instance;
        }
    }
    void Awake() {
        if (instance == null) {
            instance = this;
            settingsData          = LoadSettings();
            QuestTrackerScript    = GetComponent<QuestTracker>();
            AudioControllerScript = GetComponent<AudioController>();
            fadeAnim              = fadeUI.GetComponentInChildren<Animator>();
            fadeImage             = fadeUI.GetComponentInChildren<Image>();
            videoPlayer           = GetComponent<VideoPlayer>();
            AudioControllerScript.Init();
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
            return;
        }
    }

    public void Init() {}

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start() {
        GetPlayerInfo();
    }

    private void Update() {
        // Pause
        if (Input.GetButtonDown("Cancel") && !menuActive && canPause) {
            if (isPaused) {
                UnPauseGame();
            }
            else {
                returnButton.SetActive(SceneManager.GetActiveScene().name != "WubHub");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                pauseUI.SetActive(true);
                GameObject.Find("PauseUI/KeyTotalText").GetComponent<Text>().text = "Total Keys: " + gameData.totalKeys + "/" 
                                                                                  + ItemManagerScript.itemList[ItemManagerScript.GetItemIndex("StreamKey")].itemGameTotal;
                Time.timeScale = 0;
                isPaused = true;
            }
        }

        // Skip Intro
        if (Input.GetButtonDown("Interact")) {
            if (camViews != null && !camViews.GetComponent<DoViews>().done)
                camViews.GetComponent<DoViews>().done = true;
        }

        //Fade status
        if (fadeImage.color.a == 0 && !doingFade) {
            fadeActive = false;
            fadeUI.SetActive(false);
        }
        else {
            if (fadeImage.color.a == 1)
                fadeActive = true;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // Check for audio sources not in a mixer group
        GameObject[] objects = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject go in objects) {
            Transform[] children = go.GetComponentsInChildren<Transform>();

            foreach (Transform t in children) {
                AudioSource audio = t.GetComponent<AudioSource>();
                if (audio != null && audio.outputAudioMixerGroup == null) {
                    string s = "The object " + t.name + " does not belong to a mixer group.";
                    Transform p = t.parent;
                    if (p != null)
                        s+= " It is under " + t.name + " < ";
                    while (p != null) {
                        s += p.name;
                        p = p.parent;
                        if (p != null)
                            s += " < ";
                    }
                    Debug.Log(s);
                }
            }
        }

        // Copies inventory into new player
        GetPlayerInfo();
        GetUIs();
        if (ItemManagerScript != null) {
            ItemManagerScript.AddItem("Coin", coins);
            if (SceneManager.GetActiveScene().name == "WubHub") {
                ItemManagerScript.inWubHub = true;
                ItemManagerScript.AddItem("StreamKey", gameData.totalKeys);
            }
        }

        // Disable already collected keys
        if (gameData.levelKeys != null) {
            string levelName = SceneManager.GetActiveScene().name;
            if (levelName.Contains("Level")) {
                int level = int.Parse(levelName[levelName.Length - 1] + "");
                if (gameData.levelKeys[level - 1] != null) {
                    ItemManagerScript.itemList[ItemManagerScript.GetItemIndex("StreamKey")].itemLevelTotal = gameData.levelKeys[level - 1].Length;
                    int amount = 0;
                    for (int i = 0; i < gameData.levelKeys[level - 1].Length; i++) {
                        if (gameData.levelKeys[level - 1][i] == 1) {
                            GameObject.Find("Key:" + (i + 1)).SetActive(false);
                            amount++;
                        }
                    }
                    ItemManagerScript.AddItem("StreamKey", amount);
                }
            }
        }

        // Intro Cameras
        camViews = GameObject.Find("CameraViews");
        GameObject camOp = GameObject.Find("CameraOperator");
        if (camViews != null && camOp == null)
            camViews.GetComponent<DoViews>().DoViewSequence();

        // Quest Tracker Triggers
        if (SceneManager.GetActiveScene().name != "Loading") {
            List<GameObject> newObjects = new List<GameObject>();
            foreach (GameObject go in QuestTrackerScript.objects) {
                if (go != null)
                    newObjects.Add(go);
            }
            QuestTrackerScript.objects = newObjects;

            if (QuestTracker.activateInScene.ContainsKey(SceneManager.GetActiveScene().name)) {
                ChainTrigger[] ctList = QuestTracker.activateInScene[SceneManager.GetActiveScene().name].ToArray();
                if (ctList != null) {
                    foreach (ChainTrigger ct in ctList)
                        QuestTrackerScript.ActivateTrigger(ct);
                }
            }
        }

        GameObject music = GameObject.Find("MusicManager");
        if (music != null)
            musicManager = music.GetComponent<AudioSource>();
    }

    // Gets player info for access
    void GetPlayerInfo() {
        player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        CameraControllerScript     = Camera.main.GetComponent<CameraController>();
        LedgeClimbControllerScript = player.GetComponent<LedgeClimbController>();
        PlayerControllerScript     = player.GetComponent<PlayerController>();
        ItemManagerScript          = player.GetComponent<ItemManager>();
        HealthScript               = player.GetComponent<Health>();
        HitboxManagerScript        = player.GetComponent<HitboxManager>();
    }

    // Loads scene with loading screen
    public void LoadScene(string sceneName) {
        if (ItemManagerScript != null) {
            coins = ItemManagerScript.CheckItemAmount("Coin");
            keys  = ItemManagerScript.CheckItemAmount("StreamKey");
        }
        StartCoroutine(DoLoadScene(sceneName));
    }
    IEnumerator DoLoadScene(string sceneName) {
        int fadeState = 0;
        DoFade(true);

        yield return new WaitUntil(() => fadeActive && fadeState == 0);

        AsyncOperation ao = SceneManager.LoadSceneAsync("Loading");

        yield return new WaitUntil(() => ao.isDone && fadeState == 0);

        fadeState = 1;
        DoFade(false);

        yield return new WaitUntil(() => !fadeActive && fadeState == 1);

        yield return new WaitForSeconds(3f);

        ao = SceneManager.LoadSceneAsync(sceneName);
        ao.allowSceneActivation = false;

        while (ao.progress < 0.9f)
            yield return null;

        DoFade(true);

        yield return new WaitUntil(() => fadeActive && fadeState == 1);
        
        ao.allowSceneActivation = true;
        GameObject camViews = GameObject.Find("CameraViews");
        if (camViews == null)
            DoFade(false);
    }

    // Enables cursor and disables pausing for menus
    public void SetMenuMode(bool enabled) {
        if (enabled) {
            Cursor.lockState = CursorLockMode.None;
            menuActive = true;
        }
        else {
            Cursor.lockState = CursorLockMode.Locked;
            Invoke("MenuCancelPauseDelay", 0.1f);
        }
        Cursor.visible = enabled;
        SetPlayerMovement(!enabled);
    }
    void MenuCancelPauseDelay() { menuActive = false; }

    void GetUIs() {
        otherUIs = new List<GameObject>();
        canvas = GameObject.Find("Canvas");
        if (canvas == null)
            return;

        Transform[] ui = canvas.GetComponentsInChildren<Transform>();
        RectTransform[] ui2 = canvas.GetComponentsInChildren<RectTransform>();
        foreach (Transform t in ui) {
            if (t.gameObject != canvas)
                otherUIs.Add(t.gameObject);
        }
        foreach (RectTransform rt in ui2) {
            if (rt.gameObject != canvas)
                otherUIs.Add(rt.gameObject);
        }
    }

    // Enable/Disable player related UIs
    public void SetUIs(bool enabled) {
        if (otherUIs.Count == 0) {
            GetUIs();
            if (otherUIs.Count == 0)
                return;
        }

        foreach (GameObject go in otherUIs)
            go.SetActive(enabled);
    }

    public void SetPlayerMovement(bool enabled) {
        CameraControllerScript.enabled = enabled;
        LedgeClimbControllerScript.enabled = enabled;
        PlayerControllerScript.enabled = enabled;
        if (!enabled)
            player.GetComponent<Animator>().SetFloat("speed", 0);
    }

    public void UnPauseGame() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pauseUI.SetActive(false);
        Time.timeScale = 1;
        isPaused = false;
    }

    public void PlayVideo(string videoClip) {
        VideoClip vc = Resources.Load<VideoClip>("Videos/" + videoClip);
        if (vc == null)
            return;

        StopCoroutine(DoPlayVideo());
        videoPlayer.Stop();
        videoPlayer.targetCamera = Camera.main;
        UnPauseGame();
        SetMenuMode(false);
        SetPlayerMovement(false);
        canPause = false;
        videoPlayer.clip = vc;
        videoPlayer.Prepare();
        StartCoroutine(DoPlayVideo());
    }
    IEnumerator DoPlayVideo() {
        yield return new WaitUntil(() => videoPlayer.isPrepared);

        int fadeState = 0;
        DoFade(true);

        yield return new WaitUntil(() => fadeActive && fadeState == 0);

        fadeState = 1;
        SetUIs(false);
        DoFade(false);
        if (musicManager != null)
            musicManager.Pause();
        videoPlayer.Play();

        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        DoFade(true);

        yield return new WaitUntil(() => fadeActive && fadeState == 1);

        videoPlayer.Stop();
        DoFade(false);
        SetPlayerMovement(true);
        SetUIs(true);
        canPause = true;
        if (musicManager != null)
            musicManager.Play();
    }

    // Start fading
    public void DoFade(bool activate) {
        if (activate)
            fadeUI.SetActive(true);
        
        fadeAnim.SetBool("Fade", activate);
        doingFade = activate;
    }
    // True: set fade to already be done
    public void SetFade(bool active) {
        if (active)
            fadeUI.SetActive(true);

        fadeAnim.SetBool("SetFade", active);
    }

    public void AddKeyToList(int level, int key) {
        gameData.totalKeys++;
        if (gameData.levelKeys == null)
            gameData.levelKeys = new int[4][];
        if (gameData.levelKeys[level] == null)
            gameData.levelKeys[level] = new int[ItemManagerScript.itemList[ItemManagerScript.GetItemIndex("StreamKey")].itemLevelTotal];
        gameData.levelKeys[level][key] = 1;
    }
    public void AddObjectToList(string name) {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (gameData.levelObjects == null)
            gameData.levelObjects = new List<string>[SceneManager.sceneCountInBuildSettings];
        if (gameData.levelObjects[index] == null)
            gameData.levelObjects[index] = new List<string>();
        if (!gameData.levelObjects[index].Contains(name))
            gameData.levelObjects[index].Add(name);
    }

    // UI Buttons
    public void MenuPlay() {
        gameData = LoadGame(1);
        LoadScene(gameData.doneTutorial ? "WubHub" : "TutorialRoom");
    }
    public void ReturnToHub() {
        if (SceneManager.GetActiveScene().name == "TutorialRoom")
            gameData.doneTutorial = true;
        SaveGame();
        UnPauseGame();
        LoadScene("WubHub");
    }
    public void ReturnToMainMenu() {
        SaveGame();
        UnPauseGame();
        LoadScene("NewMainMenu");
    }
    public void DeleteSave() {
        DeleteGame(1);
    }
    public void QuitGame() {
        SaveGame();
        Application.Quit();
    }

    // Save/Load
    void SaveGame() {
        SaveSettings();
        if (gameData == null)
            return;

        if (!File.Exists(Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData"))
            Directory.CreateDirectory(Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData");

        string path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "save" + gameData.saveSlot + ".gd";
        SaveChainData.Save("chain" + gameData.saveSlot);

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(path);
        bf.Serialize(file, gameData);
        file.Close();
    }
    GameData LoadGame(int saveSlot) {
        string path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "save" + saveSlot + ".gd";
        SaveChainData.Load("chain" + saveSlot);

        if (File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            GameData gd = (GameData)bf.Deserialize(file);
            file.Close();

            coins = gd.totalCoins;
            keys = gd.totalKeys;

            return gd;
        }
        return new GameData();
    }
    void DeleteGame(int saveSlot) {
        string path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "save" + saveSlot + ".gd";
        if (File.Exists(path))
            File.Delete(path);

        path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "chain" + saveSlot + ".dat";
        if (File.Exists(path))
            File.Delete(path);
    }
    void SaveSettings() {
        if (settingsData == null)
            return;

        string path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "settings.sd";

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(path);
        bf.Serialize(file, settingsData);
        file.Close();
    }
    SettingsData LoadSettings() {
        string path = Application.persistentDataPath + Path.DirectorySeparatorChar + "SaveData" + Path.DirectorySeparatorChar + "settings.sd";

        if (File.Exists(path)) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            SettingsData sd = (SettingsData)bf.Deserialize(file);
            file.Close();

            return sd;
        }
        return new SettingsData();
    }
}
