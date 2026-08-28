using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Input;
using FrogAcross.Sim;
using FrogAcross.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FrogAcross.View
{
    /// <summary>
    /// Scene driver: loads the level, owns the fixed-step accumulator (sim at
    /// exactly 60Hz regardless of frame rate), forwards dev-keyboard input in
    /// the editor, and renders through BoardView.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public string levelId = "dev-001";
        public BoardView board;

        public GameSim Sim { get; private set; }
        public bool Frozen; // confirm dialogs / OS pause (M4 wires this)

        private float _accumulator;

        private DeathFeedback _deathFx;
        private LevelCompleteOverlay _overlay;
        private TouchInputDriver _driver;

        private void Start()
        {
            Sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            if (board == null) board = gameObject.AddComponent<BoardView>();
            board.Bind(Sim);

            _driver = gameObject.AddComponent<TouchInputDriver>();
            _driver.bootstrap = this;
            _deathFx = gameObject.AddComponent<DeathFeedback>();
            _overlay = gameObject.AddComponent<LevelCompleteOverlay>();

            Sim.OnDeath += cause =>
            {
                var reg = PieceRegistry.Load();
                var ch = reg.defaultCharacter;
                _deathFx.Play(cause,
                    new Vector3(Sim.State.PlayerX, -Sim.State.PlayerRow, 0f),
                    ch.sprites[0]);
            };
            Sim.OnCompleted += () =>
            {
                Frozen = true;
                _overlay.Show(Sim.Level, Sim.State.ClockTicks);
            };
            _overlay.OnReplay += Restart;
            _overlay.OnNext += Restart;      // M4's shell rewires to real progression
            _overlay.OnLevels += Restart;    // M4's shell rewires to the levels screen
        }

        public void Restart()
        {
            _overlay.Hide();
            Frozen = false;
            Sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            board.Bind(Sim);
            if (_driver != null) _driver.bootstrap = this;
            Sim.OnDeath += cause => _deathFx.Play(cause,
                new Vector3(Sim.State.PlayerX, -Sim.State.PlayerRow, 0f),
                PieceRegistry.Load().defaultCharacter.sprites[0]);
            Sim.OnCompleted += () => { Frozen = true; _overlay.Show(Sim.Level, Sim.State.ClockTicks); };
        }

        private void Update()
        {
            if (Sim == null || Frozen) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.upArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Forward);
                if (kb.downArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Back);
                if (kb.leftArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Left);
                if (kb.rightArrowKey.wasPressedThisFrame) Sim.EnqueueMove(Move.Right);
                if (kb.eKey.wasPressedThisFrame) Sim.EnqueueMove(Move.DiagForwardRight);
                if (kb.qKey.wasPressedThisFrame) Sim.EnqueueMove(Move.DiagForwardLeft);
            }
#endif

            _accumulator += Time.deltaTime;
            float step = 1f / SimConfig.TicksPerSecond;
            int safety = 8; // cap catch-up after hitches; sim time slows instead of spiraling
            while (_accumulator >= step && safety-- > 0)
            {
                Sim.Tick();
                _accumulator -= step;
            }
            board.Render(Sim.State.Tick + _accumulator / step);
        }
    }
}
