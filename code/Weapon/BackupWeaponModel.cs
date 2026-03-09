using UnityEngine;

/// <summary>
/// Where a backup weapon model is "hung" on the player body.
/// Used to show non-equipped weapons visually.
/// </summary>
public enum HangType
{
    LowBackHang,
    BackHang,
    SideHang
}

/// <summary>
/// Visual model for a weapon that is not currently equipped.
/// Managed by <see cref="PlayerWeaponVisuals"/>.
/// </summary>
public class BackupWeaponModel : MonoBehaviour
{
    public WeaponType weaponType;
    [SerializeField] private HangType hangType;

    public void Activate(bool activated) => gameObject.SetActive(activated);

    public bool HangTypeIs(HangType hangType) => this.hangType == hangType;
}
