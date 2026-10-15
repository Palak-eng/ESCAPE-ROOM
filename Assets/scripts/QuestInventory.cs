using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the 4 story collectibles (alarm clock, book, photograph, key) that the player
/// picks up and later places on the pedestals. Auto-created at runtime.
/// </summary>
public class QuestInventory : MonoBehaviour
{
    public static QuestInventory Instance { get; private set; }

    public class Entry
    {
        public string id;
        public string displayName;
        public GameObject worldObject;
        public bool placed;
        public bool isQuestItem = true;

        // Original spot, so a rejected item can be sent back where it came from.
        public Transform originParent;
        public Vector3 originLocalPos;
        public Quaternion originLocalRot;
        public Vector3 originLocalScale;
        public bool hasOrigin;
    }

    [Header("Settings")]
    public int totalItems = 4;

    [Tooltip("Show a small top-left progress HUD (Ritual: X/4).")]
    public bool showHud = true;

    public List<Entry> items = new List<Entry>();

    private string heldId;
    private GUIStyle hudStyle;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool Has(string id) => items.Exists(i => i.id == id);

    public void Add(string id, string displayName, GameObject worldObject, bool isQuestItem = true)
    {
        if (Has(id)) return;

        var entry = new Entry { id = id, displayName = displayName, worldObject = worldObject, placed = false, isQuestItem = isQuestItem };

        // Remember where it was picked up so it can be returned there.
        if (worldObject != null)
        {
            entry.originParent = worldObject.transform.parent;
            entry.originLocalPos = worldObject.transform.localPosition;
            entry.originLocalRot = worldObject.transform.localRotation;
            entry.originLocalScale = worldObject.transform.localScale;
            entry.hasOrigin = true;
        }

        items.Add(entry);
    }

    /// <summary>Put an item into the player's hand (shown in HUD, offered to pedestals). Only one at a time.</summary>
    public void Hold(string id) { heldId = id; }

    public void ClearHeld() { heldId = null; }

    /// <summary>Currently held, unplaced item. Null when hands are empty.</summary>
    public Entry HeldEntry() => items.Find(i => i.id == heldId && !i.placed);

    public bool HandsFull() => HeldEntry() != null;

    /// <summary>
    /// Send the held item back to its original position (e.g. wrong pedestal item).
    /// Frees the hand so the player can pick up something else.
    /// </summary>
    public void ReturnHeldToOrigin()
    {
        Entry held = HeldEntry();
        if (held == null) { heldId = null; return; }

        GameObject obj = held.worldObject;
        if (obj != null)
        {
            if (held.hasOrigin)
            {
                obj.transform.SetParent(held.originParent, false);
                obj.transform.localPosition = held.originLocalPos;
                obj.transform.localRotation = held.originLocalRot;
                obj.transform.localScale = held.originLocalScale;
            }

            foreach (Collider c in obj.GetComponentsInChildren<Collider>(true))
                c.enabled = true;

            obj.SetActive(true);

            var pickup = obj.GetComponent<CollectibleItem>();
            if (pickup != null)
                pickup.ResetForReturn();
        }

        items.Remove(held);
        heldId = null;
    }

    public int CollectedCount() => items.Count;

    /// <summary>Clears the whole run (used by Play Again). Scene objects come back fresh on reload.</summary>
    public void ResetRun()
    {
        items.Clear();
        heldId = null;
    }

    public int PlacedCount()
    {
        int c = 0;
        foreach (var i in items) if (i.placed) c++;
        return c;
    }

    public Entry GetNextUnplaced() => items.Find(i => !i.placed);

    void OnGUI()
    {
        if (!showHud) return;

        // Don't overlap fullscreen diary reader or journal
        if (DiaryUIManager.Instance != null && DiaryUIManager.Instance.IsOpen) return;
        if (CollectiblesManager.Instance != null && CollectiblesManager.Instance.JournalOpen) return;

        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label);
            hudStyle.fontSize = 24;
            hudStyle.fontStyle = FontStyle.Bold;
            hudStyle.normal.textColor = new Color(1f, 0.92f, 0.6f);
        }

        Entry held = HeldEntry();
        string holding = held != null ? held.displayName : "empty hands";
        string text = "Holding: " + holding + "\nRitual: " + PlacedCount() + " / " + totalItems + " placed";
        Rect rect = new Rect(24, 24, 500, 74);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.Box(new Rect(rect.x - 10, rect.y - 6, 340, 80), GUIContent.none);
        GUI.color = old;
        GUI.Label(rect, text, hudStyle);
    }
}
