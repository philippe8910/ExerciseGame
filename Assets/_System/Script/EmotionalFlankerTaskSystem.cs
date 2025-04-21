using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
