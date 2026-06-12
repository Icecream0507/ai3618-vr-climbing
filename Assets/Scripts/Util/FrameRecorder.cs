using System.IO;
using UnityEngine;

namespace VRClimb.Util
{
    /// <summary>
    /// Offline frame grabber for the recorded demo. When <see cref="record"/> is on it renders the
    /// spectator camera to a RenderTexture each step and writes a numbered JPG to <see cref="outDir"/>,
    /// driving the clock with <c>Time.captureDeltaTime</c> so playback is deterministic regardless of
    /// how slow the offline render is. After the run, ffmpeg assembles the JPGs into an mp4 (see
    /// DemoBuild / docs/TESTING.md). Inert unless <see cref="record"/> is set (DemoBuild sets it only
    /// for the headless record run, never for the hand-played Demo scene).
    /// </summary>
    public class FrameRecorder : MonoBehaviour
    {
        public bool record = false;
        public Camera spectator;
        public int width = 1280;
        public int height = 720;
        public int fps = 30;
        public string outDir = "Logs/frames";

        public int FrameCount { get; private set; }

        RenderTexture _rt;
        Texture2D _tex;

        void Awake()
        {
            if (!record) { enabled = false; return; }
            if (Directory.Exists(outDir))
                foreach (var f in Directory.GetFiles(outDir)) File.Delete(f);
            Directory.CreateDirectory(outDir);

            Time.captureDeltaTime = 1f / Mathf.Max(1, fps);
            _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "DemoRT" };
            _tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        void LateUpdate()
        {
            if (!record || spectator == null) return;

            var prevTarget = spectator.targetTexture;
            var prevActive = RenderTexture.active;

            spectator.targetTexture = _rt;
            spectator.Render();

            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            _tex.Apply(false);

            spectator.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            File.WriteAllBytes(Path.Combine(outDir, $"f_{FrameCount:00000}.jpg"), ImageConversion.EncodeToJPG(_tex, 92));
            FrameCount++;
        }

        void OnDisable()
        {
            Time.captureDeltaTime = 0f;
            if (_rt != null) { _rt.Release(); _rt = null; }
        }
    }
}
