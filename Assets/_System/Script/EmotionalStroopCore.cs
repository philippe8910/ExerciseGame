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
    [ReadOnly]
    public int totalNegativeAppearances = 360; // 720 / 2
    
    [LabelText("每個 Block 的試次數")]
    [MinValue(1)]
    public int trialsPerBlock = 144;
    
    [LabelText("總 Block 數")]
    [MinValue(1)]
    public int totalBlocks = 5;

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
    
    [ReadOnly, ShowInInspector]
    private int currentBlock = 0;
    
    [ReadOnly, ShowInInspector, ProgressBar(0, "trialsPerBlock")]
    private int currentTrialInBlock = 0;

    [TitleGroup("統計資訊")]
    [ReadOnly, ShowInInspector]
    private int totalCorrect = 0;
    
    [ReadOnly, ShowInInspector]
    private int totalTrials = 0;
    
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
            Debug.LogError("❌ negativeImageList 未設定或為空！");
            gameStatus = "素材缺失";
            isValid = false;
        }

        if (neutralImageList == null || neutralImageList.Count == 0)
        {
            Debug.LogError("❌ neutralImageList 未設定或為空！");
            gameStatus = "素材缺失";
            isValid = false;
        }

        if (iconContainer == null)
        {
            Debug.LogError("❌ iconContainer 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (iconImage == null)
        {
            Debug.LogError("❌ iconImage 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (crossHairImage == null)
        {
            Debug.LogError("❌ crossHairImage 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (restPanel == null)
        {
            Debug.LogError("❌ restPanel 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (endPanel == null)
        {
            Debug.LogError("❌ endPanel 未綁定！");
            gameStatus = "組件缺失";
            isValid = false;
        }

        if (congruentPrefab == null || incongruentPrefab == null || starsArrayPrefab == null)
        {
            Debug.LogError("❌ Prefab 未完整設定！");
            gameStatus = "Prefab 缺失";
            isValid = false;
        }

        if (isValid)
        {
            Debug.Log("✅ 所有組件檢查通過");
        }

        return isValid;
    }

    [Button("重新初始化任務", ButtonSizes.Large), GUIColor(0.5f, 0.5f, 1)]
    [HideInPlayMode]
    public void Init()
    {
        iconImage.sprite = null;
        currentTrialList.Clear();
        // isNegativeList 不再使用，因為狀態直接存在 StroopData 中
        isNegativeList.Clear();

        if (isTest)
        {
            totalBlocks = 1;
            trialsPerBlock = 12; // 測試用少量: 4 Star, 2 Cong, 2 Inc (x2 emotions) -> 8+4 ? no. 
            // 簡化測試: Star 4 (2N, 2Neg), Cong 2 (1N, 1Neg), Inc 2 (1N, 1Neg) -> Total 8
            Debug.Log($"🧪 測試模式：Block 數 = 1, 少量試次");
        }

        int starPerBlock = 96;
        int congPerBlock = 24;
        int incPerBlock = 24;

        if (isTest)
        {
            starPerBlock = 4;
            congPerBlock = 2;
            incPerBlock = 2;
            trialsPerBlock = starPerBlock + congPerBlock + incPerBlock;
        }

        int totalTrialCount = totalBlocks * trialsPerBlock;
        int actualNegativeCount = 0;

        for (int b = 0; b < totalBlocks; b++)
        {
            List<StroopData> blockList = new List<StroopData>();

            // 1. Star (StarsArray)
            // 50% Neutral, 50% Negative
            for (int i = 0; i < starPerBlock; i++)
            {
                StroopData data = new StroopData
                {
                    type = StroopType.StarsArray,
                    isNegative = (i < starPerBlock / 2) // 前半負向，後半中性 (之後會shuffle)
                };
                blockList.Add(data);
            }

            // 2. Congruent
            for (int i = 0; i < congPerBlock; i++)
            {
                StroopData data = new StroopData
                {
                    type = StroopType.Congruent,
                    isNegative = (i < congPerBlock / 2)
                };
                blockList.Add(data);
            }

            // 3. Incongruent
            for (int i = 0; i < incPerBlock; i++)
            {
                StroopData data = new StroopData
                {
                    type = StroopType.Incongruent,
                    isNegative = (i < incPerBlock / 2)
                };
                blockList.Add(data);
            }

            // Shuffle Block
            Shuffle(blockList);
            
            // Add to main list
            currentTrialList.AddRange(blockList);
            
            // Count
            actualNegativeCount += blockList.Count(d => d.isNegative);
        }
        
        totalNegativeAppearances = actualNegativeCount;

        Debug.Log($"✅ Stroop 任務初始化完成");
        Debug.Log($"📝 總 Block 數: {totalBlocks}, 每 Block 試次數: {trialsPerBlock}, 總試次數: {currentTrialList.Count}");
        Debug.Log($"   (Star: {currentTrialList.Count(x => x.type == StroopType.StarsArray)}, Cong: {currentTrialList.Count(x => x.type == StroopType.Congruent)}, Inc: {currentTrialList.Count(x => x.type == StroopType.Incongruent)})");
        Debug.Log($"🖼️ 負向圖片總數: {totalNegativeAppearances}");
    }

    private IEnumerator StartExperiment()
    {
        gameStatus = "準備中";
        yield return StartCoroutine(WaitForGameStart());

        for (int block = 0; block < totalBlocks; block++)
        {
            currentBlock = block + 1;
            gameStatus = $"Block {currentBlock}/{totalBlocks} 進行中";
            Debug.Log($"🚩 Block {currentBlock}/{totalBlocks} 開始");

            var blockTrials = currentTrialList.Skip(block * trialsPerBlock).Take(trialsPerBlock).ToList();
            // var blockNegatives = isNegativeList.Skip(block * trialsPerBlock).Take(trialsPerBlock).ToList(); // 不再需要

            yield return StartCoroutine(RunBlock(blockTrials));

            if (block < totalBlocks - 1)
            {
                gameStatus = "休息中";
                restPanel.SetActive(true);
                Debug.Log("🛋️ 請休息，同時按下雙手 Trigger 開始下一回合");
                yield return StartCoroutine(WaitForBothHandsTrigger());
                restPanel.SetActive(false);
            }
        }

        gameStatus = "測試完成";
        ShowFinalResult();
    }

    private IEnumerator RunBlock(List<StroopData> trialList)
    {
        for (int i = 0; i < trialList.Count; i++)
        {
            currentTrialInBlock = i + 1;
            StroopData data = trialList[i];

            Debug.Log($"▶ Block {currentBlock}, 試次 {currentTrialInBlock}/{trialsPerBlock}");

            // 顯示注視點
            crossHairImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(fixationTime);
            crossHairImage.gameObject.SetActive(false);

            // 顯示圖片（負向或中性）
            iconImage.gameObject.SetActive(true);
            SetImageForTrial(data, data.isNegative);
            Debug.Log($"  圖片: {(data.isNegative ? "負向" : "中性")}");
            yield return new WaitForSeconds(imageDisplayTime);
            iconImage.gameObject.SetActive(false);
            iconImage.sprite = null;

            // 實例化刺激 Prefab
            GameObject stimulusObject = InstantiateTrialPrefab(data.type);
            if (stimulusObject == null)
            {
                Debug.LogError($"❌ 無法實例化 {data.type} Prefab！");
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

    private void SetImageForTrial(StroopData data, bool isNegative)
    {
        data.isNegative = isNegative;
        if (isNegative)
        {
            iconImage.sprite = negativeImageList[Random.Range(0, negativeImageList.Count)];
        }
        else
        {
            iconImage.sprite = neutralImageList[Random.Range(0, neutralImageList.Count)];
        }
    }

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
        totalTrials = currentTrialList.Count(d => d.responseTime > 0);
        totalCorrect = currentTrialList.Count(d => d.isCorrect);
        currentAccuracy = totalTrials > 0 ? (float)totalCorrect / totalTrials * 100f : 0f;
        averageResponseTime = currentTrialList.Where(d => d.isCorrect).Select(d => d.responseTime).DefaultIfEmpty(0).Average();
    }

    private void ShowFinalResult()
    {
        int total = currentTrialList.Count;
        int correct = currentTrialList.Count(d => d.isCorrect);
        float accuracy = (float)correct / total * 100f;
        float avgTime = currentTrialList.Where(d => d.isCorrect).Select(d => d.responseTime).DefaultIfEmpty(0).Average();

        Debug.Log("======= ✅ Stroop 任務完成！統計結果： =======");
        Debug.Log($"🎯 正確率：{correct}/{total}（{accuracy:F2}%）");
        Debug.Log($"⏱️ 平均反應時間（正確題）：{avgTime:F3} 秒");

        endPanel.SetActive(true);
        ExportStroopResultsToCSV();
    }

    /// <summary>
    /// 外部設定觸發數字（用於接收手勢或按鈕輸入）
    /// </summary>
    public void SetTriggerNumber(int number)
    {
        triggerNumber = number;
        Debug.Log($"🔢 觸發數字: {number}");
    }

    public void ExportStroopResultsToCSV()
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
        // Android/Oculus 環境：儲存到 Download/StroopTestData 資料夾
        string downloadFolder = "/storage/emulated/0/Download/StroopTestData";
        
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
        
        path = downloadFolder + "/StroopResults_" + participantID + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
        // Unity Editor 或其他平台：儲存到 Application.dataPath
        string dataFolder = Application.dataPath + "/StroopTestData";

        // 確保資料夾存在
        if (!Directory.Exists(dataFolder))
        {
            Directory.CreateDirectory(dataFolder);
            Debug.Log($"📁 建立資料夾: {dataFolder}");
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
            Debug.Log($"✅ Stroop CSV 已儲存至: {path}");
            Debug.Log($"👤 受測者 ID: {participantID}");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 無法寫入Stroop CSV: {e.Message}");
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
    public float responseTime;
}