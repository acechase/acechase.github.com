using System;
using UnityEngine;

namespace DoomsdayTunnels.Story
{
    /// <summary>
    /// Plays a cut scene: which shot and line is up, how much of the line has been typed out, and what a tap does
    /// (finish the sentence, then the next line, the next shot, and finally hand back to the game).
    /// </summary>
    public sealed class ScenePlayer
    {
        public const float CPS = 40f;                 // characters typed per second
        public Shot[] Shots; public int I, LineNo;
        public float T, Lt, Elapsed;                  // time in the shot, in the line, and since the scene began
        public Action Done;
        /// <summary>Plays a named sound: static on every cut, the shot's jingle, and a line's own effect.</summary>
        public Action<string> Sound;

        public bool Active => Shots != null;
        public Shot Shot => Shots[I];
        public Line Cur => Shots[I].Lines[LineNo];
        public bool Typing => Lt * CPS < Cur.Text.Length;
        public int Shown => Mathf.Min(Cur.Text.Length, Mathf.FloorToInt(Lt * CPS));

        public void Play(Shot[] shots, Action done) { Shots = shots; I = 0; Done = done; Elapsed = 0; ShotEnter(); }
        void LineEnter() { Lt = 0; if (Cur.Fx != null) Sound?.Invoke(Cur.Fx); }
        void ShotEnter() { T = 0; LineNo = 0; Sound?.Invoke("static"); if (Shot.Jingle != null) Sound?.Invoke(Shot.Jingle); LineEnter(); }
        public void End() { var d = Done; Shots = null; Done = null; d?.Invoke(); }

        public void Advance()
        {
            if (Shots == null) return;
            if (Typing) { Lt = 1e4f; return; }        // first tap finishes the sentence
            if (LineNo < Shot.Lines.Length - 1) { LineNo++; LineEnter(); }
            else if (I < Shots.Length - 1) { I++; ShotEnter(); }
            else End();
        }

        public void Update(float dt) { if (Shots == null) return; T += dt; Lt += dt; Elapsed += dt; }
    }
}
