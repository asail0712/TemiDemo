using UnityEngine;

public class TemiQuickTest : MonoBehaviour
{
    AndroidJavaObject activity;

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
                var robot = robotClass.CallStatic<AndroidJavaObject>("getInstance");//, activity);
                if (robot == null)
                {
                    throw new System.Exception("Robot.getInstance() 回傳 null");
                }

                // 4) 在 UI 執行緒呼叫 speak(TtsRequest)
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using (var ttsReqClass = new AndroidJavaClass("com.robotemi.sdk.TtsRequest"))
                        {
                            // 第二參數通常表示是否排隊播報（enqueue）。false = 立即、不排隊；true = 加入佇列。
                            var tts = ttsReqClass.CallStatic<AndroidJavaObject>("create", "哈囉，我是 Temi！", false);
                            robot.Call("speak", tts);
                            Debug.Log("[TemiQuickTest] ✅ speak(TtsRequest) 已呼叫");
                        }
                    }
                    catch (AndroidJavaException aje)
                    {
                        Debug.LogError("[TemiQuickTest] speak(TtsRequest) 失敗: " + aje);
                    }
                }));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TemiQuickTest] 失敗：" + ex);
        }
#else
        Debug.Log("[TemiQuickTest] 僅在 Android 裝置上執行（Editor 已略過 JNI）");
#endif
    }
}
