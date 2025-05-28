// 整合完整版的 Adaptive N-back 任務腳本

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

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
    public string visualStimulusType;
    public string audioStimulusType;
}

public class NBackGame : MonoBehaviour
{
    [Header("準備時間")] 
    public float waitTime = 10f;
    
    [Header("n-back 設定")] [Range(1, 3)] public int n = 2;
    public int totalTrials = 20;
    public float[] stimulusInterval;
    public int visualTrials = 5;
    public int audioTrials = 5;
    public int bothTrials = 2;
    public int totalVisualStimuli = 0;
    public int totalAudioStimuli = 0;

    [Header("九宮格(視覺)設定")] public GameObject[] gridPlanes;
    [Header("結束面板")] public GameObject endPanel;
    [Header("N數字")] public TMP_Text nText;

    [Header("聲音(聽覺)設定")] public AudioSource audioSource;
    public List<AudioClip> audioClipsStimuli, audioClipsNormal, audioClips;
    public List<Sprite> stimuliSprites, normalSprites, visualAllSprites;

    [Header("玩家按鍵設定")] public KeyCode visualKey = KeyCode.Space;
    public KeyCode audioKey = KeyCode.Z;

    [Header("UI 提示物件")] public GameObject restPanel;

    public List<int> visualIDList = new();
    public List<int> audioIDList = new();
    public List<bool> visualResponseList = new();
    public List<bool> audioResponseList = new();
    public List<TrialResult> trialResults = new();

    private List<float> visualAccuracyRecord = new();
    private List<float> audioAccuracyRecord = new();
    private List<int> nRecord = new();

    public bool isVisualCheck, isAudioCheck;

    private int visualHit, visualMiss, visualFalseAlarm, visualCorrectRejection;
    private int audioHit, audioMiss, audioFalseAlarm, audioCorrectRejection;
    private int currentAudioStimuli = 0, currentVisualStimuli = 0;

    private List<Sprite> _stimuliSprites = new();
    private List<AudioClip> _audioClipsStimuli = new();

    public LineRenderer lineRenderer;
    public LineRenderer lineRenderer2;
    
    public bool isTest = false;

    void Start()
    {
        if (gridPlanes == null || gridPlanes.Length == 0 || audioSource == null)
        {
            Debug.LogError("請設定必要的元件！");
            return;
        }

        lineRenderer.enabled = false;
        lineRenderer2.enabled = false;

        StartCoroutine(MultiRoundGame());
    }

    private void Init()
    {
        bool success = false;
        totalTrials += n;

        _stimuliSprites = stimuliSprites.ToList();
        _audioClipsStimuli = audioClipsStimuli.ToList();

        Shuffle(normalSprites);
        Shuffle(stimuliSprites);

        Shuffle(audioClipsStimuli);
        Shuffle(audioClipsNormal);

        for (int i = 0; i < 6; i++)
        {
            visualAllSprites.Add(normalSprites[0]);
            visualAllSprites.Add(stimuliSprites[0]);

            normalSprites.RemoveAt(0);
            stimuliSprites.RemoveAt(0);
        }

        for (int i = 0; i < 6; i++)
        {
            audioClips.Add(audioClipsNormal[0]);
            audioClips.Add(audioClipsStimuli[0]);

            audioClipsStimuli.RemoveAt(0);
            audioClipsNormal.RemoveAt(0);
        }

        Shuffle(visualAllSprites);
        Shuffle(audioClips);

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
                audioIDList.Add(Random.Range(0, audioClips.Count));
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
                            int g = Random.Range(0, audioClips.Count);
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

    private IEnumerator MultiRoundGame()
    {
        Init();

        int roundCount = isTest ? 1 : 3;

        for (int round = 0; round < roundCount; round++)
        {
            yield return StartCoroutine(waitForGameStart());
            Debug.Log($"▶️ 開始第 {round + 1} 輪，n = {n}");
            yield return StartCoroutine(GameLoop());

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

            if ((visualAcc + audioAcc) / 2f >= 0.5f)
                n = Mathf.Min(3, n + 1);
            else
                n = Mathf.Max(1, n - 1);
            
            totalTrials = 20 + n;

            if (round < 2 && !isTest)
            {
                restPanel.SetActive(true);
                Debug.Log("🛋️ 請休息，按下雙手 Trigger 繼續");
                yield return StartCoroutine(WaitForBothHandsTrigger());
                restPanel.SetActive(false);
            }
            
            
        }

        Debug.Log("✅ 三輪測試完成結果：");
        lineRenderer.enabled = true;
        lineRenderer2.enabled = true;

        endPanel.SetActive(true);
        for (int i = 0; i < visualAccuracyRecord.Count; i++)
        {
            Debug.Log(
                $"📊 第{i + 1}輪：n = {nRecord[i]}, 視覺 {visualAccuracyRecord[i] * 100f:F2}%, 聽覺 {audioAccuracyRecord[i] * 100f:F2}%");
        }

        ExportTrialResultsToCSV();
    }

    private IEnumerator WaitForBothHandsTrigger()
    {
        //InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        //InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        //bool rightPressed = false;

        //while (!Input.GetKeyDown(KeyCode.Space)) //(leftPressed && rightPressed)
        //{
            //left.TryGetFeatureValue(CommonUsages.triggerButton, out leftPressed);
        //    right.TryGetFeatureValue(CommonUsages.triggerButton, out rightPressed);
        //    yield return null;
        //}

        yield return new WaitForSeconds(120);
        yield return null;
    }

    private IEnumerator waitForGameStart()
    {
        yield return new WaitForSeconds(waitTime);
        yield return null;
    }

    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int j = UnityEngine.Random.Range(i, n);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    IEnumerator GameLoop()
    {
        Debug.Log("🎮 遊戲開始！");
        //trialResults.Clear();
        visualHit = visualMiss = visualFalseAlarm = visualCorrectRejection = 0;
        audioHit = audioMiss = audioFalseAlarm = audioCorrectRejection = 0;

        int vistualStimuliIndex = 0, audioStimuliIndex = 0;
        
        nText.text = "N = " + n;

        for (int i = 0; i < totalTrials; i++)
        {
            float interval = stimulusInterval[Random.Range(0, stimulusInterval.Length)];
            int vID = visualIDList[i], aID = audioIDList[i];

            isVisualCheck = isAudioCheck = false;

            foreach (var plane in gridPlanes)
                plane.GetComponent<Renderer>().material.SetTexture("_BaseMap", null);

            bool isVisualOrFutureVisual =
                visualResponseList[i] || (i + n < visualResponseList.Count && visualResponseList[i + n]);
            Sprite currentSprite;

            if (isVisualOrFutureVisual)
            {
                if (this.totalVisualStimuli > currentVisualStimuli)
                {
                    currentSprite = stimuliSprites[vistualStimuliIndex];
                    gridPlanes[vID].GetComponent<Renderer>().material
                        .SetTexture("_BaseMap", stimuliSprites[vistualStimuliIndex].texture);
                    currentVisualStimuli++;
                }
                else
                {
                    currentSprite = normalSprites[vistualStimuliIndex];
                    gridPlanes[vID].GetComponent<Renderer>().material
                        .SetTexture("_BaseMap", normalSprites[vistualStimuliIndex].texture);
                }
            }
            else
            {
                int r = Random.Range(0, visualAllSprites.Count);

                currentSprite = visualAllSprites[r];
                gridPlanes[vID].GetComponent<Renderer>().material.SetTexture("_BaseMap", visualAllSprites[r].texture);
            }

            bool isAudioOrFutureAudio =
                audioResponseList[i] || (i + n < audioResponseList.Count && audioResponseList[i + n]);

            if (isAudioOrFutureAudio)
            {
                if (this.totalAudioStimuli > currentAudioStimuli)
                {
                    audioSource.clip = audioClipsStimuli[aID];
                    currentAudioStimuli++;
                }
                else
                {
                    audioSource.clip = audioClipsNormal[aID];
                }
            }
            else
            {
                audioSource.clip = audioClips[aID];
            }

            audioSource.Play();

            bool isAudioSimilar = _audioClipsStimuli.Contains(audioSource.clip);
            bool isVisualSimilar = _stimuliSprites.Contains(currentSprite);

            bool visualPressed = false, audioPressed = false;
            float visualRT = -1f, audioRT = -1f;
            float timer = 0f;

            while (timer < interval)
            {
                if (!visualPressed && isVisualCheck)
                {
                    visualPressed = true;
                    visualRT = timer;
                    isVisualCheck = false;
                }

                if (!audioPressed && isAudioCheck)
                {
                    audioPressed = true;
                    audioRT = timer;
                    isAudioCheck = false;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            TrialResult result = new TrialResult
            {
                trialIndex = i,
                isVisualStimulus = visualResponseList[i],
                isAudioStimulus = audioResponseList[i],
                visualReactionTime = visualRT,
                audioReactionTime = audioRT,
                visualStimulusType = isVisualSimilar ? "負面" : "普通",
                audioStimulusType = isAudioSimilar ? "負面" : "普通"
            };

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

            foreach (var plane in gridPlanes)
                plane.GetComponent<Renderer>().material.SetTexture("_BaseMap", null);
        }

        int actualVisualStimuli = trialResults.Count(r => r.isVisualStimulus);
        int actualAudioStimuli = trialResults.Count(r => r.isAudioStimulus);

        // 正確率計算（只看 Hit 數）
        float visualAccuracy = actualVisualStimuli > 0 ? (float)visualHit / actualVisualStimuli : 0f;
        float audioAccuracy = actualAudioStimuli > 0 ? (float)audioHit / actualAudioStimuli : 0f;

        Debug.Log("======= ✅ 遊戲結束！統計結果如下： =======");

        Debug.Log(
            $"📷 視覺 ➜ Hit: {visualHit}, Total Stimuli: {actualVisualStimuli}, Accuracy: {(visualAccuracy * 100f):F2}%");
        Debug.Log(
            $"🎧 聽覺 ➜ Hit: {audioHit}, Total Stimuli: {actualAudioStimuli}, Accuracy: {(audioAccuracy * 100f):F2}%");

        int visualStimuliCount = trialResults.Count(r => r.visualStimulusType == "負面");
        int visualNormalCount = trialResults.Count(r => r.visualStimulusType == "普通");
        int audioStimuliCount = trialResults.Count(r => r.audioStimulusType == "負面");
        int audioNormalCount = trialResults.Count(r => r.audioStimulusType == "普通");

        Debug.Log("📊 題目類型統計：");
        Debug.Log($"視覺 ➜ 負面: {visualStimuliCount}, 普通: {visualNormalCount}");
        Debug.Log($"聽覺 ➜ 負面: {audioStimuliCount}, 普通: {audioNormalCount}");
    }

    public void SetVisualCheck(bool check)
    {
        isVisualCheck = check;
    }

    public void SetAudioCheck(bool check)
    {
        isAudioCheck = check;
    }

    public void ExportTrialResultsToCSV()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            string path =
 "/storage/emulated/0/Download/NBackResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
        string path = Application.dataPath + "/NBackResults_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "-" + PlayerPrefs.GetString("ID") + ".csv";
#endif

        StringBuilder csv = new StringBuilder();
        csv.AppendLine(
            "trialIndex,isVisualStimulus,isAudioStimulus,visualCorrect,audioCorrect,visualReactionTime,audioReactionTime,visualResultType,audioResultType,visualStimulusType,audioStimulusType");
        foreach (var result in trialResults)
        {
            csv.AppendLine($"{result.trialIndex}," +
                           $"{result.isVisualStimulus}," +
                           $"{result.isAudioStimulus}," +
                           $"{result.visualCorrect}," +
                           $"{result.audioCorrect}," +
                           $"{result.visualReactionTime}," +
                           $"{result.audioReactionTime}," +
                           $"{result.visualResultType}," +
                           $"{result.audioResultType}," +
                           $"{result.visualStimulusType}," +
                           $"{result.audioStimulusType}");
        }

        try
        {
            File.WriteAllText(path, csv.ToString());
            Debug.Log("✅ CSV 已儲存至: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("❌ 無法寫入CSV: " + e.Message);
        }
    }
}