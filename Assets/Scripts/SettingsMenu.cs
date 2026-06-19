using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider masterSliderGame;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider musicSliderGame;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sfxSliderGame;

    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdownGame;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdownGame;

    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle fullscreenToggleGame;
    [SerializeField] private Toggle fpsCounterToggle;
    [SerializeField] private Toggle fpsCounterGameToggle;

    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private RenderPipelineAsset[] qualityLevels;

    private float timer;
    private bool fpsOn = false;
    private Resolution[] resolutions;
    private int savedWidth;
    private int savedHeight;
    private float maxRefreshRate;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetMasterVolume(masterSlider.value);
        SetMusicVolume(musicSlider.value);
        SetSFXVolume(sfxSlider.value);

        qualityDropdown.value = QualitySettings.GetQualityLevel();

        QualitySettings.SetQualityLevel(2);
        qualityDropdown.value = 2;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        resolutionDropdownGame.ClearOptions();

        Resolution resolution = resolutions[resolutions.Length - 1];
        savedHeight = resolution.height;
        savedWidth = resolution.width;

        Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.ExclusiveFullScreen, resolution.refreshRateRatio);

        maxRefreshRate = (float)resolutions[resolutions.Length - 1].refreshRateRatio.value;

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height + "@" + Mathf.RoundToInt((float)resolutions[i].refreshRateRatio.value) + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdownGame.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fpsText.enabled = fpsOn;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= updateInterval)
        {
            int fps = Mathf.RoundToInt(1f / Time.deltaTime);
            fpsText.text = fps + " FPS";
            timer = 0f;
        }
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(volume) * 20);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("soundFXVolume", Mathf.Log10(volume) * 20);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("musicVolume", Mathf.Log10(volume) * 20);
    }

    public void EqualToMainMenu()
    {
        masterSliderGame.value = masterSlider.value;
        musicSliderGame.value = musicSlider.value;
        sfxSliderGame.value = sfxSlider.value;
        resolutionDropdownGame.value = resolutionDropdown.value;
        qualityDropdownGame.value = qualityDropdown.value;
        fullscreenToggleGame.isOn = fullscreenToggle.isOn;
        fpsCounterGameToggle.isOn = fpsCounterToggle.isOn;
    }

    public void EqualToGame()
    {
        masterSlider.value = masterSliderGame.value;
        musicSlider.value = musicSliderGame.value;
        sfxSlider.value = sfxSliderGame.value;
        resolutionDropdown.value = resolutionDropdownGame.value;
        qualityDropdown.value = qualityDropdownGame.value;
        fullscreenToggle.isOn = fullscreenToggleGame.isOn;
        fpsCounterToggle.isOn = fpsCounterGameToggle.isOn;
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);
        QualitySettings.renderPipeline = qualityLevels[qualityIndex];
    }

    public void SetFPSCounter(bool isOn)
    {
        fpsText.enabled = isOn;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;

        if (isFullscreen == true)
        {
            Screen.SetResolution(savedWidth, savedHeight, FullScreenMode.ExclusiveFullScreen);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        savedHeight = resolution.height;
        savedWidth = resolution.width;

        if (Screen.fullScreen)
        {
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.ExclusiveFullScreen, resolution.refreshRateRatio);
        }
        else
        {
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }
}