using UnityEngine;

namespace DoomsdayTunnels.UI
{
    /// <summary>
    /// The part of the screen a phone lets us use: inside the notch, the Dynamic Island and the rounded corners.
    /// Everything the HUD draws is laid out within it; the full-screen washes still cover the whole screen. A
    /// screenshot run can fake insets to check the layout on a Mac.
    /// </summary>
    public static class SafeArea
    {
        public static Rect Fake;
        public static Rect Get() => Fake.width > 0 ? Fake : Screen.safeArea;
        public static float W => Get().width;
        public static float H => Get().height;
        public static float Left => Get().x;
        public static float Top => Screen.height - Get().yMax;        // in GUI terms: how far down the safe area starts
        /// <summary>Unity screen coordinates (y up) to GUI coordinates inside the safe area (y down).</summary>
        public static Vector2 ToGui(Vector2 screen) => new Vector2(screen.x - Get().x, Get().yMax - screen.y);
        public static Vector2 ToScreen(Vector2 gui) => new Vector2(gui.x + Get().x, Get().yMax - gui.y);
    }
}
