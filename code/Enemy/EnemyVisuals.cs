using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// High-level melee weapon category used for selecting compatible enemy melee weapon visuals.
/// </summary>
public enum EnemyMeleeWeaponType
{
    OneHand,
    Throw,
    Unarmed
}

/// <summary>
/// High-level melee weapon category used for selecting compatible enemy melee weapon visuals.
/// </summary>
public enum EnemyRangeWeaponType
{
    Pistol,
    Revolver,
    Shotgun,
    AutoRifle,
    Rifle
}

/// <summary>
/// Central visual coordinator for enemies.
/// </summary>
/// <remarks>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Randomize enemy look (body texture, weapon model selection, corruption crystals).</description></item>
/// <item><description>Activate a compatible <see cref="EnemyWeaponModel"/> for the owning enemy (melee/ranged).</description></item>
/// <item><description>Manage Animation Rigging constraints (left-hand IK + weapon aim) and smoothly blend their weights.</description></item>
/// <item><description>Support temporary visuals such as grenade throw model swaps.</description></item>
/// </list>
///
/// Key connections:
/// - Reads enemy config from <see cref="Enemy"/>, <see cref="EnemyMelee"/>, and <see cref="EnemyRange"/>.
/// - Weapon model compatibility is defined by <see cref="EnemyWeaponModel.IsCompatibleWith(Enemy)"/>.
/// - Combat states toggle visuals and IK via <see cref="EnableWeaponModel"/>, <see cref="EnableSecondaryWeaponModel"/>,
///   <see cref="EnableGrenadeModel"/>, and <see cref="EnableIK"/>.
/// </remarks>
public class EnemyVisuals : MonoBehaviour
{
    #region Dependencies

    private Enemy enemy;

    #endregion

    #region Inspector

    [Header("Colors To Randomize")]
    [SerializeField] private Texture[] colorTextures;
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;

    // Weapon Visuals to randomize will be done inside the function, since we need to filter them based on the enemy type (melee/range) and weapon type.
    [Header("Weapon Visuals To Randomize")]
    [SerializeField] private EnemyWeaponModel[] weaponModels;
    public EnemyWeaponModel currentWeaponModel { get; private set; }
    [SerializeField] private GameObject grenadeModel;

    [Header("Rig References")]
    [SerializeField] private TwoBoneIKConstraint leftHandIKConstraint;
    [SerializeField] private MultiAimConstraint weaponAimConstraint;
    [SerializeField] private Transform leftHandIK;
    [SerializeField] private Transform leftElbowIK;

    private float leftHandTargetWeight;
    private float weaponAimTargetWeight;
    private float rigChangeRate;

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

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Caches component references and initializes shared data used for visual randomization.
    /// </summary>
    /// <remarks>
    /// This includes:
    /// <list type="bullet">
    /// <item><description>Finding the attached <see cref="Enemy"/> component.</description></item>
    /// <item><description>Collecting all child <see cref="EnemyWeaponModel"/> components.</description></item>
    /// <item><description>Auto-assigning the <see cref="SkinnedMeshRenderer"/> if missing.</description></item>
    /// <item><description>Preparing the <see cref="MaterialPropertyBlock"/> and shader property ID used for texture overrides.</description></item>
    /// </list>
    /// </remarks>
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

    private void Update()
    {
        if (leftHandIKConstraint != null)
            leftHandIKConstraint.weight = AdjustIKWeight(leftHandIKConstraint.weight, leftHandTargetWeight);
        if (weaponAimConstraint != null)
        weaponAimConstraint.weight = AdjustIKWeight(weaponAimConstraint.weight, weaponAimTargetWeight);
    }

    /// <summary>
    /// Optionally randomizes the enemy's appearance whenever the object becomes enabled.
    /// </summary>
    /// <remarks>
    /// This is especially useful for pooled enemies, allowing reused instances to receive a fresh look.
    /// </remarks>
    private void OnEnable()
    {
        // Great for pooling: every time enemy is reused, it can get a new look.
        if (randomizeOnEnable)
            SetupLook();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Randomizes the full visual setup of the enemy.
    /// </summary>
    /// <remarks>
    /// This includes the body color texture, weapon model, and corruption crystal activation.
    /// </remarks>
    public void SetupLook()
    {
        SetupRandomColor();
        SetupRandomWeapon();
        SetupRandomCorruption();
    }

    public void EnableGrenadeModel(bool active) => grenadeModel?.SetActive(active);

    /// <summary>
    /// Enables or disables the visibility of the current weapon model in the game environment.
    /// </summary>
    /// <remarks>This method directly affects the rendering of the weapon model. Ensure that the weapon model
    /// is properly initialized before calling this method.</remarks>
    /// <param name="active">A value indicating whether to activate (<see langword="true"/>) or deactivate (<see langword="false"/>) the
    /// weapon model.</param>
    public void EnableWeaponModel(bool active)
    {
        currentWeaponModel?.gameObject.SetActive(active);
    }

    /// <summary>
    /// Enables or disables the secondary weapon model based on the specified state.
    /// </summary>
    /// <remarks>This method locates the secondary weapon model and sets its active state accordingly. Ensure
    /// that the secondary weapon model is properly initialized before calling this method.</remarks>
    /// <param name="active">A boolean value indicating whether to activate or deactivate the secondary weapon model. Pass <see
    /// langword="true"/> to enable the model; otherwise, pass <see langword="false"/> to disable it.</param>
    public void EnableSecondaryWeaponModel(bool active)
    {
        FindSecondaryWeaponModel()?.SetActive(active);
    }

    /// <summary>
    /// Enables or disables the active weapon model's trail effect.
    /// </summary>
    /// <param name="enable">Whether the trail effect should be enabled.</param>
    /// <remarks>
    /// If no weapon model is currently active, the call is ignored.
    /// </remarks>
    public void EnableWeaponTrail(bool enable)
    {
        if (currentWeaponModel == null)
            return;

        currentWeaponModel.EnableTrailEffect(enable);
    }

    #endregion

    #region Internal Logic

    /// <summary>
    /// Applies a random texture override to the configured material slot on the enemy mesh.
    /// </summary>
    /// <remarks>
    /// The texture is assigned through a <see cref="MaterialPropertyBlock"/>, which avoids
    /// instantiating a unique material for this renderer.
    /// </remarks>
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

    /// <summary>
    /// Selects, activates, and configures a random weapon model compatible with this enemy.
    /// </summary>
    /// <remarks>
    /// The selected model may also override the animator controller, switch animation layers,
    /// and update the left-hand IK targets if required by the weapon setup.
    /// </remarks>
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

        SetupLeftHandIK(); // Set up left-hand IK if required by the weapon model
    }

    /// <summary>
    /// Randomly activates a subset of corruption crystal visuals on the enemy.
    /// </summary>
    /// <remarks>
    /// All crystals are first disabled, then up to <see cref="corruptionAmount"/> unique crystals
    /// are chosen and activated.
    /// </remarks>
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

    /// <summary>
    /// Finds a random weapon model that is compatible with the current enemy.
    /// </summary>
    /// <returns>
    /// A randomly selected compatible <see cref="EnemyWeaponModel"/>, or <see langword="null"/>
    /// if no compatible models are available.
    /// </returns>
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

    /// <summary>
    /// Disables all weapon model GameObjects managed by this component.
    /// </summary>
    /// <remarks>
    /// This is typically called before selecting and enabling a new compatible weapon model.
    /// </remarks>
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

    /// <summary>
    /// Applies the selected weapon model's animator override controller, if one is provided.
    /// </summary>
    /// <remarks>
    /// If the current weapon model has no override controller assigned, the animator remains unchanged.
    /// </remarks>
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

    /// <summary>
    /// Collects all corruption crystal child objects attached to this enemy.
    /// </summary>
    /// <returns>
    /// An array containing the <see cref="GameObject"/> for each found
    /// <see cref="EnemyCorruptionCrystal"/> component.
    /// </returns>
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

    /// <summary>
    /// Finds the secondary weapon model that matches the enemy's weapon type.
    /// </summary>
    /// <remarks>This method searches through all child components of type EnemySecondaryRangeWeaponModel and
    /// returns the first one that matches the weapon type of the parent EnemyRange component.
    /// This is currently ONLY for the Enemy Range variation.</remarks>
    /// <returns>The GameObject representing the secondary weapon model if found; otherwise, null.</returns>
    private GameObject FindSecondaryWeaponModel()
    {
        EnemySecondaryRangeWeaponModel[] weaponModels = GetComponentsInChildren<EnemySecondaryRangeWeaponModel>(true);
        EnemyRangeWeaponType weaponType = GetComponentInParent<EnemyRange>().weaponType;

        foreach (var weaponModel in weaponModels)
        {
            if (weaponModel.weaponType == weaponType)
                return weaponModel.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Enables the requested animation layer and disables all other non-base layers.
    /// </summary>
    /// <param name="layerIndex">The animator layer index to activate.</param>
    /// <remarks>
    /// Layer 0 is treated as the base layer and is not reset by this method.
    /// Only layers starting from index 1 are cleared before the target layer is enabled.
    /// </remarks>
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

    /// <summary>
    /// Editor-only helper used to manually randomize the enemy's look from the context menu.
    /// </summary>
    [ContextMenu("Debug Randomize Look")]
    private void DebugRandomizeLook()
    {
        SetupLook();
    }

    /// <summary>
    /// Enables or disables the enemy rig constraints used for left-hand IK and weapon aiming.
    /// </summary>
    /// <param name="enableLeftHand">Whether the left-hand IK constraint should be active.</param>
    /// <param name="enableAim">Whether the weapon aim constraint should be active.</param>
    /// <param name="changeRate">The speed at which the IK weights transition to their target values. Higher values result in faster transitions.</param>
    public void EnableIK(bool enableLeftHand, bool enableAim, float changeRate = 10)
    {
        rigChangeRate = changeRate;

        if (leftHandIKConstraint != null)
            leftHandTargetWeight = enableLeftHand ? 1 : 0;
        else
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] Left hand IK constraint reference is missing on {name}. Cannot enable/disable left-hand IK.", this);

        if (weaponAimConstraint != null)
            weaponAimTargetWeight = enableAim ? 1 : 0;
        else
            Debug.LogWarning($"[{nameof(EnemyVisuals)}] Weapon aim constraint reference is missing on {name}. Cannot enable/disable weapon aim IK.", this);

    }

    /// <summary>
    /// Copies the selected weapon model's left-hand IK pose targets into the enemy rig targets.
    /// </summary>
    /// <remarks>
    /// This is only performed when the current weapon requires left-hand IK and all required
    /// rig and weapon target references are available.
    /// </remarks>
    private void SetupLeftHandIK()
    {
        // No weapon selected, or the selected weapon does not use left-hand IK.
        if (currentWeaponModel == null || !currentWeaponModel.RequiresLeftHandIK)
            return;

        // These are the actual rig targets on the enemy skeleton that we move.
        if (leftHandIK == null || leftElbowIK == null)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyVisuals)}] Left hand or elbow IK references are missing on {name}, but the selected weapon model requires them.",
                this);
            return;
        }

        // These are the pose targets stored on the weapon model itself.
        if (currentWeaponModel.LeftHandTarget == null || currentWeaponModel.LeftElbowTarget == null)
        {
            Debug.LogWarning(
                $"[{nameof(EnemyVisuals)}] IK targets are missing on weapon model '{currentWeaponModel.name}' for {name}.",
                this);
            return;
        }

        // Copy the local pose from the weapon model's helper targets into the enemy rig IK targets.
        leftHandIK.localPosition = currentWeaponModel.LeftHandTarget.localPosition;
        leftHandIK.localRotation = currentWeaponModel.LeftHandTarget.localRotation;

        leftElbowIK.localPosition = currentWeaponModel.LeftElbowTarget.localPosition;
        leftElbowIK.localRotation = currentWeaponModel.LeftElbowTarget.localRotation;
    }

    /// <summary>
    /// Interpolates the current IK weight toward the target weight using a defined change rate, ensuring smooth
    /// transitions.
    /// </summary>
    /// <remarks>This method uses linear interpolation to gradually adjust the IK weight, which helps prevent
    /// abrupt changes in animation blending. The adjustment only occurs if the difference between the current and
    /// target weights exceeds 0.05, allowing for minor fluctuations to be ignored.</remarks>
    /// <param name="currentWeight">The current IK weight value to be adjusted. Typically ranges from 0.0 to 1.0.</param>
    /// <param name="targetWeight">The desired IK weight value to reach. Typically ranges from 0.0 to 1.0.</param>
    /// <returns>The adjusted IK weight value. Returns the target weight if the difference is less than or equal to 0.05;
    /// otherwise, returns a value interpolated toward the target weight.</returns>
    private float AdjustIKWeight(float currentWeight, float targetWeight)
    {
        if (Mathf.Abs(currentWeight - targetWeight) > 0.05f)
            return Mathf.Lerp(currentWeight, targetWeight, Time.deltaTime * rigChangeRate);
        else
            return targetWeight;
    }

    #endregion
}