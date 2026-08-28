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

            UiKit.Button(root.transform, "‹", new Vector2(-880, 460), new Vector2(56, 56), shell.Back);
            UiKit.Label(root.transform, "Character", 34, UiKit.White, new Vector2(-680, 460), new Vector2(340, 48), TextAnchor.MiddleLeft);
            UiKit.Label(root.transform, "SAME SPEED. DIFFERENT MOVE.", 16, UiKit.TextDim, new Vector2(-620, 420), new Vector2(460, 24), TextAnchor.MiddleLeft);

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
                cell.rectTransform.sizeDelta = new Vector2(280, 620);
                cell.rectTransform.anchoredPosition = new Vector2(-750 + i * 300, -90);

                var spriteGo = new GameObject("preview");
                spriteGo.transform.SetParent(cell.transform, false);
                var img = spriteGo.AddComponent<Image>();
                img.sprite = def.sprites != null && def.sprites.Length > 0 ? def.sprites[0] : null;
                img.preserveAspect = true;
                var srt = img.rectTransform;
                srt.sizeDelta = new Vector2(170, 170);
                srt.anchoredPosition = new Vector2(0, 110);
                var bob = spriteGo.AddComponent<IdleBob>();
                bob.hop = def.moveStyle == MoveStyle.Hop;

                var nameL = UiKit.Label(cell.transform, def.displayName, 26, UiKit.White, new Vector2(0, -60));
                nameL.fontStyle = FontStyle.Bold;
                UiKit.Label(cell.transform, def.moveStyle == MoveStyle.Hop ? "HOP" : "STEP", 16,
                    def.moveStyle == MoveStyle.Hop ? UiKit.Mint : UiKit.Hex("B48CFF"), new Vector2(0, -100));
                if (isSelected)
                    UiKit.Label(cell.transform, "✓", 30, UiKit.Mint, new Vector2(105, 270));

                var btn = cell.gameObject.AddComponent<Button>();
                string id = def.id;
                btn.onClick.AddListener(() =>
                {
                    CharacterSelection.SelectedId = id;
                    shell.RebuildScreen("character", Build);
                    shell.Push("character");
                });
            }
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
