using System.Collections.Generic;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using UnityEngine;

namespace FrogAcross.View
{
    /// <summary>
    /// Renders the board FROM sim state with the design sprite library — the
    /// view holds no gameplay truth and runs no clocks of its own (all animated
    /// states derive from sim tick math via SpriteSelector).
    /// World: 1 unit = 1 cell; row r at z = -r; camera rig supplies the tilt.
    /// Piece behavior/layout flows from def DATA (role, cycles, sprite-array
    /// shape) — never piece-id switching.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public GameSim Sim { get; private set; }
        public CharacterDef Character => _character;

        private readonly List<SpriteRenderer[]> _objects = new();
        private readonly List<int[]> _trainStart = new();
        private readonly List<SpriteRenderer> _bayFills = new();
        private readonly List<SpriteRenderer> _strips = new();
        private SpriteRenderer _player;
        private CharacterDef _character;

        public void Bind(GameSim sim, CharacterDef character = null)
        {
            Sim = sim;
            _character = character != null ? character : PieceRegistry.Load().defaultCharacter;
            BuildStatic();
        }

        private void BuildStatic()
        {
            foreach (Transform child in transform) Destroy(child.gameObject);
            _objects.Clear();
            _trainStart.Clear();
            _bayFills.Clear();
            _strips.Clear();

            var level = Sim.Level;
            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];

                var strip = NewSprite($"row-{r}-{row.Kind.id}", SpriteOf(row.Kind, 0), -0.3f);
                strip.drawMode = SpriteDrawMode.Tiled;
                strip.size = new Vector2(level.Columns + 2f, 1f);
                strip.transform.position = new Vector3((level.Columns - 1) / 2f, -r, 0.3f);
                _strips.Add(strip);

                foreach (var ob in row.Obstructions)
                {
                    var s = NewSprite($"ob-{ob.Def.id}", SpriteOf(ob.Def, 0), -0.05f);
                    // props sit on the tile, overhanging upward like the design
                    float h = s.sprite.bounds.size.y;
                    s.transform.position = new Vector3(ob.Column, -r + (h - 1f) / 2f, 0.05f - r * 0.001f);
                }

                if (row.Kind.semantics == LaneSemantics.Goal)
                {
                    foreach (int b in level.BayColumns)
                    {
                        var bay = NewSprite($"bay-{b}", null, -0.1f);
                        bay.sprite = SpriteOf(row.Kind, 0);
                        bay.drawMode = SpriteDrawMode.Tiled;
                        bay.size = new Vector2(0.95f, 0.75f);
                        bay.color = new Color(0.04f, 0.19f, 0.10f);
                        bay.transform.position = new Vector3(b, -r, 0.1f);

                        var fill = NewSprite($"bay-fill-{b}", _character.sprites[0], -0.05f);
                        fill.transform.position = new Vector3(b, -r, 0.04f);
                        // cell-fit like the player — raw character sprites are ~4 units wide
                        fill.transform.localScale = Vector3.one
                            * (0.8f / Mathf.Max(0.1f, _character.sprites[0].bounds.size.x));
                        fill.enabled = false;
                        _bayFills.Add(fill);
                    }
                }

                var rends = new List<SpriteRenderer>();
                var starts = new int[row.Trains.Count];
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    starts[t] = rends.Count;
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                        rends.Add(NewSprite($"obj-{row.Trains[t].Def.id}-{k}", null, -0.02f - r * 0.001f));
                }
                _objects.Add(rends.ToArray());
                _trainStart.Add(starts);
            }

            // Side covers: objects wrap in the off-board margins — the design
            // hides them beyond the board edges.
            foreach (int side in new[] { -1, +1 })
            {
                // flat surround (design: solid #16321F), wide enough for any
                // phone aspect — the tiled bank texture used to band through
                var cover = NewSprite($"cover-{side}", SolidSprite(), -0.55f);
                cover.color = new Color(0.086f, 0.196f, 0.121f); // #16321F surround
                cover.transform.localScale = new Vector3(40f, level.Rows.Count + 8f, 1f);
                float cx = side < 0 ? -0.5f - 20f : level.Columns - 0.5f + 20f;
                cover.transform.position = new Vector3(cx, -(level.Rows.Count - 1) / 2f, -0.55f);
            }

            _player = NewSprite("player", _character.sprites[0], -0.5f);
        }

        public void Render(float tickF)
        {
            var level = Sim.Level;
            long tick = Sim.State.Tick;

            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    var train = row.Trains[t];
                    var def = train.Def;
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                    {
                        var sr = _objects[r][_trainStart[r][t] + k];
                        float left = Sim.ObjectLeftX(r, t, k, tick);
                        sr.transform.position = new Vector3(
                            left + def.sizeCells * 0.5f, -r, sr.transform.position.z);

                        bool visible = left + def.sizeCells > -1f && left < level.Columns + 1f;
                        if (sr.enabled != visible) sr.enabled = visible;
                        if (!visible) continue;

                        bool crashed = Sim.State.CrashedAt.ContainsKey(SimState.CrashKey(r, t, k));
                        sr.sprite = SelectSprite(def, train, row.DirSign, r, t, k, tick, crashed);
                        var color = Color.white;
                        if (def.cycleActiveTicks > 0 && !def.inactiveKills)
                            color.a = SpriteSelector.TurtleAlpha(def, train, tick);
                        sr.color = color;

                        // fit sprite to the def's cell size
                        float sw = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
                        float scale = sw > 0f ? def.sizeCells / sw : 1f;
                        sr.transform.localScale = Vector3.one * scale;
                    }
                }
            }

            // train warnings: the crossing pulses from the SAME schedule that runs the train
            for (int r = 0; r < level.Rows.Count; r++)
            {
                bool warnable = false;
                foreach (var tr in level.Rows[r].Trains)
                    if (tr.Def.warnLeadTicks > 0) { warnable = true; break; }
                if (!warnable) continue;
                bool warn = Sim.WarningActive(r) && (tick / 8) % 2 == 0;
                _strips[r].color = warn ? new Color(1f, 0.55f, 0.5f) : Color.white;
            }

            // bays
            int bi = 0;
            foreach (int b in level.BayColumns)
            {
                _bayFills[bi].enabled = Sim.State.BaysFilled.Contains(b);
                bi++;
            }

            // player
            var s = Sim.State;
            _player.sprite = _character.sprites[SpriteSelector.CharacterIndex(s.Facing)];
            _player.transform.position = new Vector3(s.PlayerX, -s.PlayerRow, -0.5f);
            float ps = 0.9f / Mathf.Max(0.1f, _player.sprite.bounds.size.x);
            _player.transform.localScale = Vector3.one * ps;
            _player.enabled = s.RespawnDelay == 0;
            _player.color = s.StunTicksLeft > 0 && (tick / 6) % 2 == 0
                ? new Color(1f, 1f, 1f, 0.5f)   // stun feedback: flicker
                : Color.white;
        }

        private Sprite SelectSprite(LaneObjectDef def, ObjectTrain train, int dirSign,
            int row, int trainIdx, int instance, long tick, bool crashed)
        {
            var sprites = def.sprites;
            if (sprites == null || sprites.Length == 0) return null;

            if (crashed && def.crashedSprites != null && def.crashedSprites.Length > 0)
            {
                int livC = def.crashedSprites.Length / 2;
                int liv = SpriteSelector.LiveryFor(row, trainIdx, instance, livC);
                return def.crashedSprites[(dirSign < 0 ? livC : 0) + liv];
            }

            switch (def.role)
            {
                case ObjectRole.Crashable:
                {
                    int livC = sprites.Length / (2 * SpriteSelector.RiderFrames);
                    int liv = SpriteSelector.LiveryFor(row, trainIdx, instance, livC);
                    return sprites[SpriteSelector.RiderIndex(tick, dirSign, liv, livC)];
                }
                case ObjectRole.Rideable when def.inactiveKills && sprites.Length >= 4:
                    return sprites[SpriteSelector.GatorIndex(def, train, tick, dirSign)];
                case ObjectRole.Rideable:
                    return sprites.Length >= 2 ? sprites[SpriteSelector.TrainOrLogIndex(dirSign)] : sprites[0];
                case ObjectRole.Kill when sprites.Length == 2:
                    return sprites[SpriteSelector.TrainOrLogIndex(dirSign)];
                case ObjectRole.Kill:
                {
                    int livC = sprites.Length / 2;
                    int liv = SpriteSelector.LiveryFor(row, trainIdx, instance, livC);
                    return sprites[SpriteSelector.VehicleIndex(def, dirSign, liv, livC)];
                }
                default:
                    return sprites[0];
            }
        }

        private Sprite SpriteOf(PieceDef def, int i) =>
            def.sprites != null && def.sprites.Length > i ? def.sprites[i] : null;

        private static Sprite _solid;

        private static Sprite SolidSprite()
        {
            if (_solid == null)
                _solid = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _solid;
        }

        private SpriteRenderer NewSprite(string name, Sprite sprite, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var pos = go.transform.position;
            go.transform.position = new Vector3(pos.x, pos.y, z);
            return sr;
        }
    }
}
