using System;
using UnityEngine;
using UnityEngine.UI;

using Granden.temi;

public class RobotFollowDemo : MonoBehaviour
{
    [SerializeField] private Button followMeBtn;
    [SerializeField] private Button stopFollowBtn;
    [SerializeField] private Button backToBtn;

    private bool bRobotReady        = false;

    private TemiRobotProxy robotProxy;

    void Start()
    {
        robotProxy = new TemiRobotProxy();

#if UNITY_ANDROID// && !UNITY_EDITOR
        robotProxy.InitialProxy((isReady) =>
        {
            bRobotReady = isReady;

            // 綁定按鈕
            if (followMeBtn)
            {
                followMeBtn.onClick.AddListener(() => 
                { 
                    Debug.Log("[Temi] Follow Me");
                    robotProxy.Call("beWithMe", (AndroidJavaObject)null);
                });
            }

            if (stopFollowBtn)
            {
                stopFollowBtn.onClick.AddListener(() => 
                { 
                    Debug.Log("[Temi] Stop Following");
                    robotProxy.Call("stopMovement");
                });
            }

            if (backToBtn)
            {
                backToBtn.onClick.AddListener(() =>
                {
                    Debug.Log("[Temi] Back To Battery");
                    robotProxy.Call("goTo", "home base");
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
