using System;
using UnityEngine;

using Action = System.Action;

namespace Granden.temi
{
#if UNITY_ANDROID
    internal class OnRobotReadyProxy : AndroidJavaProxy
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
#endif //UNITY_ANDROID

    public class TemiRobotProxy
    {
#if UNITY_ANDROID
        private AndroidJavaObject activity;
        private AndroidJavaObject robot;        // 快取 Robot 實例        
        private OnRobotReadyProxy readyProxy;   // 供反註冊使用
        private bool bIsReady;

        public TemiRobotProxy()
        {
            activity    = null;
            robot       = null;
            readyProxy  = null;
            bIsReady    = false;

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
                    throw new Exception("Robot.getInstance() 回傳 null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[Temi] 初始化失敗：" + ex);
            }
        }

        public void InitialProxy(Action<bool> finishAction)
        {
            if (robot == null)
            {
                Debug.LogError("[Temi] 初始化失敗");
                return;
            }

            // ── ① 註冊 OnRobotReadyListener ─────────────────────────────
            readyProxy = new OnRobotReadyProxy((b) => 
            {
                bIsReady = b;
                finishAction.Invoke(bIsReady);
            });

            robot.Call("addOnRobotReadyListener", readyProxy);
        }

        public void ReleaseProxy()
        {
            if (readyProxy == null)
            {
                return;
            }

            Call("removeOnRobotReadyListener", readyProxy);
        }

        public void Call(string methodName, params object[] args)
        {
            if (!bIsReady || robot == null)
            {
                Debug.LogError($"When Calling {methodName}, Temi Is Not Ready !!");
                return;
            }

            RunOnUiThread(() =>
            {
                SafeJavaCall(() => robot.Call(methodName, args), methodName);
            });
        }

        /***********************************
         * private
         * ********************************/

        // ── ② 統一 UI Thread 執行 ─────────────────────────────────────
        private void RunOnUiThread(Action a)
        {
            if (activity == null) 
            { 
                a?.Invoke(); return; 
            } // 理論上不會發生
            
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() => a?.Invoke()));
        }

        private bool SafeJavaCall(Action invoker, string tag)
        {
            try 
            { 
                invoker?.Invoke(); 
                Debug.Log("[Temi] ✅ " + tag); return true; 
            }
            catch (AndroidJavaException aje) 
            { 
                Debug.LogWarning("[Temi] ❌ " + tag + " → " + aje.Message); 
                return false; 
            }
            catch (Exception ex) 
            { 
                Debug.LogWarning("[Temi] ❌ " + tag + " → " + ex.Message); return false; 
            }
        }
#endif //UNITY_ANDROID
    }
}
