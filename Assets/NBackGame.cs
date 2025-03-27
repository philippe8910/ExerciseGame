using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TrialResult
{
    public int trialIndex;
    public bool isVisualStimulus;
    public bool isAudioStimulus;
    public bool visualCorrect;
    public bool audioCorrect;
    public float visualReactionTime;
    public float audioReactionTime;
    public string visualResultType;
    public string audioResultType;
}

public class NBackGame : MonoBehaviour
{
    [Header("n-back 設定")]
    [Range(1, 3), Tooltip("設定 n-back 數字，範圍 1~3")]
    public int n = 2;

    [Tooltip("總試次數")]
    public int totalTrials = 20;

    [Tooltip("每題顯示/播放的刺激時間（秒）")]
    public float[] stimulusInterval;
    
    [Tooltip("總視覺題目")]
    public int visualTrials = 5;
    
    [Tooltip("總聽覺題目")]
    public int audioTrials = 5;
    
    [Tooltip("總視覺聽覺題目")]
    public int bothTrials = 2;

    [Header("九宮格(視覺)設定")]
    [Tooltip("指定場上九宮格的 Plane 物件")]
    public GameObject[] gridPlanes;

    [Header("聲音(聽覺)設定")]
    [Tooltip("用來播放音效的 AudioSource")]
    public AudioSource audioSource;

    [Tooltip("可用的 AudioClip 清單")]
    public AudioClip[] audioClips;
    
    [Tooltip("視覺刺激圖片")]
    public Sprite[] visualSprites , normalSprites;

    [Header("玩家按鍵設定")]
    [Tooltip("玩家按鍵 - 視覺刺激")]
    public KeyCode visualKey = KeyCode.Space;

    [Tooltip("玩家按鍵 - 聽覺刺激")]
    public KeyCode audioKey = KeyCode.Z;

    // ---- 內部記錄 ----
    public List<int> visualIDList = new List<int>();  // 視覺 n-back ID
    public List<int> audioIDList  = new List<int>();  // 聲音 n-back ID
    
    public List<bool> visualResponseList = new List<bool>();  // 視覺回應紀錄
    public List<bool> audioResponseList = new List<bool>();     // 聲音回應紀錄
    
    public List<TrialResult> trialResults = new List<TrialResult>();

    

    // 統計：視覺
    private int visualHit = 0;
    private int visualMiss = 0;
    private int visualFalseAlarm = 0;
    private int visualCorrectRejection = 0;

    // 統計：聽覺
    private int audioHit = 0;
    private int audioMiss = 0;
    private int audioFalseAlarm = 0;
    private int audioCorrectRejection = 0;

    void Start()
    {
        if (gridPlanes == null || gridPlanes.Length == 0)
        {
            Debug.LogError("請指定九宮格物件！");
            return;
        }

        if (audioSource == null || audioClips == null || audioClips.Length == 0)
        {
            Debug.LogError("請指定 audioSource 與 audioClips！");
            return;
        }

        Init();
        StartCoroutine(StartGame());
    }


    public IEnumerator StartGame()
{
    Debug.Log("按任意鍵開始遊戲...");
    yield return new WaitUntil(() => Input.anyKeyDown);
    StartCoroutine(GameLoop());
}

IEnumerator GameLoop()
{
    Debug.Log("🎮 遊戲開始！");

    trialResults.Clear();

    // 清空統計
    visualHit = visualMiss = visualFalseAlarm = visualCorrectRejection = 0;
    audioHit = audioMiss = audioFalseAlarm = audioCorrectRejection = 0;

    for (int i = 0; i < totalTrials; i++)
    {
        float interval = stimulusInterval[Random.Range(0, stimulusInterval.Length)];

        int vID = visualIDList[i];
        int aID = audioIDList[i];

        // 顯示圖片
        foreach (var plane in gridPlanes)
            plane.GetComponent<SpriteRenderer>().sprite = null;

        gridPlanes[vID].GetComponent<SpriteRenderer>().sprite = visualSprites[vID];

        // 播放聲音
        audioSource.clip = audioClips[aID];
        audioSource.Play();

        // 記錄反應時間
        bool visualPressed = false;
        bool audioPressed = false;
        float visualRT = -1f;
        float audioRT = -1f;

        float timer = 0f;

        while (timer < interval)
        {
            if (!visualPressed && Input.GetKeyDown(visualKey))
            {
                visualPressed = true;
                visualRT = timer;
            }

            if (!audioPressed && Input.GetKeyDown(audioKey))
            {
                audioPressed = true;
                audioRT = timer;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // 記錄每題資料
        TrialResult result = new TrialResult
        {
            trialIndex = i,
            isVisualStimulus = visualResponseList[i],
            isAudioStimulus = audioResponseList[i],
            visualReactionTime = visualRT,
            audioReactionTime = audioRT
        };

        // 視覺結果分類
        if (visualResponseList[i])
        {
            if (visualPressed)
            {
                result.visualCorrect = true;
                result.visualResultType = "Hit";
                visualHit++;
            }
            else
            {
                result.visualCorrect = false;
                result.visualResultType = "Miss";
                visualMiss++;
            }
        }
        else
        {
            if (visualPressed)
            {
                result.visualCorrect = false;
                result.visualResultType = "FalseAlarm";
                visualFalseAlarm++;
            }
            else
            {
                result.visualCorrect = true;
                result.visualResultType = "CorrectRejection";
                visualCorrectRejection++;
            }
        }

        // 聽覺結果分類
        if (audioResponseList[i])
        {
            if (audioPressed)
            {
                result.audioCorrect = true;
                result.audioResultType = "Hit";
                audioHit++;
            }
            else
            {
                result.audioCorrect = false;
                result.audioResultType = "Miss";
                audioMiss++;
            }
        }
        else
        {
            if (audioPressed)
            {
                result.audioCorrect = false;
                result.audioResultType = "FalseAlarm";
                audioFalseAlarm++;
            }
            else
            {
                result.audioCorrect = true;
                result.audioResultType = "CorrectRejection";
                audioCorrectRejection++;
            }
        }

        trialResults.Add(result);

        // 重設畫面
        foreach (var plane in gridPlanes)
            plane.GetComponent<SpriteRenderer>().sprite = null;
    }

    // --- 統計輸出 ---
    Debug.Log("======= ✅ 遊戲結束！統計結果如下： =======");

    int totalVisualStimuli = visualResponseList.FindAll(v => v).Count;
    int totalAudioStimuli = audioResponseList.FindAll(a => a).Count;

    float visualErrorRate = (visualMiss + visualFalseAlarm) / (float)totalVisualStimuli;
    float audioErrorRate = (audioMiss + audioFalseAlarm) / (float)totalAudioStimuli;

    Debug.Log($"📷 視覺 ➜ Hit: {visualHit}, Miss: {visualMiss}, FalseAlarm: {visualFalseAlarm}, CorrectRej: {visualCorrectRejection}");
    Debug.Log($"🎧 聽覺 ➜ Hit: {audioHit}, Miss: {audioMiss}, FalseAlarm: {audioFalseAlarm}, CorrectRej: {audioCorrectRejection}");
    Debug.Log($"❌ 視覺錯誤率：{(visualErrorRate * 100f):F2}%");
    Debug.Log($"❌ 聽覺錯誤率：{(audioErrorRate * 100f):F2}%");
}


    public void Init()
   {
    bool success = false;

    while (!success)
    {
        visualResponseList.Clear();
        audioResponseList.Clear();
        visualIDList.Clear();
        audioIDList.Clear();

        for (int i = 0; i < totalTrials; i++)
        {
            visualResponseList.Add(false);
            audioResponseList.Add(false);
        }

        // 分配 index
        List<int> allIndices = new List<int>();
        for (int i = 0; i < totalTrials; i++) allIndices.Add(i);
        Shuffle(allIndices);

        List<int> bothIndices = allIndices.GetRange(0, bothTrials);
        List<int> remaining = allIndices.GetRange(bothTrials, allIndices.Count - bothTrials);
        List<int> visualOnlyIndices = remaining.GetRange(0, visualTrials);
        List<int> audioOnlyIndices = remaining.GetRange(visualTrials, audioTrials);

        foreach (int i in bothIndices)
        {
            visualResponseList[i] = true;
            audioResponseList[i] = true;
        }

        foreach (int i in visualOnlyIndices)
            visualResponseList[i] = true;

        foreach (int i in audioOnlyIndices)
            audioResponseList[i] = true;

        // 檢查是否所有 true 都能向前推 n
        success = true;
        for (int i = 0; i < totalTrials; i++)
        {
            if ((visualResponseList[i] || audioResponseList[i]) && i - n < 0)
            {
                success = false;
                break;
            }
        }

        if (!success) continue;

        // 先給隨機 ID
        for (int i = 0; i < totalTrials; i++)
        {
            visualIDList.Add(Random.Range(0, gridPlanes.Length));
            audioIDList.Add(Random.Range(0, audioClips.Length));
        }

        // N-back 往回複製
        for (int i = 0; i < totalTrials; i++)
        {
            if (visualResponseList[i] && i - n >= 0)
                visualIDList[i] = visualIDList[i - n];

            if (audioResponseList[i] && i - n >= 0)
                audioIDList[i] = audioIDList[i - n];
        }

        // 檢查是否有重複
        
        // 持續檢查直到沒有誤中 n-back 為止
        bool conflictExists;

        do
        {
            conflictExists = false;

            for (int i = n; i < totalTrials; i++)
            {
                // 視覺檢查
                if (!visualResponseList[i] && visualIDList[i] == visualIDList[i - n])
                {
                    conflictExists = true;
                    Debug.Log($"⚠️ 修正視覺 N-back 錯誤 at index {i} (ID: {visualIDList[i]})");

                    int maxTry = 50;
                    while (maxTry-- > 0)
                    {
                        int g = Random.Range(0, gridPlanes.Length);
                        if (g != visualIDList[i - n])
                        {
                            visualIDList[i] = g;
                            break;
                        }
                    }
                }

                // 聽覺檢查
                if (!audioResponseList[i] && audioIDList[i] == audioIDList[i - n])
                {
                    conflictExists = true;
                    Debug.Log($"⚠️ 修正聽覺 N-back 錯誤 at index {i} (ID: {audioIDList[i]})");

                    int maxTry = 50;
                    while (maxTry-- > 0)
                    {
                        int g = Random.Range(0, audioClips.Length);
                        if (g != audioIDList[i - n])
                        {
                            audioIDList[i] = g;
                            break;
                        }
                    }
                }
            }

        } while (conflictExists);


        // ✅ 成功生成，印出 debug 表格
        Debug.Log("✅ 成功配置 N-back 任務，以下是詳細配置：");

        for (int i = 0; i < totalTrials; i++)
        {
            string type = (visualResponseList[i] && audioResponseList[i]) ? "Both" :
                          (visualResponseList[i]) ? "Visual" :
                          (audioResponseList[i]) ? "Audio" : "None";

            Debug.Log($"[{i:D2}]  {type,-6} |  V-ID: {visualIDList[i]}  A-ID: {audioIDList[i]}");
        }

        break;
    }
}





    
    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int j = Random.Range(i, n); // UnityEngine.Random.Range
            // 交換元素
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
    
    /*
     public void Init()
       {
           bool success = false;
       
           while (!success)
           {
               visualResponseList.Clear();
               audioResponseList.Clear();
               visualIDList.Clear();
               audioIDList.Clear();
       
               for (int i = 0; i < totalTrials; i++)
               {
                   visualResponseList.Add(false);
                   audioResponseList.Add(false);
               }
       
               // 分配 index
               List<int> allIndices = new List<int>();
               for (int i = 0; i < totalTrials; i++) allIndices.Add(i);
               Shuffle(allIndices);
       
               List<int> bothIndices = allIndices.GetRange(0, bothTrials);
               List<int> remaining = allIndices.GetRange(bothTrials, allIndices.Count - bothTrials);
               List<int> visualOnlyIndices = remaining.GetRange(0, visualTrials);
               List<int> audioOnlyIndices = remaining.GetRange(visualTrials, audioTrials);
       
               foreach (int i in bothIndices)
               {
                   visualResponseList[i] = true;
                   audioResponseList[i] = true;
               }
       
               foreach (int i in visualOnlyIndices)
                   visualResponseList[i] = true;
       
               foreach (int i in audioOnlyIndices)
                   audioResponseList[i] = true;
       
               // 檢查是否所有 true 都能向前推 n
               success = true;
               for (int i = 0; i < totalTrials; i++)
               {
                   if (visualResponseList[i] && i - n < 0)
                   {
                       success = false;
                       break;
                   }
                   if (audioResponseList[i] && i - n < 0)
                   {
                       success = false;
                       break;
                   }
               }
       
               if (!success)
               {
                   // Debug.Log("Retry due to invalid N-back offset");
                   continue;
               }
       
               visualResponseList.ForEach(x => visualIDList.Add(Random.Range(0 , gridPlanes.Length)));
               audioResponseList.ForEach(x => audioIDList.Add(Random.Range(0 , audioClips.Length)));
       
               for (int i = 0 ; i < visualResponseList.Count; i++)
               {
                   if (visualResponseList[i] && i - n >= 0)
                   {
                       visualIDList[i] = visualIDList[i - n];
                   }
               }
       
               for (int i = 0 ; i < audioResponseList.Count; i++)
               {
                   if (audioResponseList[i] && i - n >= 0)
                   {
                       audioIDList[i] = audioIDList[i - n];
                   }
               }
       
               for (int i = 0 ; i < visualIDList.Count; i++)
               {
                   if (i - n > 0)
                   {
                       if (visualIDList[i] == visualIDList[i - n])
                       {
                           if (!visualResponseList[i])
                           {
                               Debug.Log("異常：視覺刺激重複 : " + visualIDList[i] + " at " + i);
                               Debug.Log("重新配置中...");
       
                               while (true)
                               {
                                   var g = Random.Range(0, gridPlanes.Length);
                                   
                                   if (g != visualIDList[i])
                                   {
                                       visualIDList[i] = g;
                                       break;
                                   }
                               }
                           }
                       }
                       
                   }
               }
               
               for (int i = 0 ; i < audioIDList.Count; i++)
               {
                   if (i - n > 0)
                   {
                       if (audioIDList[i] == audioIDList[i - n])
                       {
                           if (!audioResponseList[i])
                           {
                               Debug.Log("異常：聽覺刺激重複 : " + audioIDList[i] + " at " + i);
                               Debug.Log("重新配置中...");
       
                               while (true)
                               {
                                   var g = Random.Range(0, audioClips.Length);
                                   
                                   if (g != audioIDList[i])
                                   {
                                       audioIDList[i] = g;
                                       break;
                                   }
                               }
                           }
                       }
                       
                   }
               }
               
               
               
               
               
               
       
               // 成功離開
               Debug.Log("✅ 成功配置 N-back 任務！");
           }
       }
     */
    

    
}
