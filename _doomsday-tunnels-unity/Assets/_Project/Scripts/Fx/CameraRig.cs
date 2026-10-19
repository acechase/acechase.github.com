using DoomsdayTunnels.Flight;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Fx
{
    /// <summary>
    /// Puts the camera where the ship is and points it down the tunnel. Banking comes from the sim's roll, boost
    /// widens the view a little, and impacts shake it. Reduce-motion turns off the shake and the bank.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam;
        public float BaseFov = 61f;
        public float Shake;                     // 0..1, decays
        public bool ReduceMotion;
        Vector3 smoothForward = Vector3.forward;

        public void Place(FlightSim sim, TunnelPath path, float sway, float dt)
        {
            var pos = path.World(sim.Rx, sim.Ry, sim.Pz);
            var ahead = path.Tangent(sim.Pz + 3f);
            smoothForward = Vector3.Slerp(smoothForward, ahead, Mathf.Min(1, dt * 3f)).normalized;
            Shake = Mathf.Max(0, Shake - dt * 3f);
            float k = ReduceMotion ? 0 : Shake * 0.12f * sway;
            pos += new Vector3((Random.value - 0.5f) * k, (Random.value - 0.5f) * k, 0);
            var rot = Quaternion.LookRotation(smoothForward, Vector3.up);
            if (!ReduceMotion) rot = rot * Quaternion.AngleAxis(sim.Roll * Mathf.Rad2Deg, Vector3.forward);
            transform.SetPositionAndRotation(pos, rot);
            // Boost: the web game shortens the focal length by a tenth; the same thing as a wider field of view.
            float focal = 1f - (ReduceMotion ? 0 : 0.1f) * sim.BoostEase;
            Cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(BaseFov * 0.5f * Mathf.Deg2Rad) / focal) * Mathf.Rad2Deg;
        }
    }
}
