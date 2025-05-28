using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine.XR;
using Random = UnityEngine.Random;

[System.Serializable]
public class TrialTypeInfo
{
    public Sprite sprite;
    public int correctCount;
}

public class EmotionalStroopCore : MonoBehaviour
{
    [Header("圖片資源")]
    public List<Sprite> negativeImageList;
    public List<Sprite> neutralImageList;

    [Tooltip("總負面圖片次數 (720 trials 中)")]
    public int totalNegativeAppearances = 360;

    [Header("UI 元件")]
    public MeshRenderer iconContainer;
    public Image iconImage;
    public Image crossHairImage;
    public GameObject restPanel;
    public GameObject endPanel;

    [Header("Prefab")] 
    public GameObject congruentPrefab, incongruentPrefab, starsArrayPrefab;

    [Header("設定")]
    public float timeInterval = 2.0f;

    private int totalBlocks = 5;
    private int trialsPerBlock = 144;

    public List<StroopData> currentTrialList = new();
    private List<bool> isNegativeList = new();

    public bool isTest = false;

    private IEnumerator Start()
    {
        Init();
        yield return StartCoroutine(StartExperiment());
    }

    public void Init()
    {
        iconImage.sprite = null;

        if (isTest)
        {
            totalBlocks = 1;
            trialsPerBlock = 10;
        }
        

        // 建立所有 trials
        for (int i = 0; i < totalBlocks * trialsPerBlock; i++)
        {
            StroopData data = new StroopData();
            data.type = (StroopType)(i % 3); // 輪流填充類型
            currentTrialList.Add(data);
        }

        for (int i = 0; i < totalBlocks * trialsPerBlock; i++)
        {
            isNegativeList.Add(false);
        }
        for (int i = 0; i < totalNegativeAppearances; i++)
        {
            isNegativeList[i] = true;
        }

        Shuffle(currentTrialList);
        Shuffle(isNegativeList);
    }

    private IEnumerator StartExperiment()
    {
        yield return StartCoroutine(waitForGameStart());
        
        for (int block = 0; block < totalBlocks; block++)
        {
            Debug.Log($"🚩 Block {block + 1} 開始");

            var blockTrials = currentTrialList.Skip(block * trialsPerBlock).Take(trialsPerBlock).ToList();
            var blockNegatives = isNegativeList.Skip(block * trialsPerBlock).Take(trialsPerBlock).ToList();

            yield return StartCoroutine(RunBlock(blockTrials, blockNegatives));

            if (block < totalBlocks - 1)
            {
                restPanel.SetActive(true);
                Debug.Log("🛋️ 請休息並同時按下雙手 Trigger 開始下一回合");
                yield return StartCoroutine(WaitForBothHandsTrigger());
                restPanel.SetActive(false);
            }
        }

        ShowFinalResult();
    }

    private IEnumerator RunBlock(List<StroopData> trialList, List<bool> negativeList)
    {
        for (int i = 0; i < trialList.Count; i++)
        {
            StroopData data = trialList[i];

            crossHairImage.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
            crossHairImage.gameObject.SetActive(false);

            iconImage.gameObject.SetActive(true);
            SetImageForTrial(data, negativeList[i]);
            yield return new WaitForSeconds(1.5f);
            iconImage.gameObject.SetActive(false);
            iconImage.sprite = null;

            GameObject g = InstantiateTrialPrefab(data.type);
            g.transform.SetParent(iconContainer.transform, false);
            g.transform.localPosition = Vector3.up * 0.05f;
            g.transform.localRotation = Quaternion.identity;
            g.transform.localScale = Vector3.one;

            yield return new WaitForSeconds(1.5f);
            g.SetActive(false);

            float startTime = Time.time;
            bool responded = false;

            int correctCount = -1;
            
            switch (data.type)
            {
                case StroopType.Congruent:
                    correctCount = g.GetComponent<NumBackground>().enableNumber;
                    break;
                case StroopType.Incongruent:
                    correctCount = g.GetComponent<RandomNumBackground>().enableNumber;
                    break;
                case StroopType.StarsArray:
                    correctCount = g.GetComponent<RandomStarBackground>().enabledCount;
                    break;
            }

            while (Time.time - startTime < timeInterval)
            {
                if (triggerNumber != -1 && triggerNumber == correctCount)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }
                else
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false;
                }
                yield return null;
            }

            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = timeInterval;
            }

            Destroy(g);
        }
    }
    
    private IEnumerator waitForGameStart()
    {
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

    private void ShowFinalResult()
    {
        int total = currentTrialList.Count;
        int correct = currentTrialList.Count(d => d.isCorrect);
        float accuracy = (float)correct / total * 100f;
        float avgTime = currentTrialList.Where(d => d.isCorrect).Select(d => d.responseTime).DefaultIfEmpty(0).Average();

        Debug.Log("🎉 實驗完成！");
        Debug.Log($"🎯 正確率：{correct}/{total}（{accuracy:F2}%）");
        Debug.Log($"⏱ 平均反應時間：{avgTime:F2} 秒");
        
        endPanel.SetActive(true);
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
    
    public int triggerNumber = -1;

    public void SetTriggerNumber(int i)
    {
        triggerNumber = i;
    }
    
    public void ExportStroopResultsToCSV()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    string path = "/storage/emulated/0/Download/StroopResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
        string path = Application.dataPath + "/StroopResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "-" + PlayerPrefs.GetString("ID") + ".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Index,Type,IsNegative,IsCorrect,ResponseTime");

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
        csv.AppendLine($"平均反應時間（僅計算正確題）, {averageRT:F3} 秒");

        try
        {
            File.WriteAllText(path, csv.ToString());
            Debug.Log("✅ Stroop CSV 已儲存至: " + path);
        }
        catch (System.Exception e)
        {
            Debug.LogError("❌ 無法寫入Stroop CSV: " + e.Message);
        }
    }
}

public enum StroopType
{
    Congruent,
    Incongruent,
    StarsArray
}

[System.Serializable]
public class StroopData
{
    public StroopType type;
    public bool isCorrect;
    public bool isNegative;
    public float responseTime;
}
