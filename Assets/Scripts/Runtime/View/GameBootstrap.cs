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
            _overlay.OnLevels += QuitToMenu;

            _hud = gameObject.AddComponent<GameHud>();
            _hud.FreezeGate = () => !Frozen && !Sim.State.Completed;
            _hud.SetFrozen = f => Frozen = f;
            _hud.OnRestartConfirmed += StartLevel;
            _hud.OnQuitConfirmed += QuitToMenu;

            StartLevel();
            _hud.Build(Sim.Level.GoldSeconds);
        }

        public void StartLevel()
        {
            _overlay.Hide();
            Frozen = false;
            Sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            board.Bind(Sim, _character);
            FitCamera();
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

        private void QuitToMenu() => SceneManager.LoadScene("Shell");

        /// <summary>
        /// #87: frame the bound level — board centered, filling the screen
        /// height (side covers absorb wide aspects), no roll. Levels vary from
        /// 5 to 12 rows, so a fixed scene camera cannot fit them all.
        /// </summary>
        private void FitCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            int rows = Sim.Level.Rows.Count;
            float cx = (Sim.Level.Columns - 1) / 2f;
            float cy = -(rows - 1) / 2f;
            cam.transform.SetPositionAndRotation(new Vector3(cx, cy, -10f), Quaternion.identity);
            cam.orthographic = true;
            cam.orthographicSize = rows / 2f + 0.4f;
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
