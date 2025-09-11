using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID
/// Temi 的 OnRobotReadyListener 代理
public class OnRobotReadyProxy : AndroidJavaProxy
{
    private readonly Action<bool> _cb;
    public OnRobotReadyProxy(Action<bool> cb)
        : base("com.robotemi.sdk.listeners.OnRobotReadyListener")
    {
        _cb = cb;
    }

    // Java 端會呼叫這個方法
    public void onRobotReady(bool isReady)
    {
        _cb?.Invoke(isReady);
    }
}
#endif

public class RobotRotateDemo : MonoBehaviour
{
    [SerializeField] private Button rotateRightBtn;
    [SerializeField] private Button rotateLeftBtn;
    [SerializeField] private Button stopRotateBtn;

    private AndroidJavaObject activity;
    private AndroidJavaObject robot;   // 快取 Robot 實例

    private bool bIsRotatingRight   = false;
    private bool bIsRotatingLeft    = false;
    private bool bRobotReady        = false;

    // 供反註冊使用
    private OnRobotReadyProxy readyProxy;

    void Start()
    {
#if UNITY_ANDROID// && !UNITY_EDITOR
        try
        {
            // 1) 取得當前 Activity
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            if (activity == null)
            {
                throw new System.Exception("UnityPlayer.currentActivity == null");
            }

            // 2) 用 Activity 的 ClassLoader 驗證 Temi 類別存在
            var classLoader = activity.Call<AndroidJavaObject>("getClassLoader");
            using (var jClass = new AndroidJavaClass("java.lang.Class"))
            {
                jClass.CallStatic<AndroidJavaObject>(
                    "forName",
                    "com.robotemi.sdk.Robot",   // 類名必須是完整類名（不是包名）
                    false,
                    classLoader
                );
                Debug.Log("[TemiQuickTest] ✅ com.robotemi.sdk.Robot 存在");
            }

            // 3) 取得 Robot 實例（建議傳 Activity）
            using (var robotClass = new AndroidJavaClass("com.robotemi.sdk.Robot"))
            {
                robot = robotClass.CallStatic<AndroidJavaObject>("getInstance");//, activity);
            }

            if (robot == null)
            {
                throw new System.Exception("Robot.getInstance() 回傳 null");
            }

            // ── ① 註冊 OnRobotReadyListener ─────────────────────────────
            readyProxy = new OnRobotReadyProxy((isReady) =>
            {
                bRobotReady = isReady;
                Debug.Log($"[Temi] 🔔 onRobotReady({isReady})");
                if (isReady)
                {
                    // 可在 ready 後做一次自動測試
                    RunOnUiThread(() =>
                    {
                        Debug.Log("[Temi] ✅ Robot Ready，嘗試右轉 30°（單發）");

                        if (bIsRotatingRight)
                        {
                            RunOnUiThread(() =>
                            {
                                TurnBy(30, 0.5f);
                            });
                        }
                    });
                }
            });

            // add listener（注意：介面在 com.robotemi.sdk.listeners）
            robot.Call("addOnRobotReadyListener", readyProxy);
            Debug.Log("[Temi] ✅ 已註冊 OnRobotReadyListener");

            // 綁定按鈕
            if (rotateRightBtn) rotateRightBtn.onClick.AddListener(() => { Debug.Log("[Temi] ▶️ Click Right"); StartRotateRight(); });
            if (rotateLeftBtn) rotateLeftBtn.onClick.AddListener(() => { Debug.Log("[Temi] ◀️ Click Left"); StartRotateLeft(); });
            if (stopRotateBtn) stopRotateBtn.onClick.AddListener(() => { Debug.Log("[Temi] ⏹ Click Stop"); StopRotate(); });

            Debug.Log("[Temi] ✅ 初始化完成，等待 onRobotReady 回呼…");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TemiSpeakDemo] 初始化失敗：" + ex);
        }
#else
        Debug.Log("[TemiSpeakDemo] 僅在 Android 裝置上執行（Editor 已略過 JNI）");
#endif
    }
    void Update()
    {
#if UNITY_ANDROID
        if (robot == null || !bRobotReady) return;

        if (bIsRotatingRight)
        {
            RunOnUiThread(() =>
            {
                TurnBy(5, 0.5f);
            });
        }
        else if (bIsRotatingLeft)
        {
            RunOnUiThread(() =>
            {
                TurnBy(-5, 0.5f);
            });
        }
#endif
    }

    void OnDestroy()
    {
#if UNITY_ANDROID
        try
        {
            if (robot != null && readyProxy != null)
            {
                RunOnUiThread(() =>
                {
                    SafeJavaCall(() => robot.Call("removeOnRobotReadyListener", readyProxy), "removeOnRobotReady");
                });
                Debug.Log("[Temi] 🗑 已移除 OnRobotReadyListener");
            }
        }
        catch { /* 安靜忽略 */ }
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

        RunOnUiThread(() =>
        {
            SafeJavaCall(() => robot.Call("stopMovement"), "stopMovement");
        });
    }

    // ── ② 統一 UI Thread 執行 ─────────────────────────────────────
    private void RunOnUiThread(Action a)
    {
        if (activity == null) { a?.Invoke(); return; } // 理論上不會發生
        activity.Call("runOnUiThread", new AndroidJavaRunnable(() => a?.Invoke()));
    }


    private void TurnBy(int direction, float speed)
    {
        SafeJavaCall(() => robot.Call("turnBy", direction, speed), "turnBy");
    }

    private bool SafeJavaCall(Action invoker, string tag)
    {
        try { invoker?.Invoke(); Debug.Log("[Temi] ✅ " + tag); return true; }
        catch (AndroidJavaException aje) { Debug.LogWarning("[Temi] ❌ " + tag + " → " + aje.Message); return false; }
        catch (Exception ex) { Debug.LogWarning("[Temi] ❌ " + tag + " → " + ex.Message); return false; }
    }

#endif
}
