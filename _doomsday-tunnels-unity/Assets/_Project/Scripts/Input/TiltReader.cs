using UnityEngine;
using UnityEngine.InputSystem;

namespace DoomsdayTunnels.Input
{
    public enum TiltStatus { None, Wait, On }

    /// <summary>
    /// Tilt steering: the phone's tilt steers the ship, the thumb stick aims the guns. The maths is the web game's: work
    /// from the "up" vector in the phone's own axes rather than from Euler angles, which jump when a phone is held
    /// sideways. Roll is how far the right side of the picture dips; pitch is how far the top edge is tipped away. Both
    /// are measured from a calibration taken when a sector starts or the game resumes, so however the phone is held
    /// then is "straight on".
    /// </summary>
    public sealed class TiltReader
    {
        public float Roll, Pitch, Roll0, Pitch0;        // radians
        public float Fx, Fy;                            // filtered steering, -1..1
        public bool NeedCal = true, Ok;
        float enabledAt = -1, lastSample = -1;
        Vector3 accel;                                  // low-passed accelerometer, when there is no gravity sensor

        public void Enable()
        {
            if (enabledAt >= 0) return;
            enabledAt = Time.unscaledTime;
            if (GravitySensor.current != null) InputSystem.EnableDevice(GravitySensor.current);
            else if (Accelerometer.current != null) InputSystem.EnableDevice(Accelerometer.current);
        }

        public bool Active => Ok && Time.unscaledTime - lastSample < 1.5f;
        public TiltStatus Status
        {
            get
            {
                if (GravitySensor.current == null && Accelerometer.current == null) return TiltStatus.None;
                if (Active) return TiltStatus.On;
                if (enabledAt >= 0 && Time.unscaledTime - enabledAt > 2f) return TiltStatus.None;
                return TiltStatus.Wait;
            }
        }

        /// <summary>Reads the sensor and updates the steering. `range` is degrees of tilt for full steering.</summary>
        public void Sample(float dt, float range, bool invertY)
        {
            Vector3 g;
            if (GravitySensor.current != null) g = GravitySensor.current.gravity.ReadValue();
            else if (Accelerometer.current != null) { var a = Accelerometer.current.acceleration.ReadValue(); accel += (a - accel) * Mathf.Min(1, dt * 5); g = accel; }
            else { Ok = false; return; }
            if (g.sqrMagnitude < 0.01f) return;
            var up = -g.normalized;                                             // x toward the phone's right edge, y its top edge, z out of the screen
            if (!InputSystem.settings.compensateForScreenOrientation) up = ToPicture(up, Screen.orientation);
            Measure(up, out Roll, out Pitch);
            Ok = true; lastSample = Time.unscaledTime;
            if (NeedCal) { Roll0 = Roll; Pitch0 = Pitch; Fx = Fy = 0; NeedCal = false; }
            Steer(Roll, Pitch, Roll0, Pitch0, range, invertY, out float tx, out float ty);
            float k = Mathf.Min(1, dt * 14); Fx += (tx - Fx) * k; Fy += (ty - Fy) * k;
        }

        /// <summary>"Up" measured along the picture's right and up directions, for a sensor that reports in the phone's portrait axes.</summary>
        public static Vector3 ToPicture(Vector3 u, ScreenOrientation o)
        {
            switch (o)
            {
                case ScreenOrientation.LandscapeLeft: return new Vector3(-u.y, u.x, u.z);       // top edge to the left
                case ScreenOrientation.LandscapeRight: return new Vector3(u.y, -u.x, u.z);
                case ScreenOrientation.PortraitUpsideDown: return new Vector3(-u.x, -u.y, u.z);
                default: return u;
            }
        }

        /// <summary>Roll (positive when the right side of the picture dips) and pitch (0 upright, a quarter turn lying flat) from the up vector.</summary>
        public static void Measure(Vector3 up, out float roll, out float pitch)
        {
            roll = Mathf.Asin(Mathf.Clamp(-up.x, -1, 1));
            pitch = Mathf.Atan2(up.z, up.y);
        }

        /// <summary>Steering from the tilt away from the calibrated hold: a dead zone, then linear to full at `range` degrees.</summary>
        public static void Steer(float roll, float pitch, float roll0, float pitch0, float rangeDeg, bool invertY, out float tx, out float ty)
        {
            float range = rangeDeg * Mathf.Deg2Rad;
            float dp = pitch - pitch0; if (dp > Mathf.PI) dp -= 2 * Mathf.PI; else if (dp < -Mathf.PI) dp += 2 * Mathf.PI;
            tx = (roll - roll0) / range;
            ty = -dp / (range * 0.8f) * (invertY ? -1 : 1);                   // top edge tipped away = dive, like a flight yoke
            float m = Mathf.Sqrt(tx * tx + ty * ty), dz = 0.08f;
            if (m > dz) { float g = Mathf.Min(1, (m - dz) / (1 - dz)) / m; tx *= g; ty *= g; } else tx = ty = 0;
        }
    }
}
