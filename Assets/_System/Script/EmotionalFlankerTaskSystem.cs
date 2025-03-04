using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EmotionalFlankerTaskSystem : MonoBehaviour
{
    [SerializeField] 
    public EmotionalFlankerTaskDataHolder currentTaskData;  
    
    public TMP_Text middleLetterText;
    public TMP_Text flankerLetterText;
    
    public float timeBetweenTrials = 1.0f;
}

[System.Serializable]
[CreateAssetMenu(fileName = "EmotionalFlankerTaskDataHolder", menuName = "EmotionalFlankerTaskDataHolder", order = 1)]
public class EmotionalFlankerTaskDataHolder : ScriptableObject
{
    [SerializeField] 
    public List<EmotionalFlankerTaskData> emotionalFlankerTaskDataList;
}

[System.Serializable]
public class EmotionalFlankerTaskData
{
    public string middleLetter;

    public Color middleColor;
    public Color flankerColor;
}
