using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    public enum TargetDirection { Left, Right }
    public enum Congruency { Congruent, Incongruent }
    public enum EmotionType { Neutral, Negative }

    [TitleGroup("Flanker 任務資料")]
    [ReadOnly, ShowInInspector]
    public List<FlankerTaskData> currentData = new List<FlankerTaskData>();
    
    [TitleGroup("圖片資源")]
    [LabelText("中性圖片")]
    public List<Sprite> neutralImages;
    [LabelText("負向圖片")]
    public List<Sprite> negativeImages;

    [TitleGroup("UI 組件")]
    [Required, SceneObjectsOnly]
    public UnityEngine.UI.Image emotionImageDisplay; // 新增圖片顯示組件
    [Required, SceneObjectsOnly]
    public GameObject endPanel;
    
    [Required, SceneObjectsOnly]
    public TMP_Text systemText;
    
    [Required, SceneObjectsOnly]
    public TMP_Text middleLetter;
    
    [Required, SceneObjectsOnly]
    public TMP_Text upperLetter;
    
    [Required, SceneObjectsOnly]
    public TMP_Text bottomLetter;

    [TitleGroup("顏色設定")]
    public Color arrowColor = Color.blue; // 統一藍色箭頭

    [TitleGroup("時間設定")]
    [LabelText("測試階段刺激顯示時間 (毫秒)")] // 從文字推測，也許需要區分？暫時保留單一設定，或者改名更好理解
    public float stimulusDisplayTime = 500f;
    
    [LabelText("情緒圖片顯示時間 (毫秒)")]
    public float emotionalImageTime = 1000f; // 假設值，原本代碼沒寫，通用做法
    
    [LabelText("反應時間限制 (毫秒)")]
    [InfoBox("受測者可以反應的時間視窗")]
    [MinValue(0)]
    public float responseTimeLimit = 2000f;
    
    [LabelText("試次間隔時間 (秒)")]
    [InfoBox("每個試次之間的間隔時間（顯示注視點 + 的時間）")]
    [MinValue(0)]
    public float timeBetweenTrials = 1.0f;

    [TitleGroup("測試模式")]
    [LabelText("測試模式")]
    [Tooltip("開啟後只進行少量測試，不儲存資料")]
    public bool isTest = false;

    [TitleGroup("遊戲狀態")]
    [ReadOnly, ShowInInspector]
    private string gameStatus = "等待開始";
    
    [ReadOnly, ShowInInspector, ProgressBar(0, "totalTrials")]
    private int currentTrialIndex = 0;
    
    [ReadOnly, ShowInInspector]
    private int totalTrials = 0;

    [TitleGroup("統計資訊")]
    [ReadOnly, ShowInInspector]
    private int correctCount = 0;
    
    [ReadOnly, ShowInInspector]
    private int totalCount = 0;
    
    [ReadOnly, ShowInInspector, SuffixLabel("%", true)]
    private float accuracy = 0f;
    
    [ReadOnly, ShowInInspector, SuffixLabel("秒", true)]
    private float averageResponseTime = 0f;

    // 外部觸發標記
    private bool externalLeftTrigger = false;
    private bool externalRightTrigger = false;

    private void Start()
    {
        Debug.Log("🎮 Flanker 任務啟動");
        
        if (!ValidateComponents()) return;

        try
        {
            Init();
        }
        catch (Exception e)
        {
            systemText.text += "❌ Init 發生錯誤：\n" + e + "\n";
            gameStatus = "初始化失敗";
            return;
        }

        StartCoroutine(StartTask());
    }

    bool ValidateComponents()
    {
        bool isValid = true;

        if (systemText == null)
        {
            Debug.LogError("❌ systemText 未綁定");
            gameStatus = "組件缺失";
            return false;
        }

        if (middleLetter == null || upperLetter == null || bottomLetter == null)
        {
            systemText.text += "⚠️ 有 TMP_Text 元件未綁定\n";
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (endPanel == null)
        {
            systemText.text += "⚠️ endPanel 未綁定\n";
            gameStatus = "組件缺失";
            isValid = false;
        }
        
        if (emotionImageDisplay == null)
        {
             // 暫時設為警告，避免舊場景報錯
             systemText.text += "⚠️ emotionImageDisplay 未綁定 (若需要顯示情緒圖請綁定)\n";
        }

        if (isValid)
        {
            Debug.Log("✅ 所有組件檢查通過");
        }

        return isValid;
    }

    private IEnumerator waitForGameStart()
    {
        gameStatus = "準備中";
        Debug.Log("⏰ 等待 5 秒後開始 Flanker 任務");
        yield return new WaitForSeconds(5);
        yield return null;
    }

    private IEnumerator StartTask()
    {
        yield return StartCoroutine(waitForGameStart());

        gameStatus = "進行中";
        // totalTrials = currentData.Count; // Init 中已設定
        Debug.Log($"🎮 開始 Flanker 任務，總題數: {totalTrials}");
        currentTrialIndex = 0;
        
        // 隱藏圖片與文字
        if (emotionImageDisplay != null) emotionImageDisplay.gameObject.SetActive(false);
        middleLetter.text = "";
        upperLetter.text = "";
        bottomLetter.text = "";

        for (int i = 0; i < currentData.Count; i++)
        {
            var data = currentData[i];
            
            // --- 階段/區塊休息判斷 ---
            // 練習結束 (32題)
            if (i == 32)
            {
                Debug.Log("⏸ 練習階段結束，進入正式測驗");
                systemText.text = "練習結束。請按任一鍵開始正式測驗。"; // 簡單示意，實際可能需要 UI
                yield return new WaitForSeconds(2.0f); // 暫停一下
                systemText.text = ""; // 清空
            }
            
            // 正式階段 Block 休息 (每 96 題，從第 32 題後開始算)
            // 32 + 96 = 128, 32 + 192 = 224, ...
            if (i > 32 && (i - 32) % 96 == 0)
            {
                 Debug.Log($"⏸ Block 休息 (已完成 {i} 題)");
                 systemText.text = "休息時間。請按任一鍵繼續。";
                 yield return new WaitForSeconds(2.0f);
                 systemText.text = "";
            }

            if (middleLetter == null || upperLetter == null || bottomLetter == null)
                yield break;

            Debug.Log($"▶ 試次 {currentTrialIndex + 1}/{totalTrials} ({(data.isPractice ? "練習" : "正式")})");

            // 1. 顯示注視點 (+)
            middleLetter.text = "+";
            middleLetter.color = Color.black; // 注視點黑色
            upperLetter.text = "";
            bottomLetter.text = "";
            if (emotionImageDisplay != null) emotionImageDisplay.gameObject.SetActive(false);

            yield return new WaitForSeconds(timeBetweenTrials);

            // 2. 顯示情緒圖片 (如果有)
            middleLetter.text = ""; // 清除注視點
            if (data.emotionImage != null && emotionImageDisplay != null)
            {
                emotionImageDisplay.sprite = data.emotionImage;
                emotionImageDisplay.gameObject.SetActive(true);
            }
            // 圖片顯示時間
            yield return new WaitForSeconds(emotionalImageTime / 1000f);
            
            // 關閉圖片
            if (emotionImageDisplay != null) emotionImageDisplay.gameObject.SetActive(false);

            // 3. 顯示刺激 (箭頭)
            middleLetter.color = arrowColor; // All Blue
            upperLetter.color = arrowColor; // All Blue
            bottomLetter.color = arrowColor; // All Blue

            middleLetter.text = data.stimulusString;
            upperLetter.text = data.stimulusString;
            bottomLetter.text = data.stimulusString;

            // 開始計時
            float startTime = Time.time;
            
            string congStr = data.congruency == Congruency.Congruent ? "一致" : "不一致";
            string dirStr = data.targetDirection == TargetDirection.Left ? "左" : "右";
            Debug.Log($"  刺激: {data.stimulusString} ({congStr}/{dirStr}), 情緒: {data.emotion}");

            // 計算時間參數
            float stimulusDisplayTimeSec = data.stimulusDuration; // 使用資料中的設定
            float responseTimeLimitSec = responseTimeLimit / 1000f;
            float totalResponseWindow = stimulusDisplayTimeSec + responseTimeLimitSec;

            bool responded = false;
            bool stimulusCleared = false;

            // 反應視窗
            while (Time.time - startTime < totalResponseWindow)
            {
                // 刺激顯示時間結束後清空畫面 (但繼續等待反應)
                if (!stimulusCleared && Time.time - startTime >= stimulusDisplayTimeSec)
                {
                    middleLetter.text = "";
                    upperLetter.text = "";
                    bottomLetter.text = "";
                    stimulusCleared = true;
                }

                bool leftUp = externalLeftTrigger;
                bool rightUp = externalRightTrigger;

                if (leftUp || rightUp)
                {
                    data.responseTime = Time.time - startTime;
                    responded = true;
                    
                    // 判斷正確性
                    // 目標向左 -> 左手觸發為正確
                    // 目標向右 -> 右手觸發為正確
                    bool isLeftTarget = data.targetDirection == TargetDirection.Left;
                    bool isRightTarget = data.targetDirection == TargetDirection.Right; // Should be true if not Left

                    // 避免雙手同時按
                    if (leftUp && rightUp)
                    {
                         data.isCorrect = false;
                         Debug.Log($"  ✗ 雙手同時按 - 錯誤");
                    }
                    else if (isLeftTarget && leftUp)
                    {
                        data.isCorrect = true;
                        Debug.Log($"  ✓ 左手反應 - 正確");
                    }
                    else if (isRightTarget && rightUp)
                    {
                        data.isCorrect = true;
                        Debug.Log($"  ✓ 右手反應 - 正確");
                    }
                    else
                    {
                        data.isCorrect = false;
                        Debug.Log($"  ✗ 錯誤反應 (L:{leftUp} R:{rightUp} Target:{data.targetDirection})");
                    }
                    
                    externalLeftTrigger = false;
                    externalRightTrigger = false;
                    break;
                }

                yield return null;
            }

            // 超時
            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = totalResponseWindow;
                Debug.Log($"  ⏱ 超時 - 未反應");
                
                if (!stimulusCleared)
                {
                    middleLetter.text = "";
                    upperLetter.text = "";
                    bottomLetter.text = "";
                }
            }

            externalLeftTrigger = false;
            externalRightTrigger = false;
            currentTrialIndex++;
        }

        // 計算結果
        CalculateFinalResults();

        string resultText = $"✅ 正確率: {correctCount}/{totalCount} ({accuracy:F2}%)\n" +
                            $"⏱️ 平均反應時間（正確題）: {averageResponseTime:F3} 秒\n";
        systemText.text += resultText;
        Debug.Log("======= ✅ Flanker 任務結束！統計結果： =======");
        Debug.Log($"📊 正確率: {correctCount}/{totalCount} ({accuracy:F2}%)");
        Debug.Log($"⏱️ 平均反應時間（正確題）: {averageResponseTime:F3} 秒");

        gameStatus = "測試完成";

        if (endPanel != null)
            endPanel.SetActive(true);

        ExportFlankerResultsToCSV();
    }

    void CalculateFinalResults()
    {
        totalCount = currentData.Count;
        correctCount = currentData.Count(d => d.isCorrect);
        accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        averageResponseTime = currentData.Where(d => d.isCorrect).Any()
            ? currentData.Where(d => d.isCorrect).Average(d => d.responseTime)
            : 0f;
    }

    /// <summary>
    /// 外部觸發左手反應（紅色反應）
    /// </summary>
    [Button("測試左手觸發", ButtonSizes.Medium), GUIColor(1, 0.5f, 0.5f)]
    [HideInEditorMode]
    public void TriggerLeftResponse()
    {
        if (gameStatus.Contains("進行中"))
        {
            externalLeftTrigger = true;
            Debug.Log($"🔴 外部觸發左手反應 (試次 {currentTrialIndex + 1})");
        }
        else
        {
            Debug.LogWarning("⚠️ 無法觸發左手反應：遊戲未在進行中");
        }
    }

    /// <summary>
    /// 外部觸發右手反應（綠色反應）
    /// </summary>
    [Button("測試右手觸發", ButtonSizes.Medium), GUIColor(0.5f, 1, 0.5f)]
    [HideInEditorMode]
    public void TriggerRightResponse()
    {
        if (gameStatus.Contains("進行中"))
        {
            externalRightTrigger = true;
            Debug.Log($"🟢 外部觸發右手反應 (試次 {currentTrialIndex + 1})");
        }
        else
        {
            Debug.LogWarning("⚠️ 無法觸發右手反應：遊戲未在進行中");
        }
    }

    [Button("重新初始化任務", ButtonSizes.Large), GUIColor(0.5f, 0.5f, 1)]
    [HideInPlayMode]
    public void Init()
    {
        currentData.Clear();
        
        // --- 1. 定義基本條件 ---
        // 方向 x 一致性 -> 4種組合
        // 1. Target Left, Congruent: <<<<<
        // 2. Target Left, Incongruent: >><>> 
        // 3. Target Right, Congruent: >>>>>
        // 4. Target Right, Incongruent: <<><<
        
        // 修正符號定義：
        // Congruent Left: <<<<< (全左)
        // Congruent Right: >>>>> (全右)
        // Incongruent Left (Target Middle Left): >><>> (旁邊右，中間左? 題目說 "中間相反：>><>>" -> 這看起來是中間左，旁邊右)
        // Incongruent Right (Target Middle Right): <<><< (旁邊左，中間右)

        var conditions = new List<(TargetDirection dir, Congruency cong, string stimuli)>
        {
            (TargetDirection.Left, Congruency.Congruent, "<<<<<"),
            (TargetDirection.Left, Congruency.Incongruent, ">><>>"),
            (TargetDirection.Right, Congruency.Congruent, ">>>>>"),
            (TargetDirection.Right, Congruency.Incongruent, "<<><<") 
        };

        // --- 2. 練習階段 (32 trials) ---
        // 4種情境 x 2種情緒 = 8種組合
        
        List<FlankerTaskData> practiceTrials = new List<FlankerTaskData>();
        
        // 簡單生成練習試次
        int practiceRepeats = isTest ? 1 : 4; // 測試模式只跑 1 輪 (8題)，正式跑 4 輪 (32題)
        for (int i = 0; i < practiceRepeats; i++)
        { 
            foreach (var cond in conditions)
            {
                // 中性
                practiceTrials.Add(CreateTrial(cond, EmotionType.Neutral, true));
                // 負向
                practiceTrials.Add(CreateTrial(cond, EmotionType.Negative, true));
            }
        }
        ShuffleList(practiceTrials);
        currentData.AddRange(practiceTrials);

        // --- 3. 正式階段 (384 trials) ---
        // 4 Blocks, 每個 Block 96 trials
        
        List<FlankerTaskData> testTrials = new List<FlankerTaskData>();
        int blocks = isTest ? 1 : 4; // 測試模式只跑 1 Block
        int trialsPerBlock = isTest ? 1 : 12; // 測試模式每個 Block 每組合跑 1 次 (共8次)，正式跑 12 次 (共96次)
        
        for (int b = 0; b < blocks; b++)
        {
            List<FlankerTaskData> blockTrials = new List<FlankerTaskData>();
            for (int k = 0; k < trialsPerBlock; k++)
            {
                foreach (var cond in conditions)
                {
                    blockTrials.Add(CreateTrial(cond, EmotionType.Neutral, false));
                    blockTrials.Add(CreateTrial(cond, EmotionType.Negative, false));
                }
            }
            ShuffleList(blockTrials);
            testTrials.AddRange(blockTrials);
        }
        
        currentData.AddRange(testTrials);

        totalTrials = currentData.Count;
        Debug.Log($"✅ Flanker 任務初始化完成，總題數: {currentData.Count} (練習: {practiceTrials.Count}, 正式: {testTrials.Count}) Mode: {(isTest ? "TEST" : "FULL")}");
    }

    private FlankerTaskData CreateTrial((TargetDirection dir, Congruency cong, string stimuli) cond, EmotionType emotion, bool isPractice)
    {
        Sprite img = null;
        if (emotion == EmotionType.Neutral && neutralImages != null && neutralImages.Count > 0)
            img = neutralImages[Random.Range(0, neutralImages.Count)];
        else if (emotion == EmotionType.Negative && negativeImages != null && negativeImages.Count > 0)
            img = negativeImages[Random.Range(0, negativeImages.Count)];

        return new FlankerTaskData
        {
            stimulusString = cond.stimuli,
            targetDirection = cond.dir,
            congruency = cond.cong,
            emotion = emotion,
            emotionImage = img,
            isPractice = isPractice,
            stimulusDuration = stimulusDisplayTime / 1000f
        };
    }

    public void ExportFlankerResultsToCSV()
    {
        // 測試模式下不儲存資料
        if (isTest)
        {
            Debug.Log("🧪 測試模式：不儲存 CSV 資料");
            return;
        }

        // 獲取受測者 ID
        string participantID = PlayerPrefs.GetString("ID", "Unknown");

        string path;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android/Oculus 環境：儲存到 Download/FlankerTestData 資料夾
        string downloadFolder = "/storage/emulated/0/Download/FlankerTestData";
        
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
                // 如果無法建立資料夾，直接存在 Download 根目錄
                downloadFolder = "/storage/emulated/0/Download";
            }
        }
        
        path = downloadFolder + "/FlankerResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
        // Unity Editor 或其他平台：儲存到 Application.dataPath
        string dataFolder = Application.dataPath + "/FlankerTestData";

        // 確保資料夾存在
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Debug.Log($"📁 建立資料夾: {dataFolder}");
        }

        path = dataFolder + "/FlankerResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") +
               ".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Index,Stimulus,Direction,Congruency,Emotion,IsPractice,IsCorrect,ResponseTime(s)");

        for (int i = 0; i < currentData.Count; i++)
        {
            var data = currentData[i];
            csv.AppendLine(
                $"{i},{data.stimulusString},{data.targetDirection},{data.congruency},{data.emotion},{data.isPractice},{data.isCorrect},{data.responseTime:F3}");
        }

        csv.AppendLine();
        csv.AppendLine($"總題數 (Total),{totalCount}");
        csv.AppendLine($"正確題數 (Correct),{correctCount}");
        csv.AppendLine($"正確率 (Accuracy),{accuracy:F2}%");
        csv.AppendLine($"平均反應時間 (AvgRT - Correct),{averageResponseTime:F3}");
        
        // 簡單的分項統計
        var testData = currentData.Where(d => !d.isPractice).ToList();
        if (testData.Count > 0)
        {
            int testCorrect = testData.Count(d => d.isCorrect);
            float testAcc = (float)testCorrect / testData.Count * 100f;
            float testAvgRT = testData.Where(d => d.isCorrect).Any() ? testData.Where(d => d.isCorrect).Average(d => d.responseTime) : 0;
            
            csv.AppendLine($"正式測驗 (Test Phase) 統計:,");
            csv.AppendLine($"Count,{testData.Count}");
            csv.AppendLine($"Accuracy,{testAcc:F2}%");
            csv.AppendLine($"AvgRT,{testAvgRT:F3}");
        }

        try
        {
            File.WriteAllText(path, csv.ToString(), Encoding.UTF8); // 確保 UTF8，避免亂碼
            string msg = "✅ Flanker CSV 已儲存至: " + path;
            Debug.Log(msg);
            Debug.Log($"👤 受測者 ID: {participantID}");
            systemText.text += msg + "\n";
        }
        catch (Exception e)
        {
            string err = "❌ 無法寫入Flanker CSV: " + e.Message;
            Debug.LogError(err);
            systemText.text += err + "\n";
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private string ColorToString(Color c)
    {
        if (c == Color.red) return "Red";
        if (c == Color.green) return "Green";
        if (c == Color.blue) return "Blue";
        if (c == Color.white) return "White";
        if (c == Color.black) return "Black";
        return $"RGBA({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
    }
}

[System.Serializable]
[CreateAssetMenu(fileName = "EmotionalFlankerTaskDataHolder", menuName = "EmotionalFlankerTaskDataHolder", order = 1)]
public class EmotionalFlankerTaskDataHolder : ScriptableObject
{
    public List<string> neutralLatter;
    public List<string> negativeLatter;
}

[System.Serializable]
public class FlankerTaskData
{
    public string stimulusString;    // 顯示的字串 (e.g., >>>>>)
    public EmotionalFlankerTaskSystem.TargetDirection targetDirection; // 目標方向 (Left/Right)
    public EmotionalFlankerTaskSystem.Congruency congruency; // 一致性 (Congruent/Incongruent)
    public EmotionalFlankerTaskSystem.EmotionType emotion;   // 情緒 (Neutral/Negative)
    public Sprite emotionImage;      // 情緒圖片
    
    public bool isPractice;          // 是否為練習試次
    public float stimulusDuration;   // 刺激呈現時間
    
    // 結果數據
    public bool isCorrect;
    public float responseTime;
}