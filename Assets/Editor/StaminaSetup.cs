using UnityEditor;
using UnityEngine;
using VRClimb.Gameplay;
using VRClimb.Climbing;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// P2 convenience (menu: <c>VRClimb ▸ Add Stamina to Climber</c>). Attaches a <see cref="StaminaSystem"/>
    /// to the climber (the GameObject that carries the <see cref="ClimbController"/>) if one isn't already
    /// present. The StaminaSystem self-wires from there: it auto-finds the two hands at runtime, and
    /// "VRClimb ▸ Set Up HUD + Audio" links it to the stamina bar.
    ///
    /// Run order for a fresh scene: Set Up Test Scene → Set Up Climber → (this) Add Stamina to Climber →
    /// Set Up HUD + Audio. Idempotent — re-running never adds a second StaminaSystem.
    /// </summary>
    public static class StaminaSetup
    {
        [MenuItem("VRClimb/Add Stamina to Climber")]
        public static void AddStamina()
        {
            var controller = Object.FindObjectOfType<ClimbController>();
            if (controller == null)
            {
                Debug.LogWarning("[VRClimb] No ClimbController found. Run 'Set Up Test Scene' and " +
                                 "'Set Up Climber' first, then re-run 'Add Stamina to Climber'.");
                return;
            }

            var go = controller.gameObject;
            if (go.GetComponent<StaminaSystem>() != null)
            {
                Debug.Log("[VRClimb] StaminaSystem already on '" + go.name + "' — nothing to do.");
                return;
            }

            Undo.AddComponent<StaminaSystem>(go);
            EditorUtility.SetDirty(go);
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[VRClimb] Added StaminaSystem to '" + go.name + "'. It auto-finds the hands at " +
                      "runtime; now run 'VRClimb ▸ Set Up HUD + Audio' so the stamina bar is driven.");
        }
    }
}
