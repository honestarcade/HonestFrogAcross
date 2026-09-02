using FrogAcross.Input;
using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Services;
using FrogAcross.Sim;
using FrogAcross.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FrogAcross.View
{
    /// <summary>
    /// Game-scene driver: level from AppShell.PendingLevelId (or the inspector
    /// default), selected character, fixed-step sim, HUD, death FX, overlay
    /// with REAL progression, Android-back confirm-quit (freezes the sim).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public string levelId = "dev-full";
        public BoardView board;

        public GameSim Sim { get; private set; }
        public bool Frozen;

        private DeathFeedback _deathFx;
        private LevelCompleteOverlay _overlay;
        private TouchInputDriver _driver;
        private GameHud _hud;
        private CharacterDef _character;
        private float _accumulator;
        private bool _quitDialogOpen;

        private void Start()
        {
            if (!string.IsNullOrEmpty(AppShell.PendingLevelId))
            {
                levelId = AppShell.PendingLevelId;
                AppShell.PendingLevelId = null;
            }

            var reg = PieceRegistry.Load();
            _character = reg.defaultCharacter;
            if (!string.IsNullOrEmpty(CharacterSelection.SelectedId)
                && reg.TryGet<CharacterDef>(CharacterSelection.SelectedId, out var chosen))
                _character = chosen;

            if (board == null) board = gameObject.AddComponent<BoardView>();
            _driver = gameObject.AddComponent<TouchInputDriver>();
            _driver.bootstrap = this;
            _deathFx = gameObject.AddComponent<DeathFeedback>();
            _overlay = gameObject.AddComponent<LevelCompleteOverlay>();
            _overlay.OnReplay += StartLevel;
            _overlay.OnNext += NextLevel;
            _overlay.OnMainMenu += QuitToMenu;

            _hud = gameObject.AddComponent<GameHud>();
            _hud.FreezeGate = () => !Frozen && !Sim.State.Completed;
            _hud.SetFrozen = f => Frozen = f;
            _hud.OnRestartConfirmed += StartLevel;
            _hud.OnQuitConfirmed += QuitToMenu;

            StartLevel();
        }

        public void StartLevel()
        {
            _overlay.Hide();
            Frozen = false;
            Sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            board.Bind(Sim, _character);
            FitCamera();
            _hud.Build(Sim.Level.GoldSeconds); // per level: gold target, and it
                                              // must survive a board rebuild
            FrogAcross.Audio.AudioDirector.Instance.Bind(Sim, Sim.Level);
            FrogAcross.Audio.AudioDirector.Instance.PlayMusic(FrogAcross.Audio.MusicSlot.Gameplay);
            Sim.OnDeath += cause => _deathFx.Play(cause,
                new Vector3(Sim.State.PlayerX, -Sim.State.PlayerRow, 0f), _character.sprites[0]);
            Sim.OnCompleted += HandleCompleted;
        }

        private void HandleCompleted()
        {
            Frozen = true;
            int number = LevelCatalog.NumberFor(levelId);
            float seconds = Sim.State.ClockTicks / (float)SimConfig.TicksPerSecond;
            bool newBest = false;
            float prevBest = -1f;
            if (number > 0)
            {
                var prev = Progression.RecordFor(levelId);
                prevBest = prev != null ? prev.bestSeconds : -1f;
                (_, newBest) = Progression.ReportCompletion(levelId, number, seconds,
                    Sim.Level.GoldSeconds, Sim.Level.SilverSeconds, Sim.Level.BronzeSeconds);
            }
            _overlay.Show(Sim.Level, Sim.State.ClockTicks, newBest, prevBest, number);
        }

        private void NextLevel()
        {
            int number = LevelCatalog.NumberFor(levelId);
            string next = LevelCatalog.IdFor(number + 1);
            if (number > 0 && next != null && Progression.IsUnlocked(number + 1))
            {
                levelId = next;
                StartLevel();
            }
            else
            {
                QuitToMenu();
            }
        }

        private void QuitToMenu()
        {
            AppShell.SkipBoot = true; // returning must not replay the boot beat
            SceneManager.LoadScene("Shell");
        }

        /// <summary>
        /// Frames the bound level the way the design draws it: rolled -8° and
        /// overscanned so the board bleeds past every edge (the design's board
        /// is 1260×660 inside a 958×450 screen). No level-dependent letterbox.
        /// </summary>
        public const float BoardRollDegrees = -8f;

        public void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            int rows = Sim.Level.Rows.Count;
            float cx = (Sim.Level.Columns - 1) / 2f;
            float cy = -(rows - 1) / 2f;
            cam.transform.SetPositionAndRotation(new Vector3(cx, cy, -10f),
                Quaternion.Euler(0f, 0f, BoardRollDegrees));
            cam.orthographic = true;

            // Every board corner must land inside the ROLLED frame. Rotating a
            // corner (±cols/2, ±rows/2) by -θ gives |y| = cols/2·sinθ +
            // rows/2·cosθ — that, plus a margin, is the half-height we need.
            // (Fitting rows alone crops the goal and bank once the roll tips
            // the corners in.)
            float roll = Mathf.Abs(BoardRollDegrees) * Mathf.Deg2Rad;
            float halfHeight = Sim.Level.Columns / 2f * Mathf.Sin(roll)
                + rows / 2f * Mathf.Cos(roll) + 0.35f;

            // Boards are sized to fill a 21:9 panel, so on a narrower phone the
            // width is the binding constraint — fit it too, or the edge columns
            // fall off the screen. Zooming out here shows more apron, which is
            // exactly what the apron rows are for.
            float halfWidth = Sim.Level.Columns / 2f * Mathf.Cos(roll)
                + rows / 2f * Mathf.Sin(roll) + 0.35f;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / Mathf.Max(0.1f, cam.aspect));
        }

        private void Update()
        {
            if (Sim == null) return;

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame
                && !Frozen && !Sim.State.Completed && !_quitDialogOpen)
            {
                _quitDialogOpen = true;
                Frozen = true; // dialogs freeze the sim (owner amendment)
                ConfirmDialog.Show(transform, "Quit to menu?", Copy.Get("quitConfirm"), "Quit",
                    () => { _quitDialogOpen = false; QuitToMenu(); },
                    () => { _quitDialogOpen = false; Frozen = false; });
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (kb != null && !Frozen)
            {
                if (kb.upArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Forward);
                if (kb.downArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Back);
                if (kb.leftArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Left);
                if (kb.rightArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Right);
                if (kb.eKey.wasPressedThisFrame) Sim.EnqueueMove(Move.DiagForwardRight);
                if (kb.qKey.wasPressedThisFrame) Sim.EnqueueMove(Move.DiagForwardLeft);
            }
#endif

            if (Frozen) return;
            _accumulator += Time.deltaTime;
            float step = 1f / SimConfig.TicksPerSecond;
            int safety = 8;
            while (_accumulator >= step && safety-- > 0)
            {
                Sim.Tick();
                _accumulator -= step;
            }
            board.Render(Sim.State.Tick + _accumulator / step);
            _hud.Tick(Sim);
        }
    }
}
