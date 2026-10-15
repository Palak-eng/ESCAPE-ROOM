public interface IInteractable
{
    string GetInteractionPrompt();
    void OnInteract();
}

/// <summary>
/// Optional: an interactable whose collider stays active while its visuals are
/// hidden (closed box/diariy shells used as close handles). Such hits defer to
/// visible interactables behind them, but remain as fallback targeting.
/// </summary>
public interface IDeferTargeting
{
    bool DeferTargeting { get; }
}
