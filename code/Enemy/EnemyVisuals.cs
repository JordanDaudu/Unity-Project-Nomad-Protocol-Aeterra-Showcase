using System.Collections.Generic;
using UnityEngine;

public enum EnemyMeleeWeaponType
{
    OneHand,
    Throw,
    Unarmed
}

public enum EnemyRangeWeaponType
{
    Pistol,
    Revolver,
    Shotgun,
    AutoRifle,
    Rifle
}

public class EnemyVisuals : MonoBehaviour
{
    private Enemy enemy;

    [Header("Colors To Randomize")]
    [SerializeField] private Texture[] colorTextures;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    // Weapon Visuals to randomize will be done inside the function, since we need to filter them based on the enemy type (melee/range) and weapon type.
    [Header("Weapon Visuals To Randomize")]
    [SerializeField] private EnemyWeaponModel[] weaponModels;
    public EnemyWeaponModel currentWeaponModel { get; private set; }

    [Header("Corruption Visuals To Randomize")]
    [SerializeField] private GameObject[] corruptionCrystals;
    [SerializeField] private int corruptionAmount;

    [Header("Shader Property")]
    [Tooltip("URP/Shader Graph BaseMap is usually _BaseMap")]
    [SerializeField] private string baseMapPropertyName = "_BaseMap";

    [Tooltip("Material slot index on the renderer (usually 0)")]
    [SerializeField] private int materialIndex = 0;

    [Header("Behavior")]
    [SerializeField] private bool randomizeOnEnable = true;

    private MaterialPropertyBlock propertyBlock;
    private int baseMapPropertyId;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();  
        if (enemy == null)
        {
            Debug.LogError($"[{nameof(EnemyVisuals)}] No Enemy component found on {name}.", this);
        }

        weaponModels = GetComponentsInChildren<EnemyWeaponModel>(true);

        if (skinnedMeshRenderer == null)
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        propertyBlock = new MaterialPropertyBlock();
        baseMapPropertyId = Shader.PropertyToID(baseMapPropertyName);

        //InvokeRepeating(nameof(SetupLook), 0f, 1.5f); // Temporary: keep randomizing
    }

    private void OnEnable()
    {
        // Great for pooling: every time enemy is reused, it can get a new look.
        if (randomizeOnEnable)
            SetupLook();
    }

    public void SetupLook()
    {
        SetupRandomColor();
        SetupRandomWeapon();
        SetupRandomCorruption();
    }

    public void EnableWeaponTrail(bool enable)
    {
        if (currentWeaponModel == null)
            return;

        currentWeaponModel.EnableTrailEffect(enable);
    }

    private void SetupRandomColor()
    {
        if (skinnedMeshRenderer == null)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] SkinnedMeshRenderer is missing on {name}.", this);
            return;
        }

        if (colorTextures == null || colorTextures.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] No color textures assigned on {name}.", this);
            return;
        }

        Material[] sharedMats = skinnedMeshRenderer.sharedMaterials;
        if (sharedMats == null || sharedMats.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] {skinnedMeshRenderer.name} has no materials.", this);
            return;
        }

        if (materialIndex < 0 || materialIndex >= sharedMats.Length)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyVisuals)}] Material index {materialIndex} is out of range on {skinnedMeshRenderer.name}.",
                this);
            return;
        }

        Material mat = sharedMats[materialIndex];
        if (mat == null)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] Material slot {materialIndex} is null on {skinnedMeshRenderer.name}.", this);
            return;
        }

        if (!mat.HasProperty(baseMapPropertyId))
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] Material '{mat.name}' does not have property '{baseMapPropertyName}'.", this);
            return;
        }

        Texture selectedTexture = colorTextures[Random.Range(0, colorTextures.Length)];

        skinnedMeshRenderer.GetPropertyBlock(propertyBlock, materialIndex);
        propertyBlock.SetTexture(baseMapPropertyId, selectedTexture);
        skinnedMeshRenderer.SetPropertyBlock(propertyBlock, materialIndex);
    }

    private void SetupRandomWeapon()
    {
        currentWeaponModel = null;
        TurnOffWeaponModels();

        // Filter weapon models based on compatibility with the enemy's type and assigned weapon type,
        // then randomly select one of the compatible models. If no compatible models are found, log a warning and skip weapon setup.
        EnemyWeaponModel selectedWeaponModel = FindCompatibleWeaponModel();

        if (selectedWeaponModel == null)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] No compatible weapon model found for {name}. Weapon visuals will be disabled.", this);
            return;
        }

        currentWeaponModel = selectedWeaponModel;
        currentWeaponModel.gameObject.SetActive(true);

        OverrideAnimatorControllerIfCan();
        SwitchAnimationLayer(currentWeaponModel.AnimationLayerIndex);
    }

    private void SetupRandomCorruption()
    {
        corruptionCrystals = CollectCorruptionCrystals();

        if (corruptionCrystals == null || corruptionCrystals.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] No corruption crystals assigned on {name}.", this);
            return;
        }

        // Ensure corruptionAmount is within valid range
        int max = corruptionCrystals.Length;
        int corruptionAmountAfterSafetyCheck = Mathf.Clamp(corruptionAmount, 0, max);

        List<int> availableIndexes = new List<int>();

        for (int i = 0; i < corruptionCrystals.Length; i++)
        {
            availableIndexes.Add(i);
            corruptionCrystals[i].SetActive(false); // Disable all crystals first
        }

        for (int i = 0; i < corruptionAmountAfterSafetyCheck; i++)
        {
            if (availableIndexes.Count == 0)
                break; // No more crystals to activate

            int randomIndex = Random.Range(0, availableIndexes.Count);
            int crystalIndex = availableIndexes[randomIndex];
            availableIndexes.RemoveAt(randomIndex); // Ensure we don't select the same crystal again
            corruptionCrystals[crystalIndex].SetActive(true);
        }
    }

    private EnemyWeaponModel FindCompatibleWeaponModel()
    {
        List<EnemyWeaponModel> filteredWeaponModels = new List<EnemyWeaponModel>();

        foreach (var weaponModel in weaponModels)
        {
            if (weaponModel == null)
                continue;

            if (weaponModel.IsCompatibleWith(enemy))
                filteredWeaponModels.Add(weaponModel);
        }

        if (filteredWeaponModels.Count == 0)
            return null;

        int randomIndex = Random.Range(0, filteredWeaponModels.Count);
        return filteredWeaponModels[randomIndex];
    }

    private void TurnOffWeaponModels()
    {
        if (weaponModels == null || weaponModels.Length == 0)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] No weapon models found on {name}.", this);
            return;
        }

        foreach (var weaponModel in weaponModels)
        {
            if (weaponModel == null)
                continue;

            weaponModel.gameObject.SetActive(false);
        }
    }

    private void OverrideAnimatorControllerIfCan()
    {
        if (currentWeaponModel == null)
        {
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] No current weapon model to override animator controller on {name}.", this);
            return;
        }

        AnimatorOverrideController overrideController = currentWeaponModel.OverrideController;

        if (overrideController != null)
            GetComponentInChildren<Animator>().runtimeAnimatorController = overrideController;
    }

    private GameObject[] CollectCorruptionCrystals()
    {
        EnemyCorruptionCrystal[] crystalComponents = GetComponentsInChildren<EnemyCorruptionCrystal>(true);
        GameObject[] corruptionCrystals = new GameObject[crystalComponents.Length];

        for (int i = 0; i < crystalComponents.Length; i++)
        {
            corruptionCrystals[i] = crystalComponents[i].gameObject;
        }

        return corruptionCrystals;
    }

    private void SwitchAnimationLayer(int layerIndex)
    {
        Animator anim = GetComponentInChildren<Animator>();

        if (currentWeaponModel == null || anim == null)
            return;

        if (layerIndex < 0 || layerIndex >= anim.layerCount)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyVisuals)}] Invalid animation layer index {layerIndex} on {name}. Animator has {anim.layerCount} layers.",
                this);
            return;
        }

        for (int i = 1; i < anim.layerCount; i++)
        {
            anim.SetLayerWeight(i, 0);
        }

        anim.SetLayerWeight(layerIndex, 1);
    }

    /// <summary>
    /// Optional: clears overrides set by this script (and any other MPB overrides on this renderer slot).
    /// Usually not needed if SetupLook() is called on every enable.
    /// </summary>
    public void ClearLookOverride()
    {
        if (skinnedMeshRenderer == null)
            return;

        skinnedMeshRenderer.SetPropertyBlock(null, materialIndex);
    }

    [ContextMenu("Debug Randomize Look")]
    private void DebugRandomizeLook()
    {
        SetupLook();
    }
}