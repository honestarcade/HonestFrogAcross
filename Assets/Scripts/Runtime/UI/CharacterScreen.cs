using System.Linq;
using FrogAcross.Pieces;
using FrogAcross.Services;
using UnityEngine;
using UnityEngine.UI;

namespace FrogAcross.UI
{
    /// <summary>#57: six characters, same speed different move; choice persists and plays.</summary>
    public static class CharacterScreen
    {
        public static GameObject Build(Transform parent, AppShell shell)
        {
            var root = new GameObject("character");
            root.transform.SetParent(parent, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero; rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;


            var reg = PieceRegistry.Load();
            var chars = reg.All<CharacterDef>().ToList();
            string selected = CharacterSelection.SelectedId;
            if (string.IsNullOrEmpty(selected)) selected = reg.defaultCharacter.id;

            for (int i = 0; i < chars.Count; i++)
            {
                var def = chars[i];
                bool isSelected = def.id == selected;
                var cell = UiKit.Panel(root.transform, $"char-{def.id}",
                    new Color(1f, 1f, 1f, isSelected ? 0.12f : 0.05f));
                cell.rectTransform.sizeDelta = new Vector2(290, 680);
                cell.rectTransform.anchoredPosition = new Vector2(-762 + i * 305, -150);

                var spriteGo = new GameObject("preview");
                spriteGo.transform.SetParent(cell.transform, false);
                var img = spriteGo.AddComponent<Image>();
                // #90: previews face the player — the back-view frame
                int face = FrogAcross.View.SpriteSelector.CharacterIndex(FrogAcross.Sim.Move.Back);
                img.sprite = def.sprites != null && def.sprites.Length > face ? def.sprites[face]
                    : def.sprites is { Length: > 0 } ? def.sprites[0] : null;
                img.preserveAspect = true;
                var srt = img.rectTransform;
                srt.sizeDelta = new Vector2(215, 215);
                srt.anchoredPosition = new Vector2(0, 120);
                var bob = spriteGo.AddComponent<IdleBob>();
                bob.hop = def.moveStyle == MoveStyle.Hop;

                string shown = string.IsNullOrEmpty(def.displayName) ? def.id
                    : char.ToUpperInvariant(def.displayName[0]) + def.displayName.Substring(1);
                var nameL = UiKit.Label(cell.transform, shown, 34, UiKit.White, new Vector2(0, -75));
                nameL.fontStyle = FontStyle.Bold;
                UiKit.Label(cell.transform, def.moveStyle == MoveStyle.Hop ? "HOP" : "STEP", 22,
                    def.moveStyle == MoveStyle.Hop ? UiKit.Mint : UiKit.Hex("B48CFF"), new Vector2(0, -125));
                if (isSelected)
                    UiKit.Label(cell.transform, "✓", 38, UiKit.Mint, new Vector2(108, 295));

                var btn = cell.gameObject.AddComponent<Button>();
                string id = def.id;
                btn.onClick.AddListener(() =>
                {
                    CharacterSelection.SelectedId = id;
                    shell.RebuildScreen("character", Build);
                    shell.Push("character");
                });
            }
            // header last: the cells used to be drawn over the back button,
            // which is why it stopped responding (owner report, 2026-08-29)
            UiKit.Header(root.transform, "Character", shell.Back, "SAME SPEED. DIFFERENT MOVE.");
            return root;
        }
    }

    /// <summary>Idle preview: hoppers bounce, steppers sway (visual only).</summary>
    public sealed class IdleBob : MonoBehaviour
    {
        public bool hop;
        private RectTransform _rt;
        private Vector2 _basePos;

        private void Start()
        {
            _rt = (RectTransform)transform;
            _basePos = _rt.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.time;
            if (hop)
            {
                float phase = Mathf.Repeat(t, 1.4f) / 1.4f;
                float y = phase > 0.6f ? Mathf.Sin((phase - 0.6f) / 0.4f * Mathf.PI) * 26f : 0f;
                _rt.anchoredPosition = _basePos + new Vector2(0f, y);
            }
            else
            {
                _rt.anchoredPosition = _basePos + new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 3.4f)) * 7f);
                _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3.4f) * 3f);
            }
        }
    }
}
