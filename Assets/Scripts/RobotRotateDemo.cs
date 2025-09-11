using System;
using UnityEngine;
using UnityEngine.UI;

using Granden.temi;

public class RobotRotateDemo : MonoBehaviour
{
    [SerializeField] private Button rotateRightBtn;
    [SerializeField] private Button rotateLeftBtn;
    [SerializeField] private Button stopRotateBtn;

    private bool bIsRotatingRight   = false;
    private bool bIsRotatingLeft    = false;
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
            if (rotateRightBtn) rotateRightBtn.onClick.AddListener(() => { Debug.Log("[Temi] ▶️ Click Right"); StartRotateRight(); });
            if (rotateLeftBtn) rotateLeftBtn.onClick.AddListener(() => { Debug.Log("[Temi] ◀️ Click Left"); StartRotateLeft(); });
            if (stopRotateBtn) stopRotateBtn.onClick.AddListener(() => { Debug.Log("[Temi] ⏹ Click Stop"); StopRotate(); });
        });
#else
        Debug.Log("[TemiSpeakDemo] 僅在 Android 裝置上執行（Editor 已略過 JNI）");
#endif
    }
    void Update()
    {
#if UNITY_ANDROID
        if (!bRobotReady || robotProxy == null) return;

        if (bIsRotatingRight)
        {
            TurnBy(5, 0.5f);
        }
        else if (bIsRotatingLeft)
        {
            TurnBy(-5, 0.5f);            
        }
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

#if UNITY_ANDROID // && !UNITY_EDITOR
    public void StartRotateRight()
    {
        bIsRotatingRight    = true;
        bIsRotatingLeft     = false;
    }

    public void StartRotateLeft()
    {
        bIsRotatingLeft     = true;
        bIsRotatingRight    = false;
    }

    public void StopRotate()
    {
        bIsRotatingLeft     = false;
        bIsRotatingRight    = false;

        if (robotProxy == null)
        {
            return;
        }

        robotProxy.Call("stopMovement");
    }

    private void TurnBy(int direction, float speed)
    {
        robotProxy.Call("turnBy", direction, speed);
    }
#endif
}
