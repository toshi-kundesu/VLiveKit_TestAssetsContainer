using UnityEngine;

public class EnableUpdateWhenOffscreenForAllSkinnedMeshes : MonoBehaviour
{
    [SerializeField]
    private bool includeInactive = true;

    private void Awake()
    {
        EnableAll();
    }

    [ContextMenu("Enable All SkinnedMeshRenderer Update When Offscreen")]
    public void EnableAll()
    {
        SkinnedMeshRenderer[] renderers = FindObjectsByType<SkinnedMeshRenderer>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (var smr in renderers)
        {
            smr.updateWhenOffscreen = true;
        }

        Debug.Log($"Update When Offscreen enabled: {renderers.Length} SkinnedMeshRenderer(s)");
    }
}