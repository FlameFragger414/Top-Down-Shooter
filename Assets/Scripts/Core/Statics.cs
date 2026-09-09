using UnityEngine;

namespace Neon
{
    /// This project runs with "Enter Play Mode Options" on and both Reload Domain and
    /// Reload Scene disabled, so static state survives from one Play session to the next.
    /// Everything static the game owns is wiped here, before any other runtime hook.
    public static class Statics
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            // Sprites built with Sprite.Create die with play mode; the cache must go too.
            Art.ResetCache();

            Enemy.All.Clear();
            Pickup.ResetAll();

            // Singletons point at GameObjects from the previous run.
            PlayerCtrl.I = null;
            FX.I = null;
            Sfx.I = null;
            LevelGen.I = null;
            GameManager.I = null;
            HUD.I = null;
            CamCtrl.I = null;
            Boot.Vig = null;

            Time.timeScale = 1f;
        }
    }
}
