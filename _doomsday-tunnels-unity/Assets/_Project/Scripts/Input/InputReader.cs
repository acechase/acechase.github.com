using DoomsdayTunnels.Flight;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DoomsdayTunnels.Input
{
    /// <summary>
    /// Turns whatever the player is holding into a SimInput. Keyboard (arrows or WASD, Space, Shift), gamepad, and on a
    /// touch screen the web game's layout: the left half is a thumb stick that appears where the thumb lands, the right
    /// half fires, and a round button at the bottom right boosts. Screen coordinates here are Unity's (origin bottom-left).
    /// </summary>
    public sealed class InputReader
    {
        public bool TouchUI;                    // a touch has been seen: draw the thumb controls
        public bool StickDown; public Vector2 StickAnchor, StickPos; public Vector2 Stick;    // stick in -1..1
        public bool FireDown, BoostDown;
        int stickId = -1, fireId = -1, boostId = -1;
        public float U => Mathf.Clamp(Screen.height / 400f, 0.75f, 1.7f);     // the web game's size unit
        public float StickRadius => 74f * U;
        public Vector2 BoostCentre => new Vector2(Screen.width - 186f * U, 54f * U);
        public float BoostRadius => 34f * U;
        public Vector2 FireCentre => new Vector2(Screen.width - 82f * U, 84f * U);
        public float FireRadius => 46f * U;

        /// <summary>True once per frame when the player tapped or pressed to advance a screen.</summary>
        public bool Advance;
        public bool PauseKey;
        /// <summary>Where the advancing tap or click landed, in screen coordinates; (-1, -1) for a key press.</summary>
        public Vector2 TapPos = new Vector2(-1, -1);

        public SimInput Read()
        {
            var inp = new SimInput();
            Advance = false; PauseKey = false; TapPos = new Vector2(-1, -1);
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) inp.SteerX -= 1;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) inp.SteerX += 1;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) inp.SteerY += 1;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) inp.SteerY -= 1;
                inp.Fire |= kb.spaceKey.isPressed;
                inp.Boost |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                Advance |= kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                PauseKey |= kb.pKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame;
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                var ls = gp.leftStick.ReadValue(); inp.SteerX += ls.x; inp.SteerY += ls.y;
                inp.Fire |= gp.buttonSouth.isPressed || gp.rightTrigger.ReadValue() > 0.3f;
                inp.Boost |= gp.buttonWest.isPressed || gp.leftTrigger.ReadValue() > 0.3f;
                Advance |= gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame;
                PauseKey |= gp.startButton.wasPressedThisFrame;
            }
            var ts = Touchscreen.current;
            if (ts != null) ReadTouches(ts, ref inp);
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && (ts == null || !AnyTouch(ts))) { Advance = true; TapPos = mouse.position.ReadValue(); }
            return inp;
        }

        static bool AnyTouch(Touchscreen ts) { foreach (var t in ts.touches) if (t.press.isPressed) return true; return false; }

        void ReadTouches(Touchscreen ts, ref SimInput inp)
        {
            bool stickSeen = false, fireSeen = false, boostSeen = false;
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                TouchUI = true;
                int id = t.touchId.ReadValue(); var p = t.position.ReadValue();
                if (id == stickId) { stickSeen = true; MoveStick(p); continue; }
                if (id == fireId) { fireSeen = true; continue; }
                if (id == boostId) { boostSeen = true; continue; }
                if (!t.press.wasPressedThisFrame) continue;                 // a touch we never claimed (started on a button)
                Advance = true; TapPos = p;
                if (p.x < Screen.width * 0.5f) { if (stickId < 0) { stickId = id; StickAnchor = p; StickPos = p; Stick = Vector2.zero; StickDown = true; stickSeen = true; } }
                else if (Vector2.Distance(p, BoostCentre) < BoostRadius * 1.3f) { boostId = id; boostSeen = true; }
                else { fireId = id; fireSeen = true; }
            }
            if (!stickSeen) { stickId = -1; StickDown = false; Stick = Vector2.zero; }
            if (!fireSeen) fireId = -1;
            if (!boostSeen) boostId = -1;
            FireDown = fireId >= 0; BoostDown = boostId >= 0;
            if (StickDown)
            {
                // Dead zone, then a soft curve so small thumb moves stay small.
                float m = Stick.magnitude, dz = 0.07f;
                if (m > dz) { float g = Mathf.Pow((Mathf.Min(1, m) - dz) / (1 - dz), 1.5f) / m; inp.SteerX += Stick.x * g; inp.SteerY += Stick.y * g; }
            }
            inp.Fire |= FireDown; inp.Boost |= BoostDown;
        }

        void MoveStick(Vector2 p)
        {
            float R = StickRadius; var d = p - StickAnchor; float len = d.magnitude;
            if (len > R) { StickAnchor += d * (1 - R / len); d *= R / len; }    // the stick's base follows a thumb that overshoots
            StickPos = p; Stick = d / R;
        }
    }
}
