using DoomsdayTunnels.Flight;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace DoomsdayTunnels.Input
{
    public enum Haptic { None, Light, Medium, Heavy, Success, Warning, Error }

    /// <summary>
    /// A buzz in the hand for the moments that deserve one: a hit or a wall bounce, boost kicking in, the ship token,
    /// a sector cleared, the guns overheating. iPhones get the Taptic Engine through a tiny native plugin
    /// (Plugins/iOS/DoomsdayHaptics.mm); Android gets its vibrator; everything else is silent. Off in settings.
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled = true;
        static float lastLight;

        /// <summary>Which haptic, if any, a sim event earns. Pure, so it is tested.</summary>
        public static Haptic For(SimEventKind kind)
        {
            switch (kind)
            {
                case SimEventKind.Hit: return Haptic.Heavy;                 // a crash, or the first bounce off a wall
                case SimEventKind.Scrape: return Haptic.Light;
                case SimEventKind.BoostOn: return Haptic.Medium;
                case SimEventKind.BoostDry: return Haptic.Warning;
                case SimEventKind.Pick: case SimEventKind.Refuel: return Haptic.Light;
                case SimEventKind.Clear: return Haptic.Success;             // the ship token
                case SimEventKind.SectorClear: return Haptic.Success;
                case SimEventKind.ShipLost: case SimEventKind.TimeOut: return Haptic.Error;
                default: return Haptic.None;
            }
        }

        public static void Play(Haptic h)
        {
            if (!Enabled || h == Haptic.None) return;
            if (h == Haptic.Light) { if (Time.unscaledTime - lastLight < 0.08f) return; lastLight = Time.unscaledTime; }   // scrapes come every frame or so
#if UNITY_IOS && !UNITY_EDITOR
            switch (h)
            {
                case Haptic.Light: DoomsdayHapticImpact(0); break;
                case Haptic.Medium: DoomsdayHapticImpact(1); break;
                case Haptic.Heavy: DoomsdayHapticImpact(2); break;
                case Haptic.Success: DoomsdayHapticNotify(0); break;
                case Haptic.Warning: DoomsdayHapticNotify(1); break;
                case Haptic.Error: DoomsdayHapticNotify(2); break;
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            // the view's haptic feedback constants: no permission needed, and the phone's own haptic settings apply
            switch (h)
            {
                case Haptic.Light: Android(4, 1); break;        // CLOCK_TICK, else VIRTUAL_KEY
                case Haptic.Medium: Android(1, 1); break;       // VIRTUAL_KEY
                case Haptic.Heavy: Android(0, 0); break;        // LONG_PRESS
                case Haptic.Success: Android(16, 1); break;     // CONFIRM (Android 11+), else VIRTUAL_KEY
                case Haptic.Warning: Android(17, 0); break;     // REJECT (Android 11+), else LONG_PRESS
                case Haptic.Error: Android(0, 0); break;
            }
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void DoomsdayHapticImpact(int style);
        [DllImport("__Internal")] static extern void DoomsdayHapticNotify(int type);
#elif UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject view; static bool tried;
        static void Android(int constant, int fallback)
        {
            try
            {
                if (!tried)
                {
                    tried = true;
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var window = activity.Call<AndroidJavaObject>("getWindow")) view = window.Call<AndroidJavaObject>("getDecorView");
                }
                if (view == null) return;
                if (!view.Call<bool>("performHapticFeedback", constant) && fallback != constant) view.Call<bool>("performHapticFeedback", fallback);
            }
            catch { view = null; }
        }
#endif
    }
}
