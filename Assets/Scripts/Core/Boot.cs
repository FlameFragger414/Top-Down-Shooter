using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Neon
{
    /// The whole game builds itself here. Any scene you press Play on becomes Neon Miami,
    /// so there is nothing to wire up by hand in the editor.
    public static class Boot
    {
        /// Kept so the HUD can tighten the vignette while scoped.
        public static Vignette Vig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            var root = new GameObject("NeonMiami");

            Physics2D.gravity = Vector2.zero;
            Physics2D.queriesStartInColliders = false;
            for (int i = 0; i < 32; i++)
            {
                Physics2D.IgnoreLayerCollision(Layer.PickupL, i, true);
                Physics2D.IgnoreLayerCollision(Layer.PlayerShotL, i, true);
                Physics2D.IgnoreLayerCollision(Layer.EnemyShotL, i, true);
                Physics2D.IgnoreLayerCollision(Layer.CorpseL, i, true);
            }

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.position = new Vector3(0, 0, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.035f, 0.025f, 0.055f);
            cam.cullingMask = ~(1 << HUD.UiLayer);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CamCtrl>();

            SetupPostProcessing(cam, root.transform);

            Sfx.Create(root.transform);
            FX.Create(root.transform);
            LevelGen.Create(root.transform);
            GameManager.Create(root.transform);
            HUD.Create(root.transform, cam);

            Application.targetFrameRate = 144;
        }

        static void SetupPostProcessing(Camera cam, Transform parent)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
            }

            var volGo = new GameObject("PostFX");
            volGo.transform.SetParent(parent, false);
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = profile;

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 0.88f;
            bloom.intensity.value = 1.05f;
            bloom.scatter.value = 0.68f;
            bloom.tint.value = new Color(1f, 0.86f, 0.98f);

            var vig = Vig = profile.Add<Vignette>(true);
            vig.intensity.value = 0.28f;
            vig.smoothness.value = 0.55f;
            vig.color.value = new Color(0.05f, 0f, 0.08f);

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.value = 0.14f;

            var grain = profile.Add<FilmGrain>(true);
            grain.intensity.value = 0.20f;
            grain.response.value = 0.7f;

            var col = profile.Add<ColorAdjustments>(true);
            col.postExposure.value = 0.42f;
            col.contrast.value = 9f;
            col.saturation.value = 15f;

            var curves = profile.Add<LiftGammaGain>(true);
            curves.lift.value = new Vector4(1.02f, 0.97f, 1.10f, -0.02f);
            curves.gain.value = new Vector4(1.05f, 0.98f, 1.06f, 0.02f);
        }
    }
}
