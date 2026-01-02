using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

[Serializable]
public class TrialResult
{
    public int trialIndex;
    public int nValue;
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
    [TitleGroup("遊戲設定")] [LabelText("休息時間 (秒)")] [MinValue(0)]
    public int restTime = 120;

    [TitleGroup("遊戲設定")] [LabelText("準備時間 (秒)")] [MinValue(0)]
    public float waitTime = 10f;

    [LabelText("測試模式"), Tooltip("開啟後只進行一輪測試")]
    public bool isTest = false;

    [TitleGroup("N-Back 參數")] [Range(1, 3), LabelText("初始 N 值")]
    public int n = 2;

    [LabelText("每輪試次數"), MinValue(10)] public int baseTrials = 20;

    [TitleGroup("試次分配")] [LabelText("僅視覺試次數"), MinValue(0)]
    public int visualTrials = 5;

    [LabelText("僅聽覺試次數"), MinValue(0)] public int audioTrials = 5;

    [LabelText("雙重試次數"), MinValue(0)] public int bothTrials = 2;

    [TitleGroup("反應時間設定")] [LabelText("視覺反應時間 (毫秒)")] [InfoBox("視覺刺激的反應時間視窗")]
    public float visualResponseTime = 500f;

    [LabelText("聽覺反應時間 (毫秒)")] [InfoBox("聽覺刺激的反應時間視窗")]
    public float audioResponseTime = 1000f;

    [LabelText("總反應時間 (毫秒)")] [InfoBox("超過此時間自動進入下一題")]
    public float totalResponseTime = 2000f;

    [TitleGroup("負向刺激素材")] [Required, AssetsOnly]
    public List<AudioClip> negativeAudioClips;

    [Required, AssetsOnly] public List<Sprite> negativeVisualSprites;

    [TitleGroup("UI 組件")] [Required, SceneObjectsOnly]
    public GameObject[] gridPlanes;

    [Required, SceneObjectsOnly] public AudioSource audioSource;

    [SceneObjectsOnly] public TMP_Text nText;

    [SceneObjectsOnly] public GameObject restPanel;

    [SceneObjectsOnly] public GameObject endPanel;

    [TitleGroup("按鍵設定")] [LabelText("視覺反應鍵")]
    public KeyCode visualKey = KeyCode.X;

    [LabelText("聽覺反應鍵")] public KeyCode audioKey = KeyCode.Z;

    [TitleGroup("遊戲狀態")] [ReadOnly, ShowInInspector]
    private string gameStatus = "等待開始";

    [ReadOnly, ShowInInspector, ProgressBar(0, "totalTrials")]
    private int currentTrial = 0;

    // 內部變數
    private int totalTrials;
    private List<int> visualIDList = new();
    private List<int> audioIDList = new();
    private List<bool> visualResponseList = new();
    private List<bool> audioResponseList = new();
    private List<TrialResult> trialResults = new();

    private List<float> visualAccuracyRecord = new();
    private List<float> audioAccuracyRecord = new();
    private List<int> nRecord = new();

    private int visualHit, visualMiss, visualFalseAlarm, visualCorrectRejection;
    private int audioHit, audioMiss, audioFalseAlarm, audioCorrectRejection;

    // 外部觸發標記
    private bool externalVisualTrigger = false;
    private bool externalAudioTrigger = false;

    void Start()
    {
        Debug.Log("🎮 N-Back 遊戲啟動");
        if (!ValidateComponents()) return;
        StartCoroutine(MultiRoundGame());
    }

    bool ValidateComponents()
    {
        if (gridPlanes == null || gridPlanes.Length == 0)
        {
            Debug.LogError("❌ 請設定 gridPlanes！");
            gameStatus = "組件缺失";
            return false;
        }

        if (audioSource == null)
        {
            Debug.LogError("❌ 請設定 audioSource！");
            gameStatus = "組件缺失";
            return false;
        }

        if (negativeAudioClips == null || negativeAudioClips.Count == 0)
        {
            Debug.LogError("❌ 請設定負向音訊素材！");
            gameStatus = "素材缺失";
            return false;
        }

        if (negativeVisualSprites == null || negativeVisualSprites.Count == 0)
        {
            Debug.LogError("❌ 請設定負向視覺素材！");
            gameStatus = "素材缺失";
            return false;
        }

        // ✅ 檢查音訊素材是否為 null
        for (int i = 0; i < negativeAudioClips.Count; i++)
        {
            if (negativeAudioClips[i] == null)
            {
                Debug.LogError($"❌ 音訊素材 [{i}] 為 null！");
                gameStatus = "素材錯誤";
                return false;
            }
        }

        // ✅ 檢查視覺素材是否為 null
        for (int i = 0; i < negativeVisualSprites.Count; i++)
        {
            if (negativeVisualSprites[i] == null)
            {
                Debug.LogError($"❌ 視覺素材 [{i}] 為 null！");
                gameStatus = "素材錯誤";
                return false;
            }
        }

        Debug.Log("✅ 所有組件檢查通過");
        Debug.Log($"📊 音訊素材數量: {negativeAudioClips.Count}");
        Debug.Log($"📊 視覺素材數量: {negativeVisualSprites.Count}");
        Debug.Log($"📊 格子數量: {gridPlanes.Length}");
        
        return true;
    }

    private void InitializeTrial()
{
    // ✅✅✅ 強制驗證版本標記
    Debug.LogError("========================================");
    Debug.LogError("🔴🔴🔴 使用 v3.0 FINAL 版本");
    Debug.LogError("========================================");
    
    totalTrials = baseTrials + n;
    visualResponseList.Clear();
    audioResponseList.Clear();
    visualIDList.Clear();
    audioIDList.Clear();

    Debug.Log($"📝 總試次: {totalTrials}, N={n}, 基礎: {baseTrials}");
    Debug.Log($"需求 - 視覺: {visualTrials}, 聽覺: {audioTrials}, 雙重: {bothTrials}");

    // 初始化所有為 false
    for (int i = 0; i < totalTrials; i++)
    {
        visualResponseList.Add(false);
        audioResponseList.Add(false);
    }

    // ✅ 強制檢查：前 n 個鎖定
    Debug.LogError($"🔒🔒🔒 前 {n} 個試次將被強制鎖定為非刺激");
    
    // ✅ 立即驗證初始狀態
    for (int i = 0; i < n; i++)
    {
        if (visualResponseList[i] || audioResponseList[i])
        {
            Debug.LogError($"❌❌❌ 初始化錯誤：試次 {i} 不是 false！");
        }
    }

    int availableTrials = totalTrials - n;
    int totalRequiredTrials = visualTrials + audioTrials + bothTrials;
    
    if (totalRequiredTrials > availableTrials)
    {
        Debug.LogError($"❌ 試次分配錯誤！需要 {totalRequiredTrials}，可用 {availableTrials}");
        return;
    }

    bool success = false;
    int attempts = 0;
    int maxAttempts = 100;

    while (!success && attempts < maxAttempts)
    {
        attempts++;
        
        // 重置
        for (int i = 0; i < totalTrials; i++)
        {
            visualResponseList[i] = false;
            audioResponseList[i] = false;
        }

        // ✅ 生成可用索引：明確只使用 n 到 totalTrials-1
        List<int> availableIndices = new List<int>();
        for (int i = n; i < totalTrials; i++)
        {
            availableIndices.Add(i);
        }
        
        Debug.Log($"📋 嘗試 {attempts}:");
        Debug.Log($"   可用索引: 從 {n} 到 {totalTrials-1}");
        Debug.Log($"   可用數量: {availableIndices.Count}");
        Debug.Log($"   第一個可用索引: {availableIndices[0]}");
        Debug.Log($"   最後一個可用索引: {availableIndices[availableIndices.Count-1]}");
        
        Shuffle(availableIndices);

        if (availableIndices.Count < totalRequiredTrials)
        {
            Debug.LogError($"❌ 索引不足！");
            return;
        }

        // ✅ 分配刺激 - 明確記錄每個分配
        int index = 0;
        
        Debug.Log($"開始分配刺激...");
        
        // 雙重刺激
        for (int i = 0; i < bothTrials; i++)
        {
            int trialIndex = availableIndices[index];
            Debug.Log($"  雙重 [{i}] -> 試次 {trialIndex}");
            
            // ✅ 檢查是否會分配到前 n 個
            if (trialIndex < n)
            {
                Debug.LogError($"❌❌❌ 致命錯誤：試圖分配試次 {trialIndex} < {n}");
                Debug.Break(); // 強制暫停 Unity
            }
            
            visualResponseList[trialIndex] = true;
            audioResponseList[trialIndex] = true;
            index++;
        }
        
        // 視覺刺激
        for (int i = 0; i < visualTrials; i++)
        {
            int trialIndex = availableIndices[index];
            Debug.Log($"  視覺 [{i}] -> 試次 {trialIndex}");
            
            if (trialIndex < n)
            {
                Debug.LogError($"❌❌❌ 致命錯誤：試圖分配試次 {trialIndex} < {n}");
                Debug.Break();
            }
            
            visualResponseList[trialIndex] = true;
            index++;
        }
        
        // 聽覺刺激
        for (int i = 0; i < audioTrials; i++)
        {
            int trialIndex = availableIndices[index];
            Debug.Log($"  聽覺 [{i}] -> 試次 {trialIndex}");
            
            if (trialIndex < n)
            {
                Debug.LogError($"❌❌❌ 致命錯誤：試圖分配試次 {trialIndex} < {n}");
                Debug.Break();
            }
            
            audioResponseList[trialIndex] = true;
            index++;
        }

        // ✅ 立即驗證分配結果
        Debug.Log("驗證分配結果...");
        success = true;
        
        for (int i = 0; i < n; i++)
        {
            if (visualResponseList[i])
            {
                Debug.LogError($"❌❌❌ 驗證失敗：試次 {i} 有視覺刺激！");
                success = false;
            }
            if (audioResponseList[i])
            {
                Debug.LogError($"❌❌❌ 驗證失敗：試次 {i} 有聽覺刺激！");
                success = false;
            }
        }
        
        if (!success)
        {
            Debug.LogError("❌ 分配失敗，重試...");
            Debug.Break(); // 強制暫停讓你看到錯誤
            continue;
        }

        Debug.Log("✅ 分配驗證通過");

        // 生成隨機 ID
        visualIDList.Clear();
        audioIDList.Clear();
        for (int i = 0; i < totalTrials; i++)
        {
            visualIDList.Add(Random.Range(0, gridPlanes.Length));
            audioIDList.Add(Random.Range(0, negativeAudioClips.Count));
        }

        // 修正非刺激試次的衝突
        bool conflictExists;
        int conflictAttempts = 0;
        int maxConflictAttempts = 100;

        do
        {
            conflictExists = false;
            conflictAttempts++;

            if (conflictAttempts > maxConflictAttempts)
            {
                Debug.LogWarning($"⚠️ 衝突修正失敗");
                success = false;
                break;
            }

            for (int i = n; i < totalTrials; i++)
            {
                if (!visualResponseList[i] && visualIDList[i] == visualIDList[i - n])
                {
                    conflictExists = true;
                    visualIDList[i] = GetDifferentID(visualIDList[i - n], gridPlanes.Length);
                }

                if (!audioResponseList[i] && audioIDList[i] == audioIDList[i - n])
                {
                    conflictExists = true;
                    audioIDList[i] = GetDifferentID(audioIDList[i - n], negativeAudioClips.Count);
                }
            }
        } while (conflictExists);

        if (!success) continue;

        // N-back 複製（在衝突修正後）
        Debug.Log("開始 N-back 複製...");
        for (int i = n; i < totalTrials; i++)
        {
            if (visualResponseList[i])
            {
                visualIDList[i] = visualIDList[i - n];
                Debug.Log($"  試次 {i} 視覺 <- 試次 {i-n}: ID={visualIDList[i]}");
            }
            if (audioResponseList[i])
            {
                audioIDList[i] = audioIDList[i - n];
                Debug.Log($"  試次 {i} 聽覺 <- 試次 {i-n}: ID={audioIDList[i]}");
            }
        }

        break;
    }

    if (success)
    {
        Debug.Log($"✅✅✅ 配置完成（{attempts} 次嘗試）");
        
        // ✅ 最終驗證並顯示前幾個試次
        Debug.LogError("========================================");
        Debug.LogError("🔍 最終驗證 - 前 5 個試次：");
        for (int i = 0; i < Mathf.Min(5, totalTrials); i++)
        {
            string msg = $"試次 {i}: 視覺={visualResponseList[i]}, 聽覺={audioResponseList[i]}";
            if (i < n && (visualResponseList[i] || audioResponseList[i]))
            {
                Debug.LogError($"❌❌❌ {msg} <- 不應該有刺激！");
            }
            else
            {
                Debug.Log(msg);
            }
        }
        Debug.LogError("========================================");
        
        ValidateTrialConfiguration();
    }
    else
    {
        Debug.LogError($"❌❌❌ 初始化完全失敗！");
    }
}

    void ValidateTrialConfiguration()
    {
        Debug.Log("🔍 開始驗證試次配置...");
        
        // 檢查 1：前 n 個試次不應該是刺激
        for (int i = 0; i < n; i++)
        {
            if (visualResponseList[i])
                Debug.LogError($"❌ 試次 {i} 有視覺刺激（應該沒有）");
            if (audioResponseList[i])
                Debug.LogError($"❌ 試次 {i} 有聽覺刺激（應該沒有）");
        }
        
        // 檢查 2：刺激試次的 n-back 正確性
        for (int i = n; i < totalTrials; i++)
        {
            if (visualResponseList[i])
            {
                if (visualIDList[i] != visualIDList[i - n])
                    Debug.LogError($"❌ 試次 {i} 視覺 n-back 錯誤");
            }
            
            if (audioResponseList[i])
            {
                if (audioIDList[i] != audioIDList[i - n])
                    Debug.LogError($"❌ 試次 {i} 聽覺 n-back 錯誤");
            }
        }
        
        // 檢查 3：非刺激試次不應該有 n-back 匹配
        for (int i = n; i < totalTrials; i++)
        {
            if (!visualResponseList[i] && visualIDList[i] == visualIDList[i - n])
                Debug.LogError($"❌ 試次 {i} 視覺非刺激但有 n-back 匹配");
                
            if (!audioResponseList[i] && audioIDList[i] == audioIDList[i - n])
                Debug.LogError($"❌ 試次 {i} 聽覺非刺激但有 n-back 匹配");
        }
        
        // 統計
        int actualVisualStimuli = visualResponseList.Count(v => v);
        int actualAudioStimuli = audioResponseList.Count(a => a);
        Debug.Log($"📊 實際刺激數量 - 視覺: {actualVisualStimuli}, 聽覺: {actualAudioStimuli}");
        Debug.Log("✅ 驗證完成");
    }

    int GetDifferentID(int current, int max)
    {
        if (max <= 1) return 0;

        int newID;
        int attempts = 0;
        int maxAttempts = 50;

        do
        {
            newID = Random.Range(0, max);
            attempts++;

            if (attempts > maxAttempts)
            {
                Debug.LogWarning($"⚠️ GetDifferentID 無法找到不同的 ID，返回遞增值");
                return (current + 1) % max;
            }
        } while (newID == current);

        return newID;
    }

    private IEnumerator MultiRoundGame()
    {
        Debug.Log("🚀 開始多輪遊戲");
        int roundCount = isTest ? 1 : 3;

        for (int round = 0; round < roundCount; round++)
        {
            gameStatus = $"第 {round + 1} 輪準備中";
            Debug.Log($"⏳ {gameStatus}");

            InitializeTrial();

            Debug.Log($"⏰ 等待 {waitTime} 秒後開始");
            yield return new WaitForSeconds(waitTime);

            gameStatus = $"第 {round + 1} 輪進行中 (N={n})";
            Debug.Log($"▶️ 開始第 {round + 1} 輪，n = {n}");

            yield return StartCoroutine(GameLoop());

            CalculateRoundAccuracy();

            if (round < roundCount - 1)
            {
                if (restPanel != null) restPanel.SetActive(true);
                gameStatus = "休息中";
                Debug.Log("🛋️ 休息時間 120 秒");
                yield return new WaitForSeconds(restTime);
                if (restPanel != null) restPanel.SetActive(false);
            }
        }

        gameStatus = "測試完成";
        ShowFinalResults();
    }

    void CalculateRoundAccuracy()
    {
        float visualStimuli = trialResults.Count(r => r.isVisualStimulus);
        float audioStimuli = trialResults.Count(r => r.isAudioStimulus);
        float visualHitCount = trialResults.Count(r => r.isVisualStimulus && r.visualCorrect);
        float audioHitCount = trialResults.Count(r => r.isAudioStimulus && r.audioCorrect);

        float visualAcc = visualStimuli > 0 ? visualHitCount / visualStimuli : 0f;
        float audioAcc = audioStimuli > 0 ? audioHitCount / audioStimuli : 0f;

        visualAccuracyRecord.Add(visualAcc);
        audioAccuracyRecord.Add(audioAcc);
        nRecord.Add(n);

        Debug.Log($"🎯 視覺正確率：{visualAcc * 100f:F2}%");
        Debug.Log($"🎧 聽覺正確率：{audioAcc * 100f:F2}%");

        // 自適應調整 n 值
        if ((visualAcc + audioAcc) / 2f >= 0.5f)
            n = Mathf.Min(3, n + 1);
        else
            n = Mathf.Max(1, n - 1);
    }

    void ShowFinalResults()
    {
        if (endPanel != null) endPanel.SetActive(true);

        Debug.Log("✅ 測試完成！最終結果：");
        for (int i = 0; i < visualAccuracyRecord.Count; i++)
        {
            Debug.Log(
                $"📊 第{i + 1}輪：n={nRecord[i]}, 視覺 {visualAccuracyRecord[i] * 100f:F2}%, 聽覺 {audioAccuracyRecord[i] * 100f:F2}%");
        }

        ExportTrialResultsToCSV();
    }

    IEnumerator GameLoop()
    {
        Debug.Log($"🎮 開始遊戲迴圈，總試次: {totalTrials}");
        visualHit = visualMiss = visualFalseAlarm = visualCorrectRejection = 0;
        audioHit = audioMiss = audioFalseAlarm = audioCorrectRejection = 0;

        if (nText != null) nText.text = "N = " + n;

        for (currentTrial = 0; currentTrial < totalTrials; currentTrial++)
        {
            Debug.Log($"▶ 試次 {currentTrial + 1}/{totalTrials}");

            int vID = visualIDList[currentTrial];
            int aID = audioIDList[currentTrial];

            // ✅ 驗證 ID 範圍
            if (vID < 0 || vID >= gridPlanes.Length)
            {
                Debug.LogError($"❌ 視覺 ID 超出範圍: {vID}");
                continue;
            }
            if (aID < 0 || aID >= negativeAudioClips.Count)
            {
                Debug.LogError($"❌ 音訊 ID 超出範圍: {aID}");
                continue;
            }

            // 清空九宮格
            foreach (var plane in gridPlanes)
            {
                if (plane != null)
                    plane.GetComponent<Renderer>().material.SetTexture("_BaseMap", null);
            }

            // ✅ 顯示視覺刺激（直接使用 vID）
            if (gridPlanes[vID] != null && negativeVisualSprites[vID] != null)
            {
                gridPlanes[vID].GetComponent<Renderer>().material
                    .SetTexture("_BaseMap", negativeVisualSprites[vID].texture);
                Debug.Log($"  視覺刺激: 格子 {vID}, 圖片 {vID}, 刺激={visualResponseList[currentTrial]}");
            }
            else
            {
                Debug.LogError($"❌ 視覺素材或格子為 null: vID={vID}");
            }

            // ✅ 播放聽覺刺激（直接使用 aID）
            if (negativeAudioClips[aID] != null)
            {
                audioSource.Stop(); // 停止前一個音效
                audioSource.PlayOneShot(negativeAudioClips[aID]);
                Debug.Log($"  聽覺刺激: 音訊 {aID}, 刺激={audioResponseList[currentTrial]}");
            }
            else
            {
                Debug.LogError($"❌ 音訊素材為 null: aID={aID}");
            }

            // ✅ 使用 Stopwatch 精確計時
            bool visualPressed = false, audioPressed = false;
            float visualRT = -1f, audioRT = -1f;
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            float visualResponseTimeSec = visualResponseTime / 1000f;
            float audioResponseTimeSec = audioResponseTime / 1000f;
            float totalResponseTimeSec = totalResponseTime / 1000f;

            bool visualWindowOpen = true, audioWindowOpen = true;

            while (stopwatch.Elapsed.TotalSeconds < totalResponseTimeSec)
            {
                float elapsedMs = (float)stopwatch.Elapsed.TotalMilliseconds;
                
                // 視覺反應（鍵盤或外部觸發）
                if (visualWindowOpen && (Input.GetKeyDown(visualKey) || externalVisualTrigger))
                {
                    visualPressed = true;
                    visualRT = elapsedMs;
                    visualWindowOpen = false;

                    if (externalVisualTrigger)
                    {
                        Debug.Log($"  ✓ 視覺反應 (外部觸發): {visualRT:F2}ms");
                        externalVisualTrigger = false;
                    }
                    else
                    {
                        Debug.Log($"  ✓ 視覺反應: {visualRT:F2}ms");
                    }
                }

                // 聽覺反應（鍵盤或外部觸發）
                if (audioWindowOpen && (Input.GetKeyDown(audioKey) || externalAudioTrigger))
                {
                    audioPressed = true;
                    audioRT = elapsedMs;
                    audioWindowOpen = false;

                    if (externalAudioTrigger)
                    {
                        Debug.Log($"  ✓ 聽覺反應 (外部觸發): {audioRT:F2}ms");
                        externalAudioTrigger = false;
                    }
                    else
                    {
                        Debug.Log($"  ✓ 聽覺反應: {audioRT:F2}ms");
                    }
                }

                // 檢查反應視窗
                if (visualWindowOpen && elapsedMs >= visualResponseTime)
                {
                    visualWindowOpen = false;
                    Debug.Log($"  ⏰ 視覺反應視窗關閉 ({visualResponseTime}ms)");
                }
                    
                if (audioWindowOpen && elapsedMs >= audioResponseTime)
                {
                    audioWindowOpen = false;
                    Debug.Log($"  ⏰ 聽覺反應視窗關閉 ({audioResponseTime}ms)");
                }

                yield return null;
            }

            stopwatch.Stop();

            // 記錄結果
            RecordTrialResult(currentTrial, visualPressed, audioPressed, visualRT, audioRT);

            // 清空刺激
            foreach (var plane in gridPlanes)
            {
                if (plane != null)
                    plane.GetComponent<Renderer>().material.SetTexture("_BaseMap", null);
            }
        }

        Debug.Log("🏁 遊戲迴圈結束");
        ShowRoundStatistics();
    }

    void RecordTrialResult(int trialIndex, bool visualPressed, bool audioPressed, float visualRT, float audioRT)
    {
        TrialResult result = new TrialResult
        {
            trialIndex = trialIndex,
            nValue = n,
            isVisualStimulus = visualResponseList[trialIndex],
            isAudioStimulus = audioResponseList[trialIndex],
            visualReactionTime = visualRT,
            audioReactionTime = audioRT
        };

        // ✅ 驗證反應時間
        if (visualPressed && visualRT <= 0)
        {
            Debug.LogWarning($"⚠️ 試次 {trialIndex}: 視覺反應但時間異常 ({visualRT}ms)");
        }
        
        if (audioPressed && audioRT <= 0)
        {
            Debug.LogWarning($"⚠️ 試次 {trialIndex}: 聽覺反應但時間異常 ({audioRT}ms)");
        }

        // 視覺結果
        if (visualResponseList[trialIndex])
        {
            if (visualPressed)
            {
                result.visualCorrect = true;
                result.visualResultType = "Hit";
                visualHit++;
                Debug.Log($"  📊 視覺 Hit: {visualRT:F2}ms");
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

        // 聽覺結果
        if (audioResponseList[trialIndex])
        {
            if (audioPressed)
            {
                result.audioCorrect = true;
                result.audioResultType = "Hit";
                audioHit++;
                Debug.Log($"  📊 聽覺 Hit: {audioRT:F2}ms");
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
    }

    void ShowRoundStatistics()
    {
        int actualVisualStimuli = trialResults.Count(r => r.isVisualStimulus);
        int actualAudioStimuli = trialResults.Count(r => r.isAudioStimulus);

        float visualAccuracy = actualVisualStimuli > 0 ? (float)visualHit / actualVisualStimuli : 0f;
        float audioAccuracy = actualAudioStimuli > 0 ? (float)audioHit / actualAudioStimuli : 0f;

        Debug.Log("======= ✅ 本輪結束！統計結果： =======");
        Debug.Log($"📷 視覺 ➜ Hit: {visualHit}, Miss: {visualMiss}, FA: {visualFalseAlarm}, CR: {visualCorrectRejection}");
        Debug.Log($"📷 視覺 ➜ Total: {actualVisualStimuli}, Acc: {visualAccuracy * 100f:F2}%");
        Debug.Log($"🎧 聽覺 ➜ Hit: {audioHit}, Miss: {audioMiss}, FA: {audioFalseAlarm}, CR: {audioCorrectRejection}");
        Debug.Log($"🎧 聽覺 ➜ Total: {actualAudioStimuli}, Acc: {audioAccuracy * 100f:F2}%");
    }

    public void TriggerVisualResponse()
    {
        if (gameStatus.Contains("進行中") && currentTrial < totalTrials)
        {
            externalVisualTrigger = true;
            Debug.Log($"🔵 外部觸發視覺反應 (試次 {currentTrial + 1})");
        }
        else
        {
            Debug.LogWarning("⚠️ 無法觸發視覺反應：遊戲未在進行中");
        }
    }

    public void TriggerAudioResponse()
    {
        if (gameStatus.Contains("進行中") && currentTrial < totalTrials)
        {
            externalAudioTrigger = true;
            Debug.Log($"🔴 外部觸發聽覺反應 (試次 {currentTrial + 1})");
        }
        else
        {
            Debug.LogWarning("⚠️ 無法觸發聽覺反應：遊戲未在進行中");
        }
    }

    public void ExportTrialResultsToCSV()
    {
        if (isTest)
        {
            Debug.Log("🧪 測試模式：不儲存 CSV 資料");
            return;
        }

        string participantID = PlayerPrefs.GetString("ID", "Unknown");
        string path;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android/Oculus 環境：儲存到 persistentDataPath/NbackTestData 資料夾
        // 路徑通常是 /storage/emulated/0/Android/data/<package_name>/files/NbackTestData
        string downloadFolder = Path.Combine(Application.persistentDataPath, "NbackTestData");
        
        // 確保資料夾存在
        if (!Directory.Exists(downloadFolder))
        {
            try
            {
                Directory.CreateDirectory(downloadFolder);
                Debug.Log($"📁 建立資料夾: {downloadFolder}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ 無法建立資料夾: {e.Message}");
                // 如果無法建立，嘗試直接存到 persistentDataPath 根目錄
                downloadFolder = Application.persistentDataPath;
            }
        }
        
        path = Path.Combine(downloadFolder, "NBackResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
#else
        string dataFolder = Application.dataPath + "/NbackTestData";

        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Debug.Log($"📁 建立資料夾: {dataFolder}");
        }

        path = dataFolder + "/NBackResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("trialIndex,nValue,isVisualStimulus,isAudioStimulus,visualCorrect,audioCorrect,visualReactionTime(ms),audioReactionTime(ms),visualResultType,audioResultType");

        foreach (var result in trialResults)
        {
            csv.AppendLine($"{result.trialIndex}," +
                           $"{result.nValue}," +
                           $"{result.isVisualStimulus}," +
                           $"{result.isAudioStimulus}," +
                           $"{result.visualCorrect}," +
                           $"{result.audioCorrect}," +
                           $"{result.visualReactionTime}," +
                           $"{result.audioReactionTime}," +
                           $"{result.visualResultType}," +
                           $"{result.audioResultType}");
        }

        try
        {
            File.WriteAllText(path, csv.ToString());
            Debug.Log($"✅ CSV 已儲存至: {path}");
            Debug.Log($"👤 受測者 ID: {participantID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 無法寫入CSV: {e.Message}");
        }
    }

    public static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count - 1; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}