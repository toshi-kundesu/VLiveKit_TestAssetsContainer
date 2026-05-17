using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("toshi/VLiveKit/Test Assets/Scene Description")]
public sealed class VLiveKitSceneDescription : MonoBehaviour
{
    [SerializeField] string title = "";
    [SerializeField, TextArea(4, 14)] string description = "";
    [SerializeField] bool showOnSceneOpen = true;
    [SerializeField] bool showOnlyOncePerEditorSession;

    public string Title => title;
    public string Description => description;
    public bool ShowOnSceneOpen => showOnSceneOpen;
    public bool ShowOnlyOncePerEditorSession => showOnlyOncePerEditorSession;

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(description);

    public string GetDisplayTitle(string fallback)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        return string.IsNullOrWhiteSpace(fallback) ? "Scene Description" : fallback;
    }
}
