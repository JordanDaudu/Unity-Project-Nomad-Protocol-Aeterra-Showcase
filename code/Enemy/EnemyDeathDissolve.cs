using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the enemy "death dissolve" visual sequence.
///
/// Flow:
/// 1) (Optional) Swap targeted renderers to dissolve-capable materials.
///    - Controlled by <see cref="MaterialSwapMode"/>.
///    - If your enemy already uses dissolve shader materials by default, you can skip swapping.
/// 2) Animate a shader float property (e.g., "_Dissolve") from visibleValue -> dissolvedValue over dissolveDuration
///    using <see cref="MaterialPropertyBlock"/> per renderer/material-slot.
/// 3) On completion, either destroy, disable, or return the enemy to the pool.
///
/// Pooling notes:
/// - <see cref="ResetForReuse"/> restores original materials (if swapped) and clears property blocks.
/// - <see cref="OnEnable"/> calls <see cref="ResetForReuse"/> so pooled enemies come back visible.
/// - ReturnToPool uses <see cref="PooledObject"/> on this object OR any parent (robust for child setups).
/// </summary>
public class EnemyDeathDissolve : MonoBehaviour
{
    private enum CompletionMode
    {
        Destroy,
        DisableGameObject,
        ReturnToPool
    }

    /// <summary>
    /// Controls whether we swap materials when the enemy dies.
    ///
    /// - Auto: swap only if at least one valid replacement is configured.
    /// - AlwaysSwap: always apply the replacement arrays (useful if you always want dissolve versions on death).
    /// - NeverSwap: never swap materials (useful when the enemy already uses dissolve shader materials by default).
    /// </summary>
    private enum MaterialSwapMode
    {
        Auto,
        AlwaysSwap,
        NeverSwap
    }

    [System.Serializable]
    private struct MaterialReplacement
    {
        [Tooltip("Original (alive) material")]
        public Material originalMaterial;

        [Tooltip("Dissolve version of the original material")]
        public Material dissolveMaterial;
    }

    /// <summary>
    /// A single (Renderer, material index) slot that supports the dissolve property.
    /// We animate these slots with MaterialPropertyBlock so each instance can dissolve independently.
    /// </summary>
    private struct RendererMaterialSlot
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererMaterialSlot(Renderer renderer, int materialIndex)
        {
            this.renderer = renderer;
            this.materialIndex = materialIndex;
        }
    }

    [Header("Timing")]
    [SerializeField] private float delayBeforeDissolve = 2f;
    [SerializeField] private float dissolveDuration = 1.25f;

    [Header("Completion")]
    [SerializeField] private CompletionMode completionMode = CompletionMode.DisableGameObject;

    [Header("Materials")]
    [SerializeField] private MaterialSwapMode materialSwapMode = MaterialSwapMode.Auto;

    [Header("Material Mapping")]
    [SerializeField] private bool allowNameFallback = false;

    [Header("Dissolve Shader")]
    [Tooltip("Shader Graph property Reference name (not display name). Example: _Dissolve")]
    [SerializeField] private string dissolvePropertyName = "_Dissolve";

    [Header("Targets")]
    [Tooltip("Assign only the renderers that should dissolve (exclude weapons/glow if desired).")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Material Replacements")]
    [Tooltip("Map original materials to dissolve materials. If a renderer uses a material not listed here, it stays unchanged.")]
    [SerializeField] private MaterialReplacement[] materialReplacements;

    [Header("Value Range")]
    [Tooltip("Most dissolve shaders use 0=visible, 1=fully dissolved. Swap these if your shader is reversed.")]
    [SerializeField] private float visibleValue = 0f;
    [SerializeField] private float dissolvedValue = 1f;

    [Header("Debug")]
    [SerializeField] private bool logSetup = false;

    private int dissolvePropertyId;
    private bool initialized;
    private bool hasStarted;
    private Coroutine dissolveRoutine;

    // Cached original / dissolve arrays per renderer.
    // These arrays hold the material assignments we apply via sharedMaterials.
    private Material[][] originalSharedMaterialsPerRenderer;
    private Material[][] dissolveSharedMaterialsPerRenderer;

    // Slots that actually support the dissolve property.
    private readonly List<RendererMaterialSlot> dissolveSlots = new List<RendererMaterialSlot>();

    // Replacement lookups:
    // - byMaterial: best/fastest when the exact material reference matches
    // - byName: fallback for cases where Unity produces instances or references differ but names match
    private readonly Dictionary<Material, Material> replacementByMaterial = new Dictionary<Material, Material>();
    private readonly Dictionary<string, Material> replacementByName = new Dictionary<string, Material>();

    // Reused MPB to avoid per-frame allocations.
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        // Pool-safe reset when object becomes active again.
        // This ensures: visible dissolve value, original materials (if swapped), and no stale property blocks.
        ResetForReuse();
    }

    private void OnDisable()
    {
        // Safety: if we were dissolving and got disabled early, stop the routine.
        if (dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }
    }

    /// <summary>
    /// Starts the delayed dissolve sequence once.
    /// - Optionally swaps materials based on MaterialSwapMode.
    /// - Animates dissolve property from visibleValue -> dissolvedValue.
    /// - Completes by Destroy/Disable/ReturnToPool.
    /// </summary>
    public void PlayDeathDissolve()
    {
        InitializeIfNeeded();

        if (hasStarted)
            return;

        hasStarted = true;

        // Only swap if requested/needed.
        if (ShouldSwapMaterials())
            ApplyDissolveMaterials();

        // Rebuild after potential swap so we animate the correct current materials.
        RebuildDissolveSlots();

        // Ensure we start from visible.
        SetDissolveValue(visibleValue);

        dissolveRoutine = StartCoroutine(DissolveRoutine());
    }

    /// <summary>
    /// Resets visuals and material setup for reuse (pooling).
    /// This should be called when an enemy is reactivated from the pool.
    /// </summary>
    public void ResetForReuse()
    {
        InitializeIfNeeded();

        hasStarted = false;

        if (dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }

        // Restore original alive materials only if we are using swapping.
        if (ShouldSwapMaterials())
            ApplyOriginalMaterials();

        // Clear per-instance property blocks from all target renderers/material slots.
        // Note: this clears any MPB overrides on those renderer slots (including dissolve).
        ClearAllPropertyBlocks();

        // Rebuild slots from current materials and force visible.
        RebuildDissolveSlots();
        SetDissolveValue(visibleValue);
    }

    [ContextMenu("Debug Play Dissolve")]
    private void DebugPlayDissolve() => PlayDeathDissolve();

    [ContextMenu("Debug Reset Dissolve")]
    private void DebugResetDissolve() => ResetForReuse();

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        dissolvePropertyId = Shader.PropertyToID(dissolvePropertyName);
        propertyBlock = new MaterialPropertyBlock();

        // If not assigned, auto-find renderers. For production, you often want this set manually
        // to avoid accidentally dissolving weapons/glow/extra meshes.
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        BuildReplacementLookup();
        CacheRendererMaterialArrays();

        // Build slots once for debug validation (materials may later change on swap, we rebuild again when needed).
        RebuildDissolveSlots();

        if (dissolveSlots.Count == 0)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyDeathDissolve)}] No dissolve-capable material slots found on '{name}'. " +
                $"Check target renderers and dissolve property '{dissolvePropertyName}'.",
                this);
        }

        initialized = true;
    }

    /// <summary>
    /// Returns true if we should swap materials for this enemy based on the configured mode.
    /// </summary>
    private bool ShouldSwapMaterials()
    {
        switch (materialSwapMode)
        {
            case MaterialSwapMode.AlwaysSwap:
                return true;

            case MaterialSwapMode.NeverSwap:
                return false;

            case MaterialSwapMode.Auto:
            default:
                return HasAnyValidReplacements();
        }
    }

    /// <summary>
    /// Checks if at least one replacement entry is valid.
    /// Used by MaterialSwapMode.Auto.
    /// </summary>
    private bool HasAnyValidReplacements()
    {
        if (materialReplacements == null || materialReplacements.Length == 0)
            return false;

        for (int i = 0; i < materialReplacements.Length; i++)
        {
            if (materialReplacements[i].originalMaterial != null &&
                materialReplacements[i].dissolveMaterial != null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Builds lookup dictionaries for replacement materials.
    /// </summary>
    private void BuildReplacementLookup()
    {
        replacementByMaterial.Clear();
        replacementByName.Clear();

        if (materialReplacements == null)
            return;

        for (int i = 0; i < materialReplacements.Length; i++)
        {
            Material original = materialReplacements[i].originalMaterial;
            Material dissolve = materialReplacements[i].dissolveMaterial;

            if (original == null || dissolve == null)
                continue;

            replacementByMaterial[original] = dissolve;

            // Name fallback helps if Unity changes references or materials are duplicated/instanced.
            string cleanName = CleanMaterialName(original.name);
            if (!replacementByName.ContainsKey(cleanName))
                replacementByName.Add(cleanName, dissolve);
        }
    }

    /// <summary>
    /// Caches original and dissolve material arrays per renderer.
    /// We use sharedMaterials to avoid creating per-instance material duplicates (pool-friendly).
    /// </summary>
    private void CacheRendererMaterialArrays()
    {
        originalSharedMaterialsPerRenderer = new Material[targetRenderers.Length][];
        dissolveSharedMaterialsPerRenderer = new Material[targetRenderers.Length][];

        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer rend = targetRenderers[r];
            if (rend == null)
                continue;

            Material[] originalArray = rend.sharedMaterials;
            originalSharedMaterialsPerRenderer[r] = originalArray;

            Material[] dissolveArray = new Material[originalArray.Length];

            for (int m = 0; m < originalArray.Length; m++)
            {
                Material originalMat = originalArray[m];
                Material resolvedMat = originalMat;

                // If swapping is used, attempt to map original -> dissolve.
                // If no mapping exists, keep the original (e.g., emissive glow material you don't want to swap yet).
                if (originalMat != null)
                {
                    if (!replacementByMaterial.TryGetValue(originalMat, out resolvedMat))
                    {
                        if (allowNameFallback)
                            replacementByName.TryGetValue(CleanMaterialName(originalMat.name), out resolvedMat);

                        if (resolvedMat == null)
                            resolvedMat = originalMat;
                    }
                }

                dissolveArray[m] = resolvedMat;
            }

            dissolveSharedMaterialsPerRenderer[r] = dissolveArray;
        }
    }

    /// <summary>
    /// Restores the original sharedMaterials arrays for all target renderers.
    /// </summary>
    private void ApplyOriginalMaterials()
    {
        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer rend = targetRenderers[r];
            if (rend == null || originalSharedMaterialsPerRenderer[r] == null)
                continue;

            rend.sharedMaterials = originalSharedMaterialsPerRenderer[r];
        }
    }

    /// <summary>
    /// Applies the dissolve sharedMaterials arrays for all target renderers.
    /// </summary>
    private void ApplyDissolveMaterials()
    {
        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer rend = targetRenderers[r];
            if (rend == null || dissolveSharedMaterialsPerRenderer[r] == null)
                continue;

            rend.sharedMaterials = dissolveSharedMaterialsPerRenderer[r];
        }
    }

    /// <summary>
    /// Rebuilds the list of (renderer, materialIndex) slots that support the dissolve property.
    /// Must be called after any material assignment changes (e.g., swapping).
    /// </summary>
    private void RebuildDissolveSlots()
    {
        dissolveSlots.Clear();

        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer rend = targetRenderers[r];
            if (rend == null)
                continue;

            Material[] mats = rend.sharedMaterials;

            for (int m = 0; m < mats.Length; m++)
            {
                Material mat = mats[m];
                bool hasDissolve = mat != null && mat.HasProperty(dissolvePropertyId);

                if (hasDissolve)
                    dissolveSlots.Add(new RendererMaterialSlot(rend, m));

                if (logSetup)
                {
                    string matName = mat != null ? mat.name : "NULL";
                    Debug.Log(
                        $"Dissolve target -> Renderer: {rend.name}, Material[{m}]: {matName}, Has {dissolvePropertyName}: {hasDissolve}",
                        rend);
                }
            }
        }
    }

    /// <summary>
    /// Sets the dissolve property on all dissolve-capable slots using MaterialPropertyBlock.
    /// We read the current block first to avoid overwriting other MPB-driven properties.
    /// </summary>
    private void SetDissolveValue(float value)
    {
        for (int i = 0; i < dissolveSlots.Count; i++)
        {
            RendererMaterialSlot slot = dissolveSlots[i];
            if (slot.renderer == null)
                continue;

            slot.renderer.GetPropertyBlock(propertyBlock, slot.materialIndex);
            propertyBlock.SetFloat(dissolvePropertyId, value);
            slot.renderer.SetPropertyBlock(propertyBlock, slot.materialIndex);
        }
    }

    /// <summary>
    /// Clears all property blocks on the target renderers/material slots.
    /// Note: This clears any other MPB overrides on those slots too.
    /// </summary>
    private void ClearAllPropertyBlocks()
    {
        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer rend = targetRenderers[r];
            if (rend == null)
                continue;

            Material[] mats = rend.sharedMaterials;
            for (int m = 0; m < mats.Length; m++)
            {
                rend.SetPropertyBlock(null, m);
            }
        }
    }

    /// <summary>
    /// Coroutine that performs the delayed dissolve animation.
    /// </summary>
    private IEnumerator DissolveRoutine()
    {
        if (delayBeforeDissolve > 0f)
            yield return new WaitForSeconds(delayBeforeDissolve);

        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;

            float t = dissolveDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / dissolveDuration);

            float dissolveValue = Mathf.Lerp(visibleValue, dissolvedValue, t);
            SetDissolveValue(dissolveValue);

            yield return null;
        }

        SetDissolveValue(dissolvedValue);
        dissolveRoutine = null;

        CompleteSequence();
    }

    /// <summary>
    /// Final action after dissolve completes.
    /// </summary>
    private void CompleteSequence()
    {
        switch (completionMode)
        {
            case CompletionMode.Destroy:
                Destroy(gameObject);
                break;

            case CompletionMode.ReturnToPool:
                {
                    // Robust: find PooledObject on this GameObject or any parent.
                    PooledObject pooled = GetComponentInParent<PooledObject>();

                    if (ObjectPool.Instance != null && pooled != null)
                        ObjectPool.Instance.ReturnObjectToPool(pooled.gameObject, 0f);
                    else
                    {
                        Debug.LogWarning(
                            $"[{nameof(EnemyDeathDissolve)}] CompletionMode is set to ReturnToPool, but no ObjectPool found or PooledObject component missing on '{name}'. " +
                            $"Falling back to disabling the GameObject.", this);
                        gameObject.SetActive(false);
                    }
                    break;
                }

            case CompletionMode.DisableGameObject:
            default:
                gameObject.SetActive(false);
                break;
        }
    }

    /// <summary>
    /// Removes Unity's runtime suffix " (Instance)" so name-based lookups remain stable.
    /// </summary>
    private static string CleanMaterialName(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return string.Empty;

        return materialName.Replace(" (Instance)", string.Empty);
    }
}