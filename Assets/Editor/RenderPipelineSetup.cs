using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRClimb.EditorTools
{
    /// <summary>
    /// Creates and activates a URP pipeline asset (menu: <c>VRClimb ▸ Ensure URP Pipeline Asset</c>).
    /// The project uses the URP package, but without a pipeline asset assigned in Graphics settings
    /// Unity falls back to Built-in and every "Universal Render Pipeline/Lit" material — including all
    /// of <c>RouteBuilder</c>'s hold colours — renders magenta. Run once; the assets land in
    /// Assets/Settings and the assignment is saved into ProjectSettings (committed for the team).
    /// </summary>
    public static class RenderPipelineSetup
    {
        const string Dir = "Assets/Settings";
        const string RendererPath = Dir + "/URP-Renderer.asset";
        const string PipelinePath = Dir + "/URP-Pipeline.asset";

        [MenuItem("VRClimb/Ensure URP Pipeline Asset")]
        public static void Ensure()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (rp == null)
            {
                if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Settings");
                var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
                rp = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(rp, PipelinePath);
                Debug.Log("[VRClimb] Created URP pipeline asset at " + PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = rp;   // Graphics ▸ Scriptable Render Pipeline
            QualitySettings.renderPipeline = rp;           // and for the active quality level
            AssetDatabase.SaveAssets();
            Debug.Log("[VRClimb] URP pipeline asset assigned in Graphics/Quality settings.");
        }
    }
}
