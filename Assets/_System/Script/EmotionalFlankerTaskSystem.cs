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
    [TitleGroup("Flanker 任務資料")]
    [ReadOnly, ShowInInspector]
    public List<FlankerTaskData> currentData = new List<FlankerTaskData>();

    [TitleGroup("UI 組件")]
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
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    [TitleGroup("時間設定")]
    [LabelText("刺激顯示時間 (毫秒)")]
    [InfoBox("刺激在螢幕上顯示的時間")]
    [MinValue(0)]
    public float stimulusDisplayTime = 500f;
    
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
        totalTrials = currentData.Count;
        Debug.Log($"🎮 開始 Flanker 任務，總題數: {totalTrials}");
        Debug.Log($"⚙️ 刺激顯示時間: {stimulusDisplayTime}ms, 反應時間限制: {responseTimeLimit}ms, 試次間隔: {timeBetweenTrials}s");
        currentTrialIndex = 0;

        foreach (var data in currentData)
        {
            if (middleLetter == null || upperLetter == null || bottomLetter == null)
                yield break;

            Debug.Log($"▶ 試次 {currentTrialIndex + 1}/{totalTrials}");

            // 顯示注視點
            middleLetter.text = "+";
            upperLetter.text = "";
            bottomLetter.text = "";

            yield return new WaitForSeconds(timeBetweenTrials);

            // 顯示刺激
            middleLetter.color = data.midColor;
            upperLetter.color = data.OtherColor;
            bottomLetter.color = data.OtherColor;

            middleLetter.text = data.currentLetter;
            upperLetter.text = data.currentLetter;
            bottomLetter.text = data.currentLetter;

            // ✅ 關鍵修正：在刺激顯示的同時開始計時
            float startTime = Time.time;

            Debug.Log($"  刺激: {data.currentLetter}, 中間顏色: {ColorToString(data.midColor)}, 旁邊顏色: {ColorToString(data.OtherColor)}, 負向: {data.isNegative}");

            // 計算時間參數
            float stimulusDisplayTimeSec = stimulusDisplayTime / 1000f;
            float responseTimeLimitSec = responseTimeLimit / 1000f;
            float totalResponseWindow = stimulusDisplayTimeSec + responseTimeLimitSec;

            bool responded = false;
            bool stimulusCleared = false;

            // ✅ 在整個反應視窗內檢測反應（包含刺激顯示期間）
            while (Time.time - startTime < totalResponseWindow)
            {
                // 刺激顯示時間結束後才清空畫面
                if (!stimulusCleared && Time.time - startTime >= stimulusDisplayTimeSec)
                {
                    middleLetter.text = "";
                    upperLetter.text = "";
                    bottomLetter.text = "";
                    stimulusCleared = true;
                }

                bool leftUp = externalLeftTrigger;
                bool rightUp = externalRightTrigger;

                // 雙手同時觸發 = 錯誤
                if (leftUp && rightUp)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false;
                    responded = true;
                    Debug.Log($"  ✗ 反應 (外部觸發 - 雙手): {data.responseTime:F3}s - 錯誤");
                    externalLeftTrigger = false;
                    externalRightTrigger = false;
                    break;
                }

                // 右手觸發且中間是綠色 = 正確
                if (rightUp && !leftUp && data.midColor == Color.green)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    Debug.Log($"  ✓ 反應 (外部觸發 - 右手): {data.responseTime:F3}s - 正確");
                    externalRightTrigger = false;
                    break;
                }

                // 左手觸發且中間是紅色 = 正確
                if (leftUp && !rightUp && data.midColor == Color.red)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    Debug.Log($"  ✓ 反應 (外部觸發 - 左手): {data.responseTime:F3}s - 正確");
                    externalLeftTrigger = false;
                    break;
                }

                // 錯誤反應（右手但是紅色）
                if (rightUp && !leftUp && data.midColor == Color.red)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false;
                    responded = true;
                    Debug.Log($"  ✗ 反應 (外部觸發 - 右手/紅色): {data.responseTime:F3}s - 錯誤");
                    externalRightTrigger = false;
                    break;
                }

                // 錯誤反應（左手但是綠色）
                if (leftUp && !rightUp && data.midColor == Color.green)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false;
                    responded = true;
                    Debug.Log($"  ✗ 反應 (外部觸發 - 左手/綠色): {data.responseTime:F3}s - 錯誤");
                    externalLeftTrigger = false;
                    break;
                }

                yield return null;
            }

            // 沒有反應 = 超時
            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = totalResponseWindow; // ✅ 修正：記錄完整的反應視窗時間
                Debug.Log($"  ⏱ 超時: {data.responseTime:F3}s - 未反應");
                
                // 確保刺激已清空
                if (!stimulusCleared)
                {
                    middleLetter.text = "";
                    upperLetter.text = "";
                    bottomLetter.text = "";
                }
            }

            // 重置外部觸發標記
            externalLeftTrigger = false;
            externalRightTrigger = false;

            // 記錄顏色是否相同
            data.colorIsSame = (data.midColor == data.OtherColor);

            middleLetter.color = Color.white;
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
        
        /*
        "分屍", "強姦", "屠殺", "凌虐", "自焚", "崩潰", "暴躁", "上吊", "欺騙", "變態",
                    "憤怒", "亂倫", "血腥", "暴虐", "溺斃", "狠毒", "砍頭", "詛咒", "發怒", "猥褻",
                    "畜生", "怒罵", "殘忍", "驚慄", "咆哮", "悲慟", "喪命", "哭泣", "激怒", "挑釁",
                    "傷心", "憎惡", "恐怖", "破產", "悲憤", "憎恨", "悲痛", "焦躁", "淫蕩", "焦慮",
                    "雜交", "瘟疫", "陰險", "悲傷", "野蠻", "恥辱", "悽慘", "瘋癲", "反感", "骯髒",
                    "敗類", "厭煩", "焦急", "喪事", "心煩", "卑鄙", "出殯", "噁心", "罪孽", "惡劣",
                    "下蠱", "災禍", "偏見", "笨蛋", "騙子", "邪惡", "夭折", "虛偽", "厭世", "刻薄",
                    "狂傲", "沮喪", "絕望", "貪婪", "淒涼", "悲哀", "卑劣", "陪葬", "苦惱", "嫌惡",
                    "錯亂", "畸形", "自卑", "斷氣", "殘廢", "諂媚", "白痴", "罪惡", "短命", "無能",
                    "憂傷", "窮困", "輕蔑", "墮落", "憂慮", "蔑視", "醜陋", "膽小", "病態", "腐敗",
                    "去勢", "膽怯", "哀悼", "頹廢", "貧乏", "軟弱", "意圖", "懶惰",
                    
                    "空地", "默想", "冥想", "段落", "概要", "底下", "前言", "取向", "選取", "字形",
                                "厚度", "句子", "配套", "用語", "檢閱", "思量", "屬性", "歸類", "由來", "摘要",
                                "主義", "沿途", "額外", "比喻", "時程", "循環", "通往", "預先", "要件", "收取",
                                "調節", "隨身", "見解", "演繹", "抽象", "心智", "傾向", "抽取", "考察", "起點",
                                "緣故", "提取", "交替", "回顧", "聲稱", "伸直", "換取", "擺設", "調頻", "假定",
                                "慰藉", "抽樣", "清高", "備用", "推測", "知覺", "虛擬", "伴隨", "注重", "頭腦",
                                "體積", "推論", "商議", "乾燥", "轉速", "隨機", "察覺", "散佈", "評價", "轉彎",
                                "務必", "時髦", "斷定", "揣測", "華麗", "上流",
        */

        List<string> negativeLatter = new List<string>
        {
            ">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>",">>>>>>>>>"
        };

        List<string> neutralLatter = new List<string>
        {
            ">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>",">>>><>>>>"
        };

        if (!isTest)
        {
            neutralLatter = neutralLatter.Take(30).ToList();
            negativeLatter = negativeLatter.Take(30).ToList();
            Debug.Log($"📝 正式模式：中性詞 30 個，負向詞 30 個");
        }
        else
        {
            neutralLatter = neutralLatter.Take(3).ToList();
            negativeLatter = negativeLatter.Take(3).ToList();
            Debug.Log($"🧪 測試模式：中性詞 3 個，負向詞 3 個");
        }

        (Color mid, Color other)[] colorCombos = new (Color, Color)[]
        {
            (Color.red, Color.red),
            (Color.green, Color.red),
            (Color.red, Color.green),
            (Color.green, Color.green)
        };

        foreach (var word in negativeLatter)
        {
            foreach (var (midColor, otherColor) in colorCombos)
            {
                currentData.Add(new FlankerTaskData
                {
                    currentLetter = word,
                    midColor = midColor,
                    OtherColor = otherColor,
                    isNegative = true
                });
            }
        }

        foreach (var word in neutralLatter)
        {
            foreach (var (midColor, otherColor) in colorCombos)
            {
                currentData.Add(new FlankerTaskData
                {
                    currentLetter = word,
                    midColor = midColor,
                    OtherColor = otherColor,
                    isNegative = false
                });
            }
        }

        ShuffleList(currentData);
        totalTrials = currentData.Count;
        Debug.Log($"✅ Flanker 任務初始化完成，總題數: {currentData.Count}");
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
        csv.AppendLine("Index,Letter,MidColor,OtherColor,IsNegative,IsCorrect,ResponseTime(s),ColorIsSame");

        for (int i = 0; i < currentData.Count; i++)
        {
            var data = currentData[i];
            string midColorStr = ColorToString(data.midColor);
            string otherColorStr = ColorToString(data.OtherColor);

            csv.AppendLine(
                $"{i},{data.currentLetter},{midColorStr},{otherColorStr},{data.isNegative},{data.isCorrect},{data.responseTime:F3},{data.colorIsSame}");
        }

        csv.AppendLine();
        csv.AppendLine($"總題數,{totalCount}");
        csv.AppendLine($"正確題數,{correctCount}");
        csv.AppendLine($"正確率,{accuracy:F2}%");
        csv.AppendLine($"平均反應時間（僅計算正確題）,{averageResponseTime:F3}");

        try
        {
            File.WriteAllText(path, csv.ToString());
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
    public string currentLetter;
    public Color midColor, OtherColor;
    public bool isNegative;
    public bool isCorrect;
    public bool colorIsSame;
    public float responseTime;
}