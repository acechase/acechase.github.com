using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// A chase camera on a soft arm just behind and above the ship, looking down the tunnel. It follows the ship across
    /// the tunnel at a fraction of its offset so the tunnel ahead stays in view, banks with the sim's roll, widens its
    /// view under boost, and takes impulses: a kick away from a scraped wall, a jolt on a hit, that settle on a spring.
    /// Reduce-motion turns off the bank, the kicks and the boost zoom.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public float BaseFov = 64f;
        public float Shake;                     // 0..1, decays: the old random jitter, kept for big hits
        public bool ReduceMotion;
        public bool Chase = true;               // false: the web game's view, from the ship itself
        Vector3 smoothForward = Vector3.forward, kick, kickVel, follow;

        /// <summary>A push in a screen-space direction (x right, y up), in metres, that springs back.</summary>
        public void Impulse(Vector2 dir, float amount) { if (!ReduceMotion) kickVel += new Vector3(dir.x, dir.y, 0) * amount; }

        public void Place(FlightSim sim, TunnelPath path, float sway, float dt)
        {
            float back = Chase ? 2.9f : 0f, up = Chase ? 0.36f : 0f;
            var target = new Vector3(sim.Rx * (Chase ? 0.55f : 1f), sim.Ry * (Chase ? 0.4f : 1f) + up, 0);
            follow += (target - follow) * Mathf.Min(1, dt * 9);
            var pos = path.World(follow.x, follow.y, sim.Pz - back);
            var aim = path.World(sim.Rx * 0.25f, sim.Ry * 0.25f, sim.Pz + 5f);
            var ahead = Chase ? (aim - pos).normalized : path.Tangent(sim.Pz + 3f);
            smoothForward = Vector3.Slerp(smoothForward, ahead, Mathf.Min(1, dt * 4f)).normalized;
            // impulses settle on a damped spring; the old shake adds a little random jitter on top
            kickVel += (-kick * 90f - kickVel * 12f) * dt; kick += kickVel * dt;
            Shake = Mathf.Max(0, Shake - dt * 3f);
            float k = ReduceMotion ? 0 : Shake * 0.08f * sway;
            var rot = Quaternion.LookRotation(smoothForward, Vector3.up);
            pos += rot * (kick * (ReduceMotion ? 0 : 1) * sway) + new Vector3((Random.value - 0.5f) * k, (Random.value - 0.5f) * k, 0);
            if (!ReduceMotion) rot = rot * Quaternion.AngleAxis(sim.Roll * Mathf.Rad2Deg * (Chase ? 0.6f : 1f), Vector3.forward);
            transform.SetPositionAndRotation(pos, rot);
            // Boost: the web game shortens the focal length by a tenth; the same thing as a wider field of view.
            float focal = 1f - (ReduceMotion ? 0 : 0.1f) * sim.BoostEase;
            Cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(BaseFov * 0.5f * Mathf.Deg2Rad) / focal) * Mathf.Rad2Deg;
        }
    }
}
