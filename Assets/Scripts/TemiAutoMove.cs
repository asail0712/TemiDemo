using Granden.temi;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

using Button = UnityEngine.UI.Button;

public class TemiAutoMove : MonoBehaviour
{
    [SerializeField] private Button[] gotoBtnList;

    private TemiRobotProxy robotProxy;
    private List<bool> enableList;
    private List<string> locList;

    void Start()
    {
        robotProxy  = new TemiRobotProxy();
        enableList  = new List<bool>();
        locList     = null;

#if UNITY_ANDROID// && !UNITY_EDITOR
        robotProxy.InitialProxy((bIsReady) =>
        {
            enableList.Clear();

            robotProxy.Call<AndroidJavaObject>("getLocations", (listAjo) =>
            {
                locList = ToList<string>(listAjo);

                Debug.Log($"[Temi] Locations({locList.Count}): " + string.Join(", ", locList));

                for (int i = 0; i < gotoBtnList.Length; ++i)
                {
                    bool bEnable = locList.Count > i;

                    enableList.Add(bEnable);                   
                }
            });
        });
#endif

        StartCoroutine(InitBtnList());
    }

    void OnDestroy()
    {
#if UNITY_ANDROID
        if (robotProxy == null)
        {
            return;
        }

        robotProxy.ReleaseProxy();
#endif
    }

    private IEnumerator InitBtnList()
    {
        while(true)
        {
            if(enableList.Count == 0 || locList == null)
            {
                yield return null;
            }

            for(int i = 0; i < enableList.Count; ++i)
            {
                bool bEnable = enableList[i];

                gotoBtnList[i].gameObject.SetActive(bEnable);

                if (bEnable)
                {
                    int idx = i; // 👉 這裡建立區域副本

                    Text tf = gotoBtnList[i].GetComponentInChildren<Text>();
                    tf.text = locList[i];

                    gotoBtnList[i].onClick.AddListener(() =>
                    {
                        Debug.Log($"index => {idx}");

                        robotProxy.Call("goTo", locList[idx]);
                    });
                }
            }

            yield break;
        }        
    }

    private List<T> ToList<T>(AndroidJavaObject ajo)
    {
        var list = new List<T>();

        try
        {
            if (ajo != null)
            {
                int size = ajo.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    T s = ajo.Call<T>("get", i);

                    list.Add(s);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[Temi] 解析 getLocations 失敗: " + ex.Message);
        }
        finally
        {
            try { ajo?.Dispose(); } catch { }
        }

        return list;
    }
}
