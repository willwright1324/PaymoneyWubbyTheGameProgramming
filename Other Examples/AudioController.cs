using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioController : MonoBehaviour {
    public AudioMixer audioMixer;
    public Slider masterVolumeSlider, musicVolumeSlider, soundsVolumeSlider;

    public void Init() {
        UpdateAllSliders();
    }

    public void ChangeMasterVolumeSlider() {
        audioMixer.SetFloat("masterVolume", masterVolumeSlider.value);
        GlobalController.Instance.settingsData.volumeMaster = masterVolumeSlider.value;
    }
    public void ChangeMusicVolumeSlider() {
        audioMixer.SetFloat("musicVolume", musicVolumeSlider.value);
        GlobalController.Instance.settingsData.volumeMusic = musicVolumeSlider.value;
    }
    public void ChangeSoundVolumeSlider() {
        audioMixer.SetFloat("soundsVolume", soundsVolumeSlider.value);
        GlobalController.Instance.settingsData.volumeSounds = soundsVolumeSlider.value;
    }

    void UpdateAllSliders() {
        GlobalController.Instance.settingsData.volumeMaster = Mathf.Clamp(GlobalController.Instance.settingsData.volumeMaster, masterVolumeSlider.minValue, masterVolumeSlider.maxValue);
        GlobalController.Instance.settingsData.volumeMusic  = Mathf.Clamp(GlobalController.Instance.settingsData.volumeMusic,  musicVolumeSlider.minValue,  musicVolumeSlider.maxValue);
        GlobalController.Instance.settingsData.volumeSounds = Mathf.Clamp(GlobalController.Instance.settingsData.volumeSounds, soundsVolumeSlider.minValue, soundsVolumeSlider.maxValue);

        masterVolumeSlider.value = GlobalController.Instance.settingsData.volumeMaster;
        musicVolumeSlider.value = GlobalController.Instance.settingsData.volumeMusic;
        soundsVolumeSlider.value = GlobalController.Instance.settingsData.volumeSounds;

        audioMixer.SetFloat("masterVolume", GlobalController.Instance.settingsData.volumeMaster);
        audioMixer.SetFloat("musicVolume", GlobalController.Instance.settingsData.volumeMusic);
        audioMixer.SetFloat("soundsVolume", GlobalController.Instance.settingsData.volumeSounds);
    }
}
