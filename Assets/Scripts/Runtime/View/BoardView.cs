using System.Collections.Generic;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
using FrogAcross.UI;
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

        // [row][flat instance][wrap copy] — copy 0 is the object's true
        // position, the rest repeat it a whole loop away so a lane never ends
        private readonly List<List<List<SpriteRenderer>>> _objects = new();
        private readonly List<int[]> _trainStart = new();
        private readonly List<SpriteRenderer> _bayFills = new();
        private readonly List<SpriteRenderer> _bayMarks = new();
        private readonly List<SpriteRenderer> _strips = new();
        private readonly List<GameObject> _spawned = new();
        private SpriteRenderer _player;
        private CharacterDef _character;

        // Hop/step animation (view-only: the sim moves the player in one tick).
        private long _hopStartTick = long.MinValue;
        private Vector2 _hopFrom;
        private Vector2 _lastDrawnCell;

        public void Bind(GameSim sim, CharacterDef character = null)
        {
            Sim = sim;
            _character = character != null ? character : PieceRegistry.Load().defaultCharacter;
            BuildStatic();
            _lastDrawnCell = new Vector2(sim.State.PlayerX, -sim.State.PlayerRow);
            _hopStartTick = long.MinValue;
            sim.OnHop += _ =>
            {
                _hopFrom = _lastDrawnCell;      // where the frog was drawn last frame
                _hopStartTick = sim.State.Tick; // the tick the sim teleported it
            };
        }

        private void BuildStatic()
        {
            // Only our own sprites: this component shares its GameObject with
            // the HUD and the completion overlay, and clearing every child
            // deleted them on restart (owner: "buttons and timer disappear if
            // restart level and don't show up again", 2026-08-30).
            foreach (var go in _spawned) if (go != null) Destroy(go);
            _spawned.Clear();
            _objects.Clear();
            _trainStart.Clear();
            _bayFills.Clear();
            _bayMarks.Clear();
            _strips.Clear();

            var level = Sim.Level;
            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];

                var strip = NewSprite($"row-{r}-{row.Kind.id}", SpriteOf(row.Kind, 0), -0.3f);
                strip.drawMode = SpriteDrawMode.Tiled;
                strip.size = new Vector2(StripWidth(level), 1f);
                strip.transform.position = new Vector3((level.Columns - 1) / 2f, -r, 0.3f);
                _strips.Add(strip);

                foreach (var ob in row.Obstructions)
                {
                    var s = NewSprite($"ob-{ob.Def.id}", SpriteOf(ob.Def, 0), -0.05f);
                    // An obstruction blocks exactly one cell, so it is drawn
                    // inside exactly one cell. The raw art is bigger than that
                    // — a planter is 1.28x1.04 cells, a tree 1.24 tall — and
                    // drawing it unscaled put it across the lane line and into
                    // the neighbouring columns (owner: "obstructions sometimes
                    // appear between lanes", 2026-08-30).
                    var b = s.sprite.bounds.size;
                    float fit = CellFit / Mathf.Max(0.01f, Mathf.Max(b.x, b.y));
                    s.transform.localScale = Vector3.one * fit;
                    // stand it on the tile rather than centring it in the air
                    float bottom = -r - 0.5f + 0.03f + b.y * fit / 2f;
                    s.transform.position = new Vector3(ob.Column, bottom, 0.05f - r * 0.001f);
                }

                if (row.Kind.semantics == LaneSemantics.Goal)
                {
                    foreach (int b in level.BayColumns)
                    {
                        // A landing pad reads as a slot to aim at: a dark grey
                        // rounded pad carrying the Frog Across mark, not the
                        // near-black tinted lane tile it used to be (owner,
                        // 2026-08-30).
                        var shadow = NewSprite($"bay-shadow-{b}", UiKit.Rounded(PadRadius), -0.12f);
                        shadow.drawMode = SpriteDrawMode.Sliced;
                        shadow.size = new Vector2(PadSize.x, PadSize.y);
                        shadow.color = PadShadow;
                        shadow.transform.position = new Vector3(b, -r - 0.045f, 0.12f);

                        var bay = NewSprite($"bay-{b}", UiKit.Rounded(PadRadius), -0.1f);
                        bay.drawMode = SpriteDrawMode.Sliced;
                        bay.size = PadSize;
                        bay.color = PadGrey;
                        bay.transform.position = new Vector3(b, -r, 0.1f);

                        var mark = NewSprite($"bay-mark-{b}", UiKit.Logo, -0.08f);
                        if (mark.sprite != null)
                            mark.transform.localScale = Vector3.one
                                * (0.46f / Mathf.Max(0.1f, mark.sprite.bounds.size.x));
                        mark.transform.position = new Vector3(b, -r, 0.08f);
                        _bayMarks.Add(mark);

                        var fill = NewSprite($"bay-fill-{b}", _character.sprites[0], -0.05f);
                        fill.transform.position = new Vector3(b, -r, 0.04f);
                        // cell-fit like the player — raw character sprites are ~4 units wide
                        fill.transform.localScale = Vector3.one
                            * (0.8f / Mathf.Max(0.1f, _character.sprites[0].bounds.size.x));
                        fill.enabled = false;
                        _bayFills.Add(fill);
                    }
                }

                var rends = new List<List<SpriteRenderer>>();
                var starts = new int[row.Trains.Count];
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    starts[t] = rends.Count;
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                        rends.Add(new List<SpriteRenderer>
                        {
                            NewSprite($"obj-{row.Trains[t].Def.id}-{k}", null, -0.02f - r * 0.001f),
                        });
                }
                _objects.Add(rends);
                _trainStart.Add(starts);
            }

            // The design tilts the board (-8°) and lets it bleed past every
            // screen edge. Aprons repeat the plain bank surface above and below
            // so the roll never exposes the background, and the strips run
            // full-bleed sideways (#3, owner device report 2026-08-29).
            for (int a = 1; a <= ApronRows; a++)
            {
                var top = NewSprite($"apron-top-{a}", SpriteOf(level.Rows[level.BankRow].Kind, 0), -0.35f);
                top.drawMode = SpriteDrawMode.Tiled;
                top.size = new Vector2(StripWidth(level), 1f);
                top.transform.position = new Vector3((level.Columns - 1) / 2f, a, 0.35f);

                var bottom = NewSprite($"apron-bottom-{a}", SpriteOf(level.Rows[level.BankRow].Kind, 0), -0.35f);
                bottom.drawMode = SpriteDrawMode.Tiled;
                bottom.size = new Vector2(StripWidth(level), 1f);
                bottom.transform.position = new Vector3((level.Columns - 1) / 2f, -(level.BankRow + a), 0.35f);
            }

            _player = NewSprite("player", _character.sprites[0], -0.5f);
        }

        public void Render(float tickF)
        {
            var level = Sim.Level;
            long tick = Sim.State.Tick;

            (float viewLeft, float viewRight) = VisibleXRange(level);

            for (int r = 0; r < level.Rows.Count; r++)
            {
                var row = level.Rows[r];
                float loop = Sim.LoopCells(r);
                // enough repeats to cover the camera however wide it is
                int copies = Mathf.Clamp(
                    Mathf.CeilToInt((viewRight - viewLeft) / Mathf.Max(1f, loop)) + 1, 1, 9);
                for (int t = 0; t < row.Trains.Count; t++)
                {
                    var train = row.Trains[t];
                    var def = train.Def;
                    for (int k = 0; k < Sim.InstanceCount(r, t); k++)
                    {
                        var slot = _objects[r][_trainStart[r][t] + k];
                        float left = Sim.ObjectLeftX(r, t, k, tick);
                        bool crashed = Sim.State.CrashedAt.ContainsKey(SimState.CrashKey(r, t, k));
                        // a wreck is a one-off at a fixed spot: repeating it
                        // would show phantom copies of a crash that happened once
                        int want = crashed ? 1 : copies;

                        for (int c = 0; c < slot.Count || c < want; c++)
                        {
                            if (c >= want)
                            {
                                if (c < slot.Count && slot[c].enabled) slot[c].enabled = false;
                                continue;
                            }
                            var sr = Copy(slot, r, def, k, c);
                            float x = left + WrapOffset(c) * loop;
                            sr.transform.position = new Vector3(
                                x + def.sizeCells * 0.5f, -r, sr.transform.position.z);

                            bool visible = x + def.sizeCells > viewLeft && x < viewRight;
                            if (sr.enabled != visible) sr.enabled = visible;
                            if (!visible) continue;

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
                bool filled = Sim.State.BaysFilled.Contains(b);
                _bayFills[bi].enabled = filled;
                if (bi < _bayMarks.Count) _bayMarks[bi].enabled = !filled;
                bi++;
            }

            // player
            var s = Sim.State;
            _player.sprite = _character.sprites[SpriteSelector.CharacterIndex(s.Facing)];
            var cell = new Vector2(s.PlayerX, -s.PlayerRow);
            _lastDrawnCell = cell;

            // The sim moves in a single tick; the view plays the character's
            // own move style across the hop cooldown so a frog hops and a
            // runner runs (owner: "character should use its designated move",
            // 2026-08-30). Purely cosmetic — the sim is already at the target.
            float lift = 0f, squash = 1f;
            float phase = (tickF - _hopStartTick) / SimConfig.HopCooldownTicks;
            if (_hopStartTick != long.MinValue && phase > 0f && phase < 1f && !s.Riding)
            {
                cell = Vector2.Lerp(_hopFrom, cell, Mathf.SmoothStep(0f, 1f, phase));
                (lift, squash) = SpriteSelector.MoveArc(_character.moveStyle, phase);
            }
            _player.transform.position = new Vector3(cell.x, cell.y + lift, -0.5f);
            float ps = 0.9f / Mathf.Max(0.1f, _player.sprite.bounds.size.x);
            _player.transform.localScale = new Vector3(ps / squash, ps * squash, 1f);
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

        /// <summary>How much of a cell an obstruction may occupy.</summary>
        public const float CellFit = 0.94f;

        private const int PadRadius = 14;
        private static readonly Vector2 PadSize = new Vector2(0.88f, 0.66f);
        private static readonly Color PadGrey = new Color(0.302f, 0.329f, 0.365f);
        private static readonly Color PadShadow = new Color(0.086f, 0.098f, 0.114f);

        /// <summary>
        /// World-X span the camera can actually see, including the board roll.
        /// Objects are drawn across this, not across the board — the camera is
        /// far wider than the board, so culling at the board margin let cars
        /// appear and vanish in open view (owner: "cars spawn part way on the
        /// screen. This should never happen, ever", 2026-08-30).
        /// </summary>
        private (float left, float right) VisibleXRange(LevelDefinition level)
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
                return (-MarginCells(level), level.Columns + MarginCells(level));
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float roll = cam.transform.eulerAngles.z * Mathf.Deg2Rad;
            float ext = Mathf.Abs(halfW * Mathf.Cos(roll)) + Mathf.Abs(halfH * Mathf.Sin(roll));
            float cx = cam.transform.position.x;
            return (cx - ext - 1f, cx + ext + 1f);
        }

        /// <summary>Wrap copy c sits this many loops from the real object: 0, -1, +1, -2, +2…</summary>
        private static int WrapOffset(int c) => (c + 1) / 2 * (c % 2 == 1 ? -1 : 1);

        private SpriteRenderer Copy(List<SpriteRenderer> slot, int row, LaneObjectDef def, int instance, int c)
        {
            while (slot.Count <= c)
                slot.Add(NewSprite($"obj-{def.id}-{instance}-w{slot.Count}", null, -0.02f - row * 0.001f));
            return slot[c];
        }

        private Sprite SpriteOf(PieceDef def, int i) =>
            def.sprites != null && def.sprites.Length > i ? def.sprites[i] : null;

        /// <summary>Rows of repeated goal/bank surface drawn beyond the board.</summary>
        public const int ApronRows = 4;

        /// <summary>How far past the board objects and surfaces are drawn.</summary>
        public static float MarginCells(LevelDefinition level)
        {
            float maxSize = 1f;
            foreach (var row in level.Rows)
                foreach (var train in row.Trains)
                    maxSize = Mathf.Max(maxSize, train.Def.sizeCells);
            return LaneGeometry.MarginFor(maxSize);
        }

        public static float StripWidth(LevelDefinition level) =>
            level.Columns + 2f * MarginCells(level) + 6f;

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
            _spawned.Add(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            var pos = go.transform.position;
            go.transform.position = new Vector3(pos.x, pos.y, z);
            return sr;
        }
    }
}
