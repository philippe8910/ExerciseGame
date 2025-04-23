using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    [Header("Flanker 設定")]
    [SerializeField]
    public EmotionalFlankerTaskDataHolder currentTaskData;

    public List<FlankerTaskData> currentData = new List<FlankerTaskData>();

    public TMP_Text middleLetter;
    public TMP_Text upperLetter, bottomLetter;

    public Color redColor = Color.red, greenColor = Color.green;
    public float timeBetweenTrials = 1.0f;
    public float[] responseTimeLimit;

    [Header("旗子參考")]
    public FlagComponent leftFlag;   // 對應紅色回答
    public FlagComponent rightFlag;  // 對應綠色回答

    private void Start()
    {
        Init();
        StartCoroutine(StartTask());
    }

    private IEnumerator StartTask()
    {
        foreach (var data in currentData)
        {
            middleLetter.text = "+";
            upperLetter.text = "";
            bottomLetter.text = "";

            yield return new WaitForSeconds(timeBetweenTrials);

            middleLetter.color = data.midColor;
            upperLetter.color = data.OtherColor;
            bottomLetter.color = data.OtherColor;

            middleLetter.text = data.currentLetter;
            upperLetter.text = data.currentLetter;
            bottomLetter.text = data.currentLetter;

            yield return new WaitForSeconds(0.5f);

            middleLetter.text = "";
            upperLetter.text = "";
            bottomLetter.text = "";

            float responseTime = responseTimeLimit[Random.Range(0, responseTimeLimit.Length)];
            float startTime = Time.time;

            bool responded = false;

            while (Time.time - startTime < responseTime)
            {
                bool leftUp = leftFlag != null && leftFlag.isAbove;
                bool rightUp = rightFlag != null && rightFlag.isAbove;

                if (leftUp && rightUp)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = false; // 雙手舉起 = 錯誤
                    responded = true;
                    break;
                }

                if (rightUp && !leftUp && middleLetter.color == Color.green)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }

                if (leftUp && !rightUp && middleLetter.color == Color.red)
                {
                    data.responseTime = Time.time - startTime;
                    data.isCorrect = true;
                    responded = true;
                    break;
                }

                yield return null;
            }

            if (!responded)
            {
                data.isCorrect = false;
                data.responseTime = responseTime;
            }

            middleLetter.color = Color.white;
        }

        int totalCount = currentData.Count;
        int correctCount = currentData.Count(d => d.isCorrect);
        float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
        float averageResponseTime = currentData.Where(d => d.isCorrect).Any()
            ? currentData.Where(d => d.isCorrect).Average(d => d.responseTime)
            : 0f;

        Debug.Log($"✅ 正確率: {correctCount}/{totalCount} ({accuracy:F2}%)");
        Debug.Log($"⏱️ 平均反應時間（正確題）: {averageResponseTime:F2} 秒");
        
        ExportFlankerResultsToCSV();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    public void Init()
    {
        currentData.Clear();

        var negativeLatter = currentTaskData.negativeLatter.Take(30).ToList();
        var neutralLatter = currentTaskData.neutralLatter.Take(30).ToList();

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
    }
    
    public void ExportFlankerResultsToCSV()
{
#if UNITY_ANDROID && !UNITY_EDITOR
    string path = "/storage/emulated/0/Download/FlankerResults_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#else
    string path = Application.dataPath + "/FlankerResults_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
#endif

    StringBuilder csv = new StringBuilder();
    csv.AppendLine("Index,Letter,MidColor,OtherColor,IsNegative,IsCorrect,ResponseTime");

    int correctCount = 0;
    float totalResponseTime = 0f;
    int totalCount = currentData.Count;

    for (int i = 0; i < currentData.Count; i++)
    {
        var data = currentData[i];

        string midColorStr = ColorToString(data.midColor);
        string otherColorStr = ColorToString(data.OtherColor);

        if (data.isCorrect)
        {
            correctCount++;
            totalResponseTime += data.responseTime;
        }

        csv.AppendLine($"{i}," +
                       $"{data.currentLetter}," +
                       $"{midColorStr}," +
                       $"{otherColorStr}," +
                       $"{data.isNegative}," +
                       $"{data.isCorrect}," +
                       $"{data.responseTime:F3}");
    }

    float accuracy = totalCount > 0 ? (float)correctCount / totalCount * 100f : 0f;
    float averageRT = correctCount > 0 ? totalResponseTime / correctCount : 0f;

    csv.AppendLine(); // 空行
    csv.AppendLine($"總題數,{totalCount}");
    csv.AppendLine($"正確題數,{correctCount}");
    csv.AppendLine($"正確率,{accuracy:F2}%");
    csv.AppendLine($"平均反應時間（僅計算正確題）, {averageRT:F3} 秒");

    try
    {
        File.WriteAllText(path, csv.ToString());
        Debug.Log("✅ Flanker CSV 已儲存至: " + path);
    }
    catch (System.Exception e)
    {
        Debug.LogError("❌ 無法寫入Flanker CSV: " + e.Message);
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
    public float responseTime;
}
