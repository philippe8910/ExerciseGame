using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.XR;
using Sirenix.OdinInspector;
using Random = UnityEngine.Random;

public class EmotionalStroopCore : MonoBehaviour
{
    [TitleGroup("圖片資源")]
    [Required, AssetsOnly]
    [LabelText("負向圖片列表")]
    public List<Sprite> negativeImageList;
    
    [Required, AssetsOnly]
    [LabelText("中性圖片列表")]
    public List<Sprite> neutralImageList;

    [TitleGroup("試次設定")]
    [LabelText("總負面圖片次數")]
    [Tooltip("此數值由程式自動計算控制，Inspector 設定無效")]
    public int totalNegativeAppearances = 360; // 720 / 2
    
    [LabelText("總試次數")]
    [MinValue(1)]
    public int totalTrials = 720;

    [LabelText("幾次試次後休息 (0 = 不休息)")]
    [MinValue(0)]
    public int trialsBeforeRest = 0;

    [TitleGroup("UI 組件")]
    [Required, SceneObjectsOnly]
    public MeshRenderer iconContainer;
    
    [Required, SceneObjectsOnly]
    public Image iconImage;
    
    [Required, SceneObjectsOnly]
    public Image crossHairImage;
    
    [Required, SceneObjectsOnly]
    public GameObject restPanel;
    
    [Required, SceneObjectsOnly]
    public GameObject endPanel;

    [TitleGroup("Prefab")]
    [Required, AssetsOnly]
    [LabelText("一致性 Prefab")]
    public GameObject congruentPrefab;
    
    [Required, AssetsOnly]
    [LabelText("不一致性 Prefab")]
    public GameObject incongruentPrefab;
    
    [Required, AssetsOnly]
    [LabelText("星星陣列 Prefab")]
    public GameObject starsArrayPrefab;

    [TitleGroup("時間設定")]
    [LabelText("反應時間限制 (秒)")]
    [MinValue(0)]
    public float responseTimeLimit = 2.0f;
    
    [LabelText("注視點顯示時間 (秒)")]
    [MinValue(0)]
    public float fixationTime = 0.5f;
    
    [LabelText("圖片顯示時間 (秒)")]
    [MinValue(0)]
    public float imageDisplayTime = 1.5f;
    
    [LabelText("刺激顯示時間 (秒)")]
    [MinValue(0)]
    public float stimulusDisplayTime = 1.5f;

    [TitleGroup("測試模式")]
    [LabelText("測試模式")]
    [Tooltip("開啟後只進行少量測試，不儲存資料")]
    public bool isTest = false;

    [TitleGroup("遊戲狀態")]
    [ReadOnly, ShowInInspector]
    private string gameStatus = "等待開始";
    
    [ReadOnly, ShowInInspector, ProgressBar(0, "totalTrials")]
    private int currentTrialIndex = 0;

    [TitleGroup("統計資訊")]
    [ReadOnly, ShowInInspector]
    private int totalCorrect = 0;
    
    // totalTrials exists as config now
    [ReadOnly, ShowInInspector]
    private int validTrialsCount = 0;
    
    [ReadOnly, ShowInInspector, SuffixLabel("%", true)]
    private float currentAccuracy = 0f;
    
    [ReadOnly, ShowInInspector, SuffixLabel("秒", true)]
    private float averageResponseTime = 0f;

    [TitleGroup("試次資料")]
    [ReadOnly, ShowInInspector]
    public List<StroopData> currentTrialList = new();
    
    private List<bool> isNegativeList = new();

    // 外部觸發數字
    private int triggerNumber = -1;

    private IEnumerator Start()
    {
        Debug.Log("🎮 Emotional Stroop 任務啟動");

        if (!ValidateComponents()) yield return null;
        
        Init();
        yield return StartCoroutine(StartExperiment());
    }

    bool ValidateComponents()
    {
        bool isValid = true;

        if (negativeImageList == null || negativeImageList.Count == 0)
        {
            Debug.LogError(" negativeImageList 未設定或為空！");
            gameStatus = "素材缺失";
            isValid = false;
        }

        if (neutralImageList == null || neutralImageList.Count == 0)
        {
            Debug.LogError(" neutralImageList 未設定或為空！");
            gameStatus = "素材缺失";
            isValid = false;
        }

        if (iconContainer == null)
        {
            Debug.LogError(" iconContainer 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (iconImage == null)
        {
            Debug.LogError(" iconImage 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (crossHairImage == null)
        {
            Debug.LogError(" crossHairImage 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (restPanel == null)
        {
            Debug.LogError(" restPanel 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (endPanel == null)
        {
            Debug.LogError(" endPanel 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (congruentPrefab == null || incongruentPrefab == null || starsArrayPrefab == null)
        {
            Debug.LogError(" Prefab 未完整設定！");
            gameStatus = "Prefab 缺失";
            isValid = false;
        }

        if (isValid)
        {
            Debug.Log(" 所有組件檢查通過");
        }

        return isValid;
    }

    [Button("重新初始化任務", ButtonSizes.Large), GUIColor(0.5f, 0.5f, 1)]
    [HideInPlayMode]
    public void Init()
    {
        iconImage.sprite = null;
        currentTrialList.Clear();
        isNegativeList.Clear();

        // 實驗設計：
        // Formal: 5 Blocks * 144 Trials = 720 Total
        // Image IDs: 1-72 (Indices 0-71)
        // Each Block: All 72 images appear once (as Negative or Neutral context, but script treats lists separately)
        // Correction: User said "Negative & Neutral 72 images each". So we assume negativeImageList has 72 and neutralImageList has 72.
        // Rule: "每張刺激物在同一個 block 內只會出現一次" -> 72 Neg + 72 Neu = 144 ? 
        // Or one set of 72 images used for both? Typically Stroop uses distinct sets or same set. 
        // "負向中性各 72 張刺激物" suggests 72 distinct Negative images and 72 distinct Neutral images. Total 144 unique images.
        // 144 trials per block matches 72 Neg + 72 Neu perfectly if each appears once.
        
        // Conditions per Block (144 trials):
        // Star: 480 / 5 = 96 trials (User wrote "Star 有 480 次嘗試次" in total 720)
        // Cong: 120 / 5 = 24 trials
        // Inc:  120 / 5 = 24 trials
        // Total: 96 + 24 + 24 = 144. Matches.
        
        // Image Distribution in Block (144 trials):
        // We have 72 Neg images and 72 Neu images. Total 144.
        // We need to map these 144 images to the 144 trials (96 Star, 24 Cong, 24 Inc).
        // Proportions:
        // Neg: 48 Star, 12 Cong, 12 Inc = 72
        // Neu: 48 Star, 12 Cong, 12 Inc = 72
        // Total: 96 Star, 24 Cong, 24 Inc. Matches.

        if (isTest) // Practice Mode
        {
            // 練習階段 24 題
            // 六種情境 (3*2) 各 4 次 -> Star/Neg:4, Star/Neu:4, Cong/Neg:4, Cong/Neu:4, Inc/Neg:4, Inc/Neu:4
            // Total: 12 Neg, 12 Neu.
            // Images: #73-#78 (Indices 72-77), 6 images each.
            // Each image repeated 2 times. 6 * 2 = 12. Matches.
            
            Debug.Log("初始化：練習模式 (Practice)");
            totalTrials = 24;
            trialsBeforeRest = 0; // 練習通常不休息，或結束後休息

            List<int> practiceIndices = new List<int> { 72, 73, 74, 75, 76, 77 }; // 假設 list 足夠長
            
            // Generate Practice Trials
            List<StroopData> practiceTrials = GenerateBlockTrials(
                starCount: 8, congCount: 8, incCount: 8, // Total 24
                negImages: GetPracticeImages(negativeImageList, practiceIndices),
                neuImages: GetPracticeImages(neutralImageList, practiceIndices)
            );
            
            currentTrialList = practiceTrials;
        }
        else // Formal Mode
        {
            Debug.Log("初始化：正式模式 (Formal)");
            totalTrials = 720;
            trialsBeforeRest = 144; // 144題休息一次

            int blockCount = 5;
            // Config per block
            int starPerBlock = 96;
            int congPerBlock = 24;
            int incPerBlock = 24;
            
            for (int b = 0; b < blockCount; b++)
            {
                // Each block uses all 72 Neg and 72 Neu images exactly once
                // Indices 0-71
                List<Sprite> blockNegs = negativeImageList.Take(72).ToList();
                List<Sprite> blockNeus = neutralImageList.Take(72).ToList();
                
                // Shuffle images to assign randomly to conditions
                Shuffle(blockNegs);
                Shuffle(blockNeus);
                
                List<StroopData> blockTrials = GenerateBlockTrials(
                    starPerBlock, congPerBlock, incPerBlock,
                    blockNegs, blockNeus
                );
                
                currentTrialList.AddRange(blockTrials);
            }
        }

        totalNegativeAppearances = currentTrialList.Count(d => d.isNegative);

        Debug.Log($" Stroop 任務初始化完成");
        Debug.Log($" 模式: {(isTest ? "練習" : "正式")}");
        Debug.Log($" 總試次數: {currentTrialList.Count}");
        Debug.Log($" 休息間隔: {trialsBeforeRest}");
    }

    private List<Sprite> GetPracticeImages(List<Sprite> source, List<int> indices)
    {
        List<Sprite> images = new List<Sprite>();
        foreach (int idx in indices)
        {
            if (idx < source.Count) images.Add(source[idx]);
        }
        // Repeat twice to get 12 images from 6
        var result = new List<Sprite>(images);
        result.AddRange(images); 
        return result; 
        // Note: result size should be 12 if indices valid.
    }

    private List<StroopData> GenerateBlockTrials(int starCount, int congCount, int incCount, List<Sprite> negImages, List<Sprite> neuImages)
    {
        // Total images needed: Star+Cong+Inc (half Neg, half Neu)
        // Input lists shouls match requirements
        
        List<StroopData> blockList = new List<StroopData>();
        
        int negIndex = 0;
        int neuIndex = 0;

        // 1. Star
        // Half Neg, Half Neu
        for (int i = 0; i < starCount; i++)
        {
            bool isNeg = (i < starCount / 2);
            blockList.Add(new StroopData 
            { 
                type = StroopType.StarsArray, 
                isNegative = isNeg,
                assignedSprite = isNeg ? negImages[negIndex++] : neuImages[neuIndex++]
            });
        }

        // 2. Congruent
        for (int i = 0; i < congCount; i++)
        {
            bool isNeg = (i < congCount / 2);
            blockList.Add(new StroopData 
            { 
                type = StroopType.Congruent, 
                isNegative = isNeg,
                assignedSprite = isNeg ? negImages[negIndex++] : neuImages[neuIndex++]
            });
        }

        // 3. Incongruent
        for (int i = 0; i < incCount; i++)
        {
            bool isNeg = (i < incCount / 2);
            blockList.Add(new StroopData 
            { 
                type = StroopType.Incongruent, 
                isNegative = isNeg,
                assignedSprite = isNeg ? negImages[negIndex++] : neuImages[neuIndex++]
            });
        }

        Shuffle(blockList);
        return blockList;
    }

    private IEnumerator StartExperiment()
    {
        gameStatus = "準備中";
        yield return StartCoroutine(WaitForGameStart());

            yield return StartCoroutine(RunBlock(currentTrialList));

        gameStatus = "測試完成";
        ShowFinalResult();
    }

    private IEnumerator RunBlock(List<StroopData> trialList)
    {
        for (int i = 0; i < trialList.Count; i++)
        {
            currentTrialIndex = i + 1;
            StroopData data = trialList[i];

            Debug.Log($"▶ 試次 {currentTrialIndex}/{trialList.Count}");

            // 顯示注視點
            crossHairImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(fixationTime);
            crossHairImage.gameObject.SetActive(false);

            // 顯示圖片（負向或中性）
            iconImage.gameObject.SetActive(true);
            // SetImageForTrial(data, data.isNegative); // Removed, using pre-assigned
            iconImage.sprite = data.assignedSprite;
            Debug.Log($"  圖片: {(data.isNegative ? "負向" : "中性")} - {data.assignedSprite?.name}");
            yield return new WaitForSeconds(imageDisplayTime);
            iconImage.gameObject.SetActive(false);
            iconImage.sprite = null;

            // 實例化刺激 Prefab
            GameObject stimulusObject = InstantiateTrialPrefab(data.type);
            if (stimulusObject == null)
            {
                Debug.LogError($" 無法實例化 {data.type} Prefab！");
                continue;
            }

            stimulusObject.transform.SetParent(iconContainer.transform, false);
            stimulusObject.transform.localPosition = Vector3.up * 0.05f;
            stimulusObject.transform.localRotation = Quaternion.identity;
            stimulusObject.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(stimulusDisplayTime);
            stimulusObject.SetActive(false);

            // 獲取正確答案
            int correctCount = GetCorrectAnswer(stimulusObject, data.type);
            Debug.Log($"  類型: {data.type}, 正確答案: {correctCount}");

            // 等待反應
            float startTime = Time.time;
            bool responded = false;
            triggerNumber = -1; // 重置觸發數字

            while (Time.time - startTime < responseTimeLimit)
            {
                if (triggerNumber != -1)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = (triggerNumber == correctCount);
                    responded = true;

                    if (data.isCorrect)
                        Debug.Log($"  ✓ 正確反應: {triggerNumber}, 反應時間: {data.responseTime:F3}s");
                    else
                        Debug.Log($"  ✗ 錯誤反應: {triggerNumber} (正確答案: {correctCount}), 反應時間: {data.responseTime:F3}s");

                    break;
                }

                yield return null;
            }

            // 未反應 = 超時
            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = responseTimeLimit;
                Debug.Log($"  ⏱ 超時: {data.responseTime:F3}s - 未反應");
            }

            // 更新統計
            UpdateStatistics();

            Destroy(stimulusObject);
            triggerNumber = -1; // 重置觸發數字

            // 檢查是否需要休息
            // 如果 trialsBeforeRest > 0 且 當前試次是 trialsBeforeRest 的倍數
            // 且 不是本 Block 的最後一次試次 (避免與 Block 間的休息重疊)
            if (trialsBeforeRest > 0 && (i + 1) < trialList.Count && (i + 1) % trialsBeforeRest == 0)
            {
                gameStatus = "休息中";
                restPanel.SetActive(true);
                Debug.Log($"已進行 {i + 1} 次試次，進入階段性休息。請按下雙手 Trigger 繼續");
                yield return StartCoroutine(WaitForBothHandsTrigger());
                restPanel.SetActive(false);
                gameStatus = $"進行中: {i + 1}/{trialList.Count}";
            }
        }
    }

    private IEnumerator WaitForGameStart()
    {
        Debug.Log("⏰ 等待 5 秒後開始 Stroop 任務");
        yield return new WaitForSeconds(5);
        yield return null;
    }

    private IEnumerator WaitForBothHandsTrigger()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool leftPressed = false, rightPressed = false;

        while (!(leftPressed && rightPressed))
        {
            left.TryGetFeatureValue(CommonUsages.triggerButton, out leftPressed);
            right.TryGetFeatureValue(CommonUsages.triggerButton, out rightPressed);
            yield return null;
        }

        Debug.Log("✓ 雙手 Trigger 已按下，繼續實驗");
    }

    // SetImageForTrial Removed - Handled in Init via assignedSprite

    private GameObject InstantiateTrialPrefab(StroopType type)
    {
        return type switch
        {
            StroopType.Congruent => Instantiate(congruentPrefab),
            StroopType.Incongruent => Instantiate(incongruentPrefab),
            StroopType.StarsArray => Instantiate(starsArrayPrefab),
            _ => null
        };
    }

    private int GetCorrectAnswer(GameObject stimulusObject, StroopType type)
    {
        return type switch
        {
            StroopType.Congruent => stimulusObject.GetComponent<NumBackground>()?.enableNumber ?? -1,
            StroopType.Incongruent => stimulusObject.GetComponent<RandomNumBackground>()?.enableNumber ?? -1,
            StroopType.StarsArray => stimulusObject.GetComponent<RandomStarBackground>()?.enabledCount ?? -1,
            _ => -1
        };
    }

    void UpdateStatistics()
    {
        validTrialsCount = currentTrialList.Count(d => d.responseTime > 0);
        totalCorrect = currentTrialList.Count(d => d.isCorrect);
        currentAccuracy = validTrialsCount > 0 ? (float)totalCorrect / validTrialsCount * 100f : 0f;
        averageResponseTime = currentTrialList.Where(d => d.isCorrect).Select(d => d.responseTime).DefaultIfEmpty(0).Average();
    }

    private void ShowFinalResult()
    {
        int total = currentTrialList.Count;
        int correct = currentTrialList.Count(d => d.isCorrect);
        float accuracy = (float)correct / total * 100f;
        float avgTime = currentTrialList.Where(d => d.isCorrect).Select(d => d.responseTime).DefaultIfEmpty(0).Average();

        Debug.Log("======= Stroop 任務完成！統計結果： =======");
        Debug.Log($"正確率：{correct}/{total}（{accuracy:F2}%）");
        Debug.Log($"平均反應時間（正確題）：{avgTime:F3} 秒");

        endPanel.SetActive(true);
        ExportStroopResultsToCSV();
    }

    /// <summary>
    /// 外部設定觸發數字（用於接收手勢或按鈕輸入）
    /// </summary>
    public void SetTriggerNumber(int number)
    {
        triggerNumber = number;
        Debug.Log($"觸發數字: {number}");
    }

    public void ExportStroopResultsToCSV()
    {
        // 測試模式下不儲存資料
        if (isTest)
        {
            Debug.Log("測試模式：不儲存 CSV 資料");
            return;
        }

        // 獲取受測者 ID
        string participantID = PlayerPrefs.GetString("ID", "Unknown");

        string path;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android/Oculus 環境：儲存到 persistentDataPath/StroopTestData 資料夾
         // 路徑通常是 /storage/emulated/0/Android/data/<package_name>/files/StroopTestData
        string downloadFolder = Path.Combine(Application.persistentDataPath, "StroopTestData");
        
        // 確保資料夾存在
        if (!Directory.Exists(downloadFolder))
        {
            try
            {
                Directory.CreateDirectory(downloadFolder);
                Debug.Log($" 建立資料夾: {downloadFolder}");
            }
            catch (Exception e)
            {
                Debug.LogError($" 無法建立資料夾: {e.Message}");
                // 如果無法建立資料夾，直接存在根目錄
                downloadFolder = Application.persistentDataPath;
            }
        }
        
        path = Path.Combine(downloadFolder, "StroopResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
#else
        // Unity Editor 或其他平台：儲存到 Application.dataPath
        string dataFolder = Application.dataPath + "/StroopTestData";

        // 確保資料夾存在
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Debug.Log($" 建立資料夾: {dataFolder}");
        }

        path = dataFolder + "/StroopResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Index,Type,IsNegative,IsCorrect,ResponseTime(s)");

        int correctCount = 0;
        float totalResponseTime = 0f;
        int totalCount = currentTrialList.Count;

        for (int i = 0; i < currentTrialList.Count; i++)
        {
            var data = currentTrialList[i];
            string typeStr = data.type.ToString();

            if (data.isCorrect)
            {
                correctCount++;
                totalResponseTime += data.responseTime;
            }

            csv.AppendLine($"{i},{typeStr},{data.isNegative},{data.isCorrect},{data.responseTime:F3}");
        }

        float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        float averageRT = correctCount > 0 ? totalResponseTime / correctCount : 0f;

        csv.AppendLine();
        csv.AppendLine($"總題數,{totalCount}");
        csv.AppendLine($"正確題數,{correctCount}");
        csv.AppendLine($"正確率,{accuracy:F2}%");
        csv.AppendLine($"平均反應時間（僅計算正確題）,{averageRT:F3}");

        try
        {
            File.WriteAllText(path, csv.ToString());
            Debug.Log($"Stroop CSV 已儲存至: {path}");
            Debug.Log($"受測者 ID: {participantID}");
        }
        catch (Exception e)
        {
            Debug.LogError($" 無法寫入Stroop CSV: {e.Message}");
        }
    }

    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int j = Random.Range(i, n);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public enum StroopType
{
    Congruent,      // 一致性
    Incongruent,    // 不一致性
    StarsArray      // 星星陣列
}

[System.Serializable]
public class StroopData
{
    public StroopType type;
    public bool isCorrect;
    public bool isNegative;
    public Sprite assignedSprite;
    public float responseTime;
}