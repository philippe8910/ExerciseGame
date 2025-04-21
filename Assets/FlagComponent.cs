using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlagComponent : MonoBehaviour
{
    [Header("References")]
        public Transform headTransform;     // 頭部 Transform
        public Transform handTransform;     // 手部控制器 Transform
    
        [Header("Events")]
        public UnityEvent OnHandAboveHead;  // 高於頭部觸發的事件
        public UnityEvent OnHandBelowHead;  // 低於頭部觸發的事件
    
        public bool isAbove = false;       // 當前是否高於頭部
    
        void Update()
        {
            if (headTransform == null || handTransform == null)
                return;
    
            float headY = headTransform.position.y;
            float handY = handTransform.position.y;
    
            if (!isAbove && handY > headY)
            {
                isAbove = true;
                OnHandAboveHead?.Invoke();
            }
            else if (isAbove && handY <= headY)
            {
                isAbove = false;
                OnHandBelowHead?.Invoke();
            }
        }
}
