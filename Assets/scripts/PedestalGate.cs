using System.Collections;
using UnityEngine;

/// <summary>
/// Put this on the gate (wall_with_small_door). When every PedestalSlot is filled,
/// the gate opens, disabled colliders, and shows a message.
/// </summary>
public class PedestalGate : MonoBehaviour
{
    public static PedestalGate Instance { get; private set; }

    public enum OpenMode { Slide, Rotate, Disable }

    [Header("Slots")]
    [Tooltip("Leave empty to auto-find all PedestalSlot in the scene.")]
    public PedestalSlot[] slots;

    [Header("Gate")]
    [Tooltip("The gate object that moves. Defaults to this object.")]
    public Transform gate;

    public OpenMode openMode = OpenMode.Slide;

    [Header("Slide")]
    public Vector3 slideOffset = new Vector3(0f, -3.5f, 0f);
    public bool slideInLocalSpace = true;

    [Header("Rotate")]
    public Vector3 rotateAxis = Vector3.up;
    public float rotateAngle = 90f;

    [Header("Timing")]
    public float openDuration = 2f;

    [Header("Message")]
    public string openMessage = "All collectibles placed. The door opens!";

    public bool IsOpen { get; private set; }

    void Awake()
    {
        Instance = this;
        if (gate == null) gate = transform;
    }

    void Update()
    {
        if (IsOpen) return;
        if (slots == null || slots.Length == 0)
        {
            PedestalSlot[] found = FindObjectsOfType<PedestalSlot>();
            if (found != null && found.Length > 0)
                slots = found;
            else
                return;
        }

        if (AllFilled())
            Open();
    }

    private bool AllFilled()
    {
        if (slots == null || slots.Length == 0) return false;
        foreach (PedestalSlot s in slots)
            if (s == null || !s.IsFilled) return false;
        return true;
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (InteractionUI.Instance != null)
            InteractionUI.Instance.ShowPrompt(openMessage);

        foreach (Collider c in gate.GetComponentsInChildren<Collider>(true))
            c.enabled = false;

        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        if (openMode == OpenMode.Disable)
        {
            yield return new WaitForSeconds(0.3f);
            gate.gameObject.SetActive(false);
            yield break;
        }

        if (openMode == OpenMode.Slide)
        {
            Vector3 from = slideInLocalSpace ? gate.localPosition : gate.position;
            Vector3 to = from + slideOffset;

            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / openDuration);
                if (slideInLocalSpace)
                    gate.localPosition = Vector3.Lerp(from, to, k);
                else
                    gate.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            if (slideInLocalSpace) gate.localPosition = to;
            else gate.position = to;
        }
        else if (openMode == OpenMode.Rotate)
        {
            Quaternion from = gate.localRotation;
            Quaternion to = from * Quaternion.AngleAxis(rotateAngle, rotateAxis);
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                gate.localRotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t / openDuration));
                yield return null;
            }
            gate.localRotation = to;
        }
    }
}
