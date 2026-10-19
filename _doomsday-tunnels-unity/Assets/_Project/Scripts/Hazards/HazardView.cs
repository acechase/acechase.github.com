using DoomsdayTunnels.Core;
using DoomsdayTunnels.Tunnel;
using UnityEngine;

namespace DoomsdayTunnels.Hazards
{
    /// <summary>
    /// The visible side of one SectorObject, built from primitives. The sim owns the truth; this mirrors it, refreshing
    /// only when the object's Version changes (a wall hit, a block shot, a cell taken).
    /// </summary>
    public sealed class HazardView : MonoBehaviour
    {
        public SectorObject Obj;
        public bool Dropped;                 // in the branch the ship did not take: never shown again this sector
        TunnelPath path; Material baseMat; int seen = -1;
        Renderer[] parts; Transform spin;
        static readonly Color WallFill = Color.Lerp(Palette.Warn, Palette.Void, 0.48f), WallHit = Color.Lerp(Palette.Warn, Palette.Void, 0.82f);

        public static HazardView Create(SectorObject o, TunnelPath path, Material unlit, Palette pal, Transform parent)
        {
            var go = new GameObject(o.Kind.ToString());
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<HazardView>();
            v.Obj = o; v.path = path; v.baseMat = unlit;
            v.Build(pal);
            return v;
        }

        Renderer Part(PrimitiveType type, Vector3 local, Vector3 scale, Quaternion rot, Color col, float intensity = 1f)
        {
            var p = GameObject.CreatePrimitive(type);
            Destroy(p.GetComponent<Collider>());                       // collisions are analytic; PhysX is for debris only
            p.transform.SetParent(transform, false);
            p.transform.localPosition = local; p.transform.localScale = scale; p.transform.localRotation = rot;
            var r = p.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.material = new Material(baseMat);
            r.material.SetColor("_Tint", col); r.material.SetFloat("_Intensity", intensity);
            return r;
        }

        void Build(Palette pal)
        {
            transform.position = path.World(0, 0, Obj.Z, Obj.Branch);
            float haze = 1f;
            switch (Obj.Kind)
            {
                case ObjKind.Wall:
                    parts = new Renderer[Obj.Rects.Length];
                    for (int i = 0; i < Obj.Rects.Length; i++)
                    {
                        var r = Obj.Rects[i];
                        parts[i] = Part(PrimitiveType.Cube, new Vector3(r.Cx, r.Cy, 0), new Vector3(r.X1 - r.X0, r.Y1 - r.Y0, 0.16f), Quaternion.identity, WallFill);
                        Frame(parts[i].transform, Palette.Warn);
                    }
                    break;
                case ObjKind.Gate:
                    parts = new Renderer[Obj.Blocks.Length];
                    for (int i = 0; i < Obj.Blocks.Length; i++)
                    {
                        var b = Obj.Blocks[i];
                        parts[i] = Part(PrimitiveType.Cube, new Vector3(b.R.Cx, b.R.Cy, 0), new Vector3(b.R.X1 - b.R.X0 - 0.04f, b.R.Y1 - b.R.Y0 - 0.04f, 0.12f), Quaternion.identity, b.Hp > 1 ? Palette.GateFill2 : Palette.GateFill);
                        Frame(parts[i].transform, Palette.Gate, b.Hp > 1 ? 2 : 1);
                    }
                    break;
                case ObjKind.Mine:
                    {
                        var pivot = new GameObject("Spin").transform; pivot.SetParent(transform, false); spin = pivot;
                        var core = Part(PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.3f, Quaternion.Euler(45, 0, 45), Palette.MineFill);
                        core.transform.SetParent(spin, false);
                        var shell = Part(PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.3f, Quaternion.Euler(0, 45, 45), Palette.MineFill);
                        shell.transform.SetParent(spin, false);
                        var heart = Part(PrimitiveType.Cube, Vector3.zero, Vector3.one * 0.12f, Quaternion.identity, Palette.Warn, 2.2f);
                        heart.transform.SetParent(spin, false);
                        Frame(core.transform, Palette.Gate); Frame(shell.transform, Palette.Gate);
                        parts = new[] { core, shell, heart };
                        break;
                    }
                default:
                    {
                        var pivot = new GameObject("Spin").transform; pivot.SetParent(transform, false); spin = pivot;
                        bool ship = Obj.Kind == ObjKind.Ship;
                        var gem = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.22f, 0.22f, 0.06f) * (ship ? 2f : 1f), Quaternion.Euler(0, 0, 45), Palette.Pick, ship ? 3f : 2.2f);
                        gem.transform.SetParent(spin, false);
                        var ring = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(0.36f, 0.36f, 0.02f) * (ship ? 2f : 1f), Quaternion.Euler(0, 0, 45), Color.Lerp(Palette.Pick, Palette.Void, 0.75f));
                        ring.transform.SetParent(spin, false);
                        parts = new[] { gem, ring };
                        break;
                    }
            }
            foreach (var r in GetComponentsInChildren<Renderer>()) r.material.SetColor("_Haze", pal.Haze);
            seen = Obj.Version;
        }

        // A thin bright outline around a slab, so a wall reads as a wireframe box and not a flat tile.
        void Frame(Transform slab, Color col, int rings = 1)
        {
            float t = 0.03f;
            for (int k = 0; k < rings; k++)
            {
                float inset = k * 0.1f;
                foreach (var e in new[] { new Vector3(0, 0.5f - inset, 0), new Vector3(0, -0.5f + inset, 0) })
                {
                    var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bar.GetComponent<Collider>());
                    bar.transform.SetParent(slab, false); bar.transform.localPosition = e; bar.transform.localScale = new Vector3(1 - 2 * inset, t / slab.localScale.y, 1.2f);
                    Tint(bar, col);
                }
                foreach (var e in new[] { new Vector3(0.5f - inset, 0, 0), new Vector3(-0.5f + inset, 0, 0) })
                {
                    var bar = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(bar.GetComponent<Collider>());
                    bar.transform.SetParent(slab, false); bar.transform.localPosition = e; bar.transform.localScale = new Vector3(t / slab.localScale.x, 1 - 2 * inset, 1.2f);
                    Tint(bar, col);
                }
            }
        }
        void Tint(GameObject go, Color col)
        {
            var r = go.GetComponent<Renderer>(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            r.material = new Material(baseMat); r.material.SetColor("_Tint", col); r.material.SetFloat("_Intensity", 1.8f);
        }

        public void Tick(float now, float camZ)
        {
            // Nothing far behind the camera or far beyond the haze needs to move or draw.
            bool near = !Dropped && Obj.Z - camZ > -4 && Obj.Z - camZ < G.Depth + 6;
            if (gameObject.activeSelf != near) gameObject.SetActive(near);
            if (!near) return;
            if (Obj.Version != seen) Refresh();
            switch (Obj.Kind)
            {
                case ObjKind.Mine:
                    transform.position = path.World(Obj.MineX(now), Obj.Y, Obj.Z, Obj.Branch);
                    spin.localRotation = Quaternion.Euler(0, 0, (now * 2 + Obj.Phase) * Mathf.Rad2Deg);
                    break;
                case ObjKind.Wall: case ObjKind.Gate: break;
                default:
                    transform.position = path.World(Obj.X, Obj.Y + Mathf.Sin(now * 3 + Obj.Z) * 0.04f, Obj.Z, Obj.Branch);
                    spin.localRotation = Quaternion.Euler(0, now * 90f, 0);
                    break;
            }
        }

        void Refresh()
        {
            seen = Obj.Version;
            switch (Obj.Kind)
            {
                case ObjKind.Wall: if (Obj.Hit) foreach (var p in parts) p.material.SetColor("_Tint", WallHit); break;
                case ObjKind.Gate:
                    for (int i = 0; i < parts.Length; i++)
                    {
                        var hp = Obj.Blocks[i].Hp;
                        parts[i].gameObject.SetActive(hp > 0);
                        if (hp == 1) parts[i].material.SetColor("_Tint", Palette.GateFill);
                    }
                    break;
                case ObjKind.Mine: if (!Obj.Alive) gameObject.SetActive(false); break;
                default: if (Obj.Taken) gameObject.SetActive(false); break;
            }
        }
    }
}
