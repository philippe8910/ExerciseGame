using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class VRGUI_Button : MonoBehaviour
{
    public Animator animator;
    public UnityEvent onButtonTrigger;
    public UnityEvent onButtonRelease;

    public int num;

    private bool isPlayerInside = false;
    private Transform currentPlayer;
    private InputDevice device;

    private bool wasGrabbing = false;

    private void Start()
    {
        device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand); // 可改為 LeftHand
        
        onButtonTrigger.AddListener(delegate
        {
            FindObjectOfType<EmotionalStroopCore>().SetTriggerNumber(num);
        });
        
        onButtonRelease.AddListener(delegate
        {
            FindObjectOfType<EmotionalStroopCore>().SetTriggerNumber(-1);
        });
    }

    private void Update()
    {
        if (!isPlayerInside || currentPlayer == null) return;
        if (!VRButtonProximityManager.Instance.IsClosest(currentPlayer, this)) return;

        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return;
        }

        bool isGrabbing = false;
        device.TryGetFeatureValue(CommonUsages.gripButton, out isGrabbing);

        if (isGrabbing && !wasGrabbing)
        {
            onButtonTrigger?.Invoke();
            SendHaptic(0.7f, 0.2f); // 👉 按下震動：強
        }
        else if (!isGrabbing && wasGrabbing)
        {
            onButtonRelease?.Invoke();
        }

        wasGrabbing = isGrabbing;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            currentPlayer = other.transform;

            float distance = Vector3.Distance(other.transform.position, transform.position);
            VRButtonProximityManager.Instance.ReportProximity(other.transform, this, distance);

            SendHaptic(0.3f, 0.1f); // 👉 碰到震動：輕
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float distance = Vector3.Distance(other.transform.position, transform.position);
            VRButtonProximityManager.Instance.ReportProximity(other.transform, this, distance);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.transform == currentPlayer)
        {
            if (wasGrabbing) onButtonRelease?.Invoke();
            VRButtonProximityManager.Instance.Unregister(other.transform, this);
            isPlayerInside = false;
            currentPlayer = null;
            wasGrabbing = false;
        }
    }

    private void SendHaptic(float amplitude, float duration)
    {
        if (device.isValid && device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}
