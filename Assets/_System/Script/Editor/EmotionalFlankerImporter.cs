// 路徑：Assets/Editor/EmotionalFlankerImporter.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class EmotionalFlankerImporter : EditorWindow
{
    private string csvFilePath = "";

    [MenuItem("Tools/Import Emotional Words from CSV")]
    public static void ShowWindow()
    {
        GetWindow<EmotionalFlankerImporter>("CSV → ScriptableObject");
    }

    private void OnGUI()
    {
        GUILayout.Label("匯入情緒詞彙 CSV 檔案", EditorStyles.boldLabel);

        if (GUILayout.Button("選擇 CSV 檔案"))
        {
            csvFilePath = EditorUtility.OpenFilePanel("選擇 CSV", "", "csv");
        }

        GUILayout.Label("檔案路徑：");
        GUILayout.TextField(csvFilePath);

        if (!string.IsNullOrEmpty(csvFilePath))
        {
            if (GUILayout.Button("產生 ScriptableObject"))
            {
                CreateScriptableObjectFromCSV(csvFilePath);
            }
        }
    }

    void CreateScriptableObjectFromCSV(string path)
    {
        var lines = File.ReadAllLines(path);
        List<string> neutralList = new List<string>();
        List<string> negativeList = new List<string>();

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // 忽略行尾多的逗號、雙引號包住的內容等情況
            var cols = ParseCsvLine(line);
            if (cols.Count >= 2)
            {
                if (!string.IsNullOrWhiteSpace(cols[0])) negativeList.Add(cols[0].Trim().Trim('"'));
                if (!string.IsNullOrWhiteSpace(cols[1])) neutralList.Add(cols[1].Trim().Trim('"'));
            }
        }

        var asset = ScriptableObject.CreateInstance<EmotionalFlankerTaskDataHolder>();
        asset.neutralLatter = neutralList;
        asset.negativeLatter = negativeList;

        string folder = "Assets/Resources";
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        string assetPath = Path.Combine(folder, "EmotionalFlankerTaskDataHolder.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("成功", $"ScriptableObject 已儲存至：\n{assetPath}", "OK");
        Debug.Log($"✅ 匯入完成，負向：{negativeList.Count} 筆，中性：{neutralList.Count} 筆");
    }

// 簡易 CSV 解析器：可處理引號內含逗號的情況
    List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string current = "";

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current);
        return result;
    }

}
