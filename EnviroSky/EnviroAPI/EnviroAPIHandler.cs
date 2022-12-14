using ShanghaiWindy.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class EnviroAPIHandler : MonoBehaviour
{
    public List<EnviroWeatherPreset> zoneWeatherPresets = new List<EnviroWeatherPreset>();

    public GameObject enviroGo;
    public GameObject enviorEffectGo;
    public GameObject defaultGo;

    public EnviroSkyMgr skyMgr;
    public EnviroSkyLite skyLite;

    private Material skyBoxMat;
    private AmbientMode ambientMode;
    private Color ambientEquatorColor;
    private Color ambientGroundColor;
    private Color ambientLight;
    private Color ambientSkyColor;
    private DefaultReflectionMode defaultReflectionMode;
    private bool fog;

    private bool isEnableSky = false;

    private void Awake()
    {
        SHWEventManager.AddListener<EnviroVisiblityEvent>(HandleVisibility);
        SHWEventManager.AddListener<EnviroTimeProgressModeEvent>(HandleTimeProgressMode);
        SHWEventManager.AddListener<EnviroTimeDateEvent>(HandleTimeDate);
        SHWEventManager.AddListener<EnviroWeatherEvent>(HandleWeather);
    }

    private void OnDestroy()
    {
        SHWEventManager.RemoveListener<EnviroVisiblityEvent>(HandleVisibility);
        SHWEventManager.RemoveListener<EnviroTimeProgressModeEvent>(HandleTimeProgressMode);
        SHWEventManager.RemoveListener<EnviroTimeDateEvent>(HandleTimeDate);
        SHWEventManager.RemoveListener<EnviroWeatherEvent>(HandleWeather);
    }

    private void HandleVisibility(EnviroVisiblityEvent evtData)
    {
        isEnableSky = evtData.isVisible;

        if (evtData.isVisible)
        {
            skyBoxMat = RenderSettings.skybox;
            ambientMode = RenderSettings.ambientMode;
            ambientEquatorColor = RenderSettings.ambientEquatorColor;
            ambientGroundColor = RenderSettings.ambientGroundColor;
            ambientLight = RenderSettings.ambientLight;
            ambientSkyColor = RenderSettings.ambientSkyColor;
            defaultReflectionMode = RenderSettings.defaultReflectionMode;
            fog = RenderSettings.fog;

            RenderSettings.fog = true;
        }
        else
        {
            RenderSettings.skybox = skyBoxMat;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientEquatorColor = ambientEquatorColor;
            RenderSettings.ambientGroundColor = ambientGroundColor;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientSkyColor = ambientSkyColor;
            RenderSettings.defaultReflectionMode = defaultReflectionMode;
            RenderSettings.fog = fog;
        }

        defaultGo.SetActive(!evtData.isVisible);
        enviroGo.SetActive(evtData.isVisible);
        enviorEffectGo.SetActive(evtData.isVisible);

        if (evtData.isVisible)
        {
            RenderSettings.fog = true; // Enviro  需要开启 Fog.
            skyLite.Activate();
            AssignCamera();
        }
        else
        {
            skyLite.Deactivate();
        }
    }

    private void HandleTimeProgressMode(EnviroTimeProgressModeEvent evtData)
    {
        skyLite.GameTime.ProgressTime = (EnviroTime.TimeProgressMode)evtData.timeProgressMode;
        skyLite.SetGameTime();
    }

    private void HandleTimeDate(EnviroTimeDateEvent evtData)
    {
        skyLite.SetTime(evtData.years, evtData.days, evtData.hours, evtData.minutes, evtData.seconds);
    }

    private void HandleWeather(EnviroWeatherEvent evtData)
    {
        skyLite.ChangeWeather(evtData.weather);
    }

    private void Update()
    {
        if (isEnableSky)
        {
            AssignCamera();
        }
    }

    private void AssignCamera()
    {
        if (skyLite.PlayerCamera == null)
        {
            var mainCamera = Camera.main;

            if (mainCamera != null)
            {
                if (PlayerGameData.playerIdentity != null)
                {
                    skyLite.AssignAndStart(PlayerGameData.playerIdentity.gameObject, mainCamera);
                }
                else
                {
                    skyLite.AssignAndStart(mainCamera.gameObject, mainCamera);
                }

                Debug.Log("Sky Mgr Assign Camera");
            }
        }
        else
        {
            // 非主摄像机 ，重定向
            if (!skyLite.PlayerCamera.isActiveAndEnabled)
            {
                skyLite.PlayerCamera = null;
            }

            // 非玩家节点，重定向
            if (skyLite.Player == skyLite.PlayerCamera.gameObject)
            {
                if (PlayerGameData.playerIdentity != null)
                {
                    skyLite.PlayerCamera = null;
                }
            }
        }
    }
}
