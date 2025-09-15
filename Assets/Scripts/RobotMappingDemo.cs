using System;
using UnityEngine;
using UnityEngine.UI;

using Granden.temi;

public class RobotMappingDemo : MonoBehaviour
{
    [SerializeField] private Button editMapBtn;
    [SerializeField] private Button finishEditBtn;
    [SerializeField] private Button addGoalBtn;
    [SerializeField] private Button removeGoalBtn;
    [SerializeField] private InputField inputText;

    private TemiRobotProxy robotProxy;

    void Start()
    {
        editMapBtn.interactable     = false;
        finishEditBtn.interactable  = false;
        addGoalBtn.interactable     = false;
        removeGoalBtn.interactable  = false;
        inputText.interactable      = false;

        robotProxy = new TemiRobotProxy();

#if UNITY_ANDROID// && !UNITY_EDITOR
        robotProxy.InitialProxy((bIsReady) =>
        {            
            if(!bIsReady)
            {
                return;
            }

            RefreshUI();

            // 綁定按鈕
            if (editMapBtn)
            {
                editMapBtn.onClick.AddListener(() => 
                { 
                    Debug.Log("[Temi] Begin Edit Map");
                    robotProxy.Call<int>("continueMapping", (result) =>
                    {
                        RefreshUI();
                    });
                });
            }

            if (finishEditBtn)
            {
                finishEditBtn.onClick.AddListener(() => 
                { 
                    Debug.Log("[Temi] Finish Edit Map");
                    robotProxy.Call<int>("finishMapping", (result) => 
                    {
                        RefreshUI();
                    });
                });
            }

            if (addGoalBtn)
            {
                addGoalBtn.onClick.AddListener(() =>
                {
                    Debug.Log("[Temi] Add Goal");
                    robotProxy.Call<bool>("saveLocation", (b) => 
                    {
                        RefreshUI();
                    }, inputText.text);
                });
            }

            if (removeGoalBtn)
            {
                removeGoalBtn.onClick.AddListener(() =>
                {
                    Debug.Log("[Temi] Remove Goal");
                    robotProxy.Call<bool>("deleteLocation", (b) =>
                    {
                        RefreshUI();
                    }, inputText.text);
                });
            }
        });
#else
        Debug.Log("[TemiSpeakDemo] 僅在 Android 裝置上執行（Editor 已略過 JNI）");
#endif
    }

    void OnDestroy()
    {
#if UNITY_ANDROID
        if(robotProxy == null)
        {
            return;
        }

        robotProxy.ReleaseProxy();
#endif
    }

    private void RefreshUI()
    {
        robotProxy.Call<AndroidJavaObject>("isMapLocked", (b) => 
        {
            bool bIsLocked = false;

            Debug.Log($"[Temi] isMapLocked Result {b}");

            if (b != null)
            {
                // Kotlin Boolean? 對應到 Java Boolean (java.lang.Boolean)
                bIsLocked = b.Call<bool>("booleanValue");
            }
            else
            {
                Debug.Log($"[Temi] isMapLocked Null");
            }

            editMapBtn.interactable     = bIsLocked;
            finishEditBtn.interactable  = !bIsLocked;
            addGoalBtn.interactable     = !bIsLocked;
            removeGoalBtn.interactable  = !bIsLocked;
            inputText.interactable      = !bIsLocked;
        });
    }
}
