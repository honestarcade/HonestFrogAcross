using FrogAcross.Levels;
using FrogAcross.Pieces;
using FrogAcross.Sim;
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

        private void Start()
        {
            Sim = new GameSim(LevelLoader.LoadFromResources(levelId, PieceRegistry.Load()));
            if (board == null) board = gameObject.AddComponent<BoardView>();
            board.Bind(Sim);
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
