using System;
using UnityEngine;
using UnityEngine.UI;

using Granden.temi;

public class RobotMappingDemo : MonoBehaviour
{
    [SerializeField] private Button addGoalBtn;
    [SerializeField] private Button removeGoalBtn;
    [SerializeField] private InputField inputText;

    private TemiRobotProxy robotProxy;
    private bool bIsLocked;

    void Start()
    {
        bIsLocked                   = false;

        addGoalBtn.interactable     = !bIsLocked;
        removeGoalBtn.interactable  = !bIsLocked;
        inputText.interactable      = !bIsLocked;

        robotProxy = new TemiRobotProxy();

#if UNITY_ANDROID// && !UNITY_EDITOR
        robotProxy.InitialProxy((bIsReady) =>
        {            
            if(!bIsReady)
            {
                return;
            }

            Debug.Log("[Temi] Initial Complete !!");

            if (addGoalBtn)
            {
                addGoalBtn.onClick.AddListener(() =>
                {
                    Debug.Log("[Temi] Add Goal");
                    robotProxy.Call<bool>("saveLocation", (b) => 
                    {
                        Debug.Log($"[Temi] Add Goal Result {b}");
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
                        Debug.Log($"[Temi] Remove Goal Result {b}");
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
}
