# Devlog 03 – Weapon System, Animation Rigging & Visual State Control

Date: 20/01/26

## 🎯 Goal
Design and implement a **modular weapon system** integrated with advanced **animation rigging**, enabling smooth **weapon switching**, **reloading**, and **firing** while maintaining visual consistency and gameplay safety.

## 🧠 Design Approach
This phase focused on treating **weapons** as independent **visual and logical entities**, while ensuring that **animation**, **IK**, and **rigging systems** cooperate instead of conflicting.

Key goals:

- Allow multiple **weapons** with different **animation needs**.

- Ensure **locomotion**, **aiming**, and **weapon actions** can coexist.

- Prevent **animation conflicts** (e.g., reloading while grabbing).

- Use **Animation Events** to synchronize **gameplay state** with **visuals**.

- Avoid hardcoded **per-weapon animation logic** in the **player controller**.

The result is a **layered, data-driven animation and weapon visualization system**.

---

## 🏗 Implementation
### 🔫 Weapon Holder & Switching

- Created a **WeaponHolder GameObject** attached to the player’s **right hand**.

- Contains **five weapons**:

  - **Pistol**

  - **Revolver**

  - **Auto Rifle**

  - **Shotgun**

  - **Rifle**

- **Weapon switching** is handled via script by enabling/disabling weapon GameObjects.

- Player logic remains **weapon-agnostic**.

---

### 🦴 Advanced Animation Rigging

Using Unity’s **Animation Rigging Package**:

Aim Alignment

- Added **Multi-Aim Constraint**:

  - **Head** and **right hand** track an **aim target**.

  - Ensures visual alignment with **cursor-based aiming**.

Left Hand IK (Per Weapon)

- Implemented **Two Bone IK Constraint** for the **left hand**.

- Each **weapon prefab** contains a child **LeftHandTargetTransform** with weapon-specific **grip offsets**.

- On weapon switch, the **left-hand IK target** is reassigned dynamically.

> Avoids hardcoded offsets and allows **per-weapon customization**.

---

### 🎞 Animator Layer Architecture

Animator now includes:

1. **Base Layer** – locomotion (**idle**, **walk**, **run**)

2. **Common Weapon Layer** – **pistol**, **revolver**, **auto rifle**

3. **Shotgun Weapon Layer**

4. **Rifle Weapon Layer**

- **Shotgun** and **Rifle layers** are **synced** to the **Common Weapon Layer**, simplifying setup while allowing distinct **firing feedback** per weapon type.

---


### 🔄 Reload Animation & Rig Weight Control

Reloading required temporarily disabling **IK** and **aim locking** to avoid unnatural poses.

Solution:

- Added **Animation Events** that control **rig** and **IK weights**.

- **Rig weight** is reduced at reload start and smoothly restored afterward:

```csharp
private void ReduceRigWeight()
{
    rig.weight = .15f;
}
```
- Weight restoration is handled gradually for smooth blending.

---

### 🤲 Weapon Grab Animation System

- Implemented **weapon grab animations** with two styles:

  - **Side Grab**
  - **Back Grab**

- Introduced a **GrabType enum** (code block untouched).

```csharp
public enum GrabType 
{
    SideGrab, 
    BackGrab
}
```
- Animator uses a **Blend Tree** driven by the **GrabType enum** value.

- Added **isGrabbingWeapon** boolean to:

  - Block **reloads** during grabs.
  - Prevent conflicting animations.

---

### 🎯 Animation Events as a Control Bridge

Added a dedicated **PlayerAnimationEvents** script to the **character model**.

Responsibilities:

- Restore **rig** and **IK weights** after reload.

- Signal when **weapon grab animations** finish.

- Decouple **animation timing** from **gameplay logic**.

```csharp
public void WeaponGrabIsOver()
{
    visualController.SetBusyGrabbingWeaponTo(false);
}
```
> Keeps **animation-driven state changes** explicit and maintainable.

---

## ⚠ Problems Encountered
- **IK** and **aim constraints** interfering with **reload animations**.

- Visual conflicts between **grabbing**, **reloading**, and **firing**.

- **Per-weapon grip positioning** without hardcoding offsets.

- Managing multiple **animation layers** without duplication.

- Preventing **invalid animation combinations**.

---

## ✅ Solutions
- Temporarily lowered **rig** and **IK weights** during **reload** and **grab animations**.

- Introduced explicit **animation-driven state flags** (**isGrabbingWeapon**).

- Used **Animation Events** as **synchronization points**.

- Stored per-weapon **IK targets** as child GameObjects.

- Synced specialized **weapon layers** to a shared **Common Weapon Layer**.

---

## 🚀 Result
- Seamless **weapon switching** with correct **hand placement**.

- **Reload animations** that temporarily disable **aim** and **IK** cleanly.

- **Weapon grab animations** integrated safely with other actions.

- **Layered animation system** supporting different **weapon behaviors**.

- Scalable **visual architecture** ready for more weapons and polish.

  ![Weapon Grab, Reload, Switching GIF](gifs/03.gif) <!-- placeholder for GIF -->

---

## 📈 Engineering Takeaways
- **Animation systems** benefit greatly from **explicit state ownership**.

- **Animation Events** are powerful when used as **synchronization boundaries**.

- **IK** should be treated as a **dynamic system**, not a constant constraint.

- **Data-driven transforms** scale far better than hardcoded offsets.

- **Layered animation architectures** prevent **state explosion**.

---

## ➡ Next Steps
- Configure **camera** and camera settings for **top-down perspective**.

- Decompose **player movement** and **aim** for modular control.

- Implement **smooth character rotation** using **Lerp/Slerp**.

- Add **camera lookahead** to anticipate player movement.

- Separate **aim logic** from **camera orientation** for precision.

- Create **bullet prefab** and set up **direction** and **trajectory**.

- Implement **precise aiming** and **target lock** systems.

- Visualize **aiming** with a **laser indicator** for feedback.