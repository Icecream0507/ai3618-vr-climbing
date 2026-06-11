using UnityEditor;
using UnityEngine;
using VRClimb.Gameplay;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// Editor convenience (menu: <c>VRClimb ▸ Set Up Test Scene</c>) that removes most of the manual
    /// scaffolding for a playable scene: it creates the <c>Hold</c> layer if missing, drops a
    /// GameManager and a RouteBuilder into the scene, and attaches <see cref="PlayerClimberSetup"/> to
    /// an XR Origin if one is present. It does NOT replace reading docs/SETUP.md §6 — you still drop the
    /// XR Origin (XR Rig) prefab, run "Set Up Climber", and assign the grip actions + Hold layer.
    /// </summary>
    public static class ClimbSceneSetup
    {
        [MenuItem("VRClimb/Set Up Test Scene")]
        public static void SetUpTestScene()
        {
            EnsureLayer("Hold");

            if (Object.FindObjectOfType<GameManager>() == null)
            {
                var gm = new GameObject("GameManager", typeof(GameManager));
                Undo.RegisterCreatedObjectUndo(gm, "Create GameManager");
            }

            if (Object.FindObjectOfType<RouteBuilder>() == null)
            {
                var rb = new GameObject("RouteBuilder", typeof(RouteBuilder));
                Undo.RegisterCreatedObjectUndo(rb, "Create RouteBuilder");
            }

            var origin = FindXrOrigin();
            if (origin != null && origin.GetComponent<PlayerClimberSetup>() == null)
            {
                origin.AddComponent<PlayerClimberSetup>();
                Debug.Log("[VRClimb] Added PlayerClimberSetup to '" + origin.name +
                          "'. Assign head/controllers and run its 'Set Up Climber' context menu.", origin);
            }
            else if (origin == null)
            {
                Debug.LogWarning("[VRClimb] No XR Origin found in the scene. Drop the XR Origin (XR Rig) " +
                                 "prefab from the XRI Starter Assets, then re-run this, or add PlayerClimberSetup manually.");
            }

            Debug.Log("[VRClimb] Test scene scaffolded. Remaining manual steps (docs/SETUP.md §6): " +
                      "on each ClimbingHand assign gripAction; set the Hold layer on the hands + FootPlacementSystem.");
        }

        static GameObject FindXrOrigin()
        {
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go.name.Contains("XR Origin") || go.name.Contains("XR Rig"))
                    return go;
            return null;
        }

        // Adds a named layer to the first free user slot (8..31) if it doesn't already exist.
        public static void EnsureLayer(string layerName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[VRClimb] Could not load TagManager.asset to create the '" + layerName + "' layer.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == layerName) return;   // already exists

            for (int i = 8; i < layers.arraySize; i++)
            {
                var sp = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(sp.stringValue))
                {
                    sp.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[VRClimb] Created layer '" + layerName + "' (slot " + i + ").");
                    return;
                }
            }

            Debug.LogWarning("[VRClimb] No free user-layer slot available for '" + layerName + "'.");
        }
    }
}
