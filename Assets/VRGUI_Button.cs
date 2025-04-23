using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class VRGUI_Button : MonoBehaviour
{
    public enum FilterMode
    {
        None,        // 不過濾 (所有碰撞都觸發)
        Tag,         // 只過濾 Tag
        Layer,       // 只過濾 Layer
        TagAndLayer  // 同時過濾 Tag 和 Layer
    }

    public float num;

    [BoxGroup("碰撞過濾設定")]
    [LabelText("碰撞過濾模式")]
    public FilterMode filterMode = FilterMode.None;

    [BoxGroup("碰撞過濾設定")]
    [ShowIf("@filterMode == FilterMode.Tag || filterMode == FilterMode.TagAndLayer")]
    [ValueDropdown("@UnityEditorInternal.InternalEditorUtility.tags")]
    [LabelText("目標 Tag")]
    public string targetTag = "";

    [BoxGroup("碰撞過濾設定")]
    [ShowIf("@filterMode == FilterMode.Layer || filterMode == FilterMode.TagAndLayer")]
    [LabelText("目標 Layer")]
    public LayerMask targetLayer;

    [FoldoutGroup("事件回調")]
    [LabelText("碰撞開始時觸發")]
    public UnityEvent onCollisionEnter;

    [FoldoutGroup("事件回調")]
    [LabelText("碰撞持續時觸發")]
    public UnityEvent onCollisionStay;

    [FoldoutGroup("事件回調")]
    [LabelText("碰撞結束時觸發")]
    public UnityEvent onCollisionExit;

    private bool IsCollisionValid(GameObject obj)
    {
        switch (filterMode)
        {
            case FilterMode.None:
                return true; // 不過濾，所有碰撞都觸發

            case FilterMode.Tag:
                return obj.CompareTag(targetTag);

            case FilterMode.Layer:
                return ((1 << obj.layer) & targetLayer) != 0;

            case FilterMode.TagAndLayer:
                return obj.CompareTag(targetTag) && ((1 << obj.layer) & targetLayer) != 0;

            default:
                return false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsCollisionValid(collision.gameObject))
        {
            onCollisionEnter?.Invoke();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsCollisionValid(collision.gameObject))
        {
            onCollisionStay?.Invoke();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsCollisionValid(collision.gameObject))
        {
            onCollisionExit?.Invoke();
        }
    }
}
