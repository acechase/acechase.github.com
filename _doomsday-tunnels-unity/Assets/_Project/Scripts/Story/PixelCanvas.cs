using System.Collections.Generic;
using UnityEngine;

namespace DoomsdayTunnels.Story
{
    /// <summary>
    /// A tiny software canvas, 320 pixels wide, with the handful of 2D-canvas operations the cut scenes are painted
    /// with: rectangles, ellipses, polygons, lines, arcs, a circular clip, global alpha and a translate/scale
    /// transform. Pixels go straight into a Color32 buffer; Upload copies it to a point-filtered texture, which the
    /// HUD blows up without smoothing for the chunky late-80s look. Coordinates are canvas coordinates: origin
    /// top-left, y down, the same as the web game's.
    /// </summary>
    public sealed class PixelCanvas
    {
        public const int VW = 320;
        public const float TAU = 6.2831853f;
        public int H { get; private set; }
        public Color32[] Buffer { get; private set; }
        public Texture2D Texture { get; private set; }
        public float Alpha = 1f;

        float tx, ty, sx = 1, sy = 1;                 // current transform: device = t + canvas * s
        readonly Stack<(float, float, float, float)> saved = new Stack<(float, float, float, float)>();
        bool clip; float ccx, ccy, cr2, cyMax;        // a disc, optionally cut off below its centre line
        readonly List<float> xs = new List<float>(); float[] q = new float[64];

        public PixelCanvas(int h) { Resize(h); }

        public void Resize(int h)
        {
            if (Buffer != null && h == H) return;
            H = h; Buffer = new Color32[VW * h];
            if (Texture != null) { if (Application.isPlaying) Object.Destroy(Texture); else Object.DestroyImmediate(Texture); }
            Texture = new Texture2D(VW, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Scene canvas" };
        }
        public void Upload() { Texture.SetPixels32(Buffer); Texture.Apply(false, false); }

        public void Save() => saved.Push((tx, ty, sx, sy));
        public void Restore() { (tx, ty, sx, sy) = saved.Pop(); }
        public void Translate(float x, float y) { tx += x * sx; ty += y * sy; }
        public void Scale(float x, float y) { sx *= x; sy *= y; }
        float X(float x) => tx + x * sx;
        float Y(float y) => ty + y * sy;
        static int R(float v) => (int)Mathf.Floor(v + 0.5f);      // JavaScript's Math.round

        public void ClipCircle(float cx, float cy, float r, bool upperHalf = false)
        {
            clip = true; ccx = X(cx); ccy = Y(cy); float rr = r * Mathf.Abs(sx); cr2 = rr * rr; cyMax = upperHalf ? ccy : float.MaxValue;
        }
        public void ClipNone() => clip = false;

        void Plot(int x, int y, Color32 c)
        {
            if ((uint)x >= VW || (uint)y >= (uint)H) return;
            if (clip) { float dx = x + 0.5f - ccx, dy = y + 0.5f - ccy; if (dx * dx + dy * dy > cr2 || y + 0.5f > cyMax) return; }
            int i = (H - 1 - y) * VW + x;                           // texture rows run bottom-up
            if (Alpha >= 1f) { Buffer[i] = c; return; }
            var d = Buffer[i]; float a = Alpha;
            Buffer[i] = new Color32((byte)(d.r + (c.r - d.r) * a), (byte)(d.g + (c.g - d.g) * a), (byte)(d.b + (c.b - d.b) * a), 255);
        }
        void Span(int x0, int x1, int y, Color32 c) { if (x0 < 0) x0 = 0; if (x1 > VW) x1 = VW; for (int x = x0; x < x1; x++) Plot(x, y, c); }

        /// <summary>Filled rectangle; edges round to whole pixels, as the web game's do.</summary>
        public void Bx(float x, float y, float w, float h, Color32 c)
        {
            float x0 = X(x), y0 = Y(y), x1 = X(x + w), y1 = Y(y + h);
            if (x1 < x0) (x0, x1) = (x1, x0); if (y1 < y0) (y0, y1) = (y1, y0);
            int ix = R(x0), iy = R(y0), iw = R(x1 - x0), ih = R(y1 - y0);
            for (int yy = iy; yy < iy + ih; yy++) if ((uint)yy < (uint)H) Span(ix, ix + iw, yy, c);
        }

        /// <summary>Filled ellipse.</summary>
        public void Ov(float cx, float cy, float rx, float ry, Color32 c)
        {
            float ex = X(cx), ey = Y(cy), rxx = rx * Mathf.Abs(sx), ryy = ry * Mathf.Abs(sy);
            if (rxx <= 0 || ryy <= 0) return;
            int y0 = Mathf.FloorToInt(ey - ryy), y1 = Mathf.CeilToInt(ey + ryy);
            for (int y = y0; y <= y1; y++)
            {
                float dy = (y + 0.5f - ey) / ryy; if (dy <= -1 || dy >= 1) continue;
                float hw = rxx * Mathf.Sqrt(1 - dy * dy); Span(R(ex - hw), R(ex + hw), y, c);
            }
        }

        /// <summary>Filled polygon from a flat list of x, y pairs (even-odd rule).</summary>
        public void Pg(Color32 c, params float[] p)
        {
            int n = p.Length / 2; if (n < 3) return;
            if (q.Length < p.Length) q = new float[p.Length * 2];
            for (int i = 0; i < n; i++) { q[2 * i] = X(p[2 * i]); q[2 * i + 1] = Y(p[2 * i + 1]); }
            PgDevice(c, q, n);
        }
        void PgDevice(Color32 c, float[] d, int n)
        {
            float ymin = float.MaxValue, ymax = float.MinValue;
            for (int i = 0; i < n; i++) { float y = d[2 * i + 1]; if (y < ymin) ymin = y; if (y > ymax) ymax = y; }
            int y0 = Mathf.Max(0, Mathf.FloorToInt(ymin)), y1 = Mathf.Min(H - 1, Mathf.CeilToInt(ymax));
            for (int y = y0; y <= y1; y++)
            {
                float yc = y + 0.5f; xs.Clear();
                for (int i = 0, j = n - 1; i < n; j = i++)
                {
                    float ax = d[2 * i], ay = d[2 * i + 1], bx = d[2 * j], by = d[2 * j + 1];
                    if ((ay <= yc) != (by <= yc)) xs.Add(ax + (yc - ay) * (bx - ax) / (by - ay));
                }
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2) Span(R(xs[k]), R(xs[k + 1]), y, c);
            }
        }

        /// <summary>A line of the given width: one pixel wide walks the pixels, wider ones are a thin filled quad.</summary>
        public void Li(float x1, float y1, float x2, float y2, Color32 c, float w = 1)
        {
            float ax = X(x1), ay = Y(y1), bx = X(x2), by = Y(y2), ww = w * Mathf.Abs(sx);
            float dx = bx - ax, dy = by - ay;
            if (ww <= 1.3f)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy))));
                for (int i = 0; i <= steps; i++) { float t = (float)i / steps; Plot(Mathf.FloorToInt(ax + dx * t), Mathf.FloorToInt(ay + dy * t), c); }
                return;
            }
            float len = Mathf.Sqrt(dx * dx + dy * dy); if (len < 1e-4f) return;
            float nx = -dy / len * ww / 2, ny = dx / len * ww / 2;
            var d = new[] { ax + nx, ay + ny, bx + nx, by + ny, bx - nx, by - ny, ax - nx, ay - ny };
            PgDevice(c, d, 4);
        }

        /// <summary>Stroked arc from angle a0 to a1 (radians, clockwise on screen, as the 2D canvas counts them).</summary>
        public void Arc(float cx, float cy, float r, float a0, float a1, Color32 c, float w = 1)
        {
            if (a1 < a0) a1 += TAU;
            int n = Mathf.Max(8, Mathf.CeilToInt(r * Mathf.Abs(sx) * (a1 - a0)));
            float px = cx + Mathf.Cos(a0) * r, py = cy + Mathf.Sin(a0) * r;
            for (int i = 1; i <= n; i++) { float a = a0 + (a1 - a0) * i / n, qx = cx + Mathf.Cos(a) * r, qy = cy + Mathf.Sin(a) * r; Li(px, py, qx, qy, c, w); px = qx; py = qy; }
        }
        /// <summary>Filled pie slice between two angles, closed by its chord.</summary>
        public void FillArc(float cx, float cy, float r, float a0, float a1, Color32 c)
        {
            if (a1 < a0) a1 += TAU;
            int n = Mathf.Max(8, Mathf.CeilToInt(r * (a1 - a0))); var p = new float[(n + 1) * 2];
            for (int i = 0; i <= n; i++) { float a = a0 + (a1 - a0) * i / n; p[2 * i] = cx + Mathf.Cos(a) * r; p[2 * i + 1] = cy + Mathf.Sin(a) * r; }
            Pg(c, p);
        }
        /// <summary>Stroked quadratic curve from (x0, y0) to (x1, y1) with control point (qx, qy).</summary>
        public void QuadCurve(float x0, float y0, float qx, float qy, float x1, float y1, Color32 c, float w)
        {
            const int n = 12; float px = x0, py = y0;
            for (int i = 1; i <= n; i++)
            {
                float t = (float)i / n, u = 1 - t, x = u * u * x0 + 2 * u * t * qx + t * t * x1, y = u * u * y0 + 2 * u * t * qy + t * t * y1;
                Li(px, py, x, y, c, w); px = x; py = y;
            }
        }
    }
}
