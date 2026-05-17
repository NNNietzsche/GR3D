using EchoRoom.Core;
using EchoRoom.UI;
using UnityEngine;

namespace EchoRoom.Board
{
    public class BoardLabelLocalizer : MonoBehaviour
    {
        private void OnEnable()
        {
            EchoRoomUI.LanguageChanged += OnLanguageChanged;
        }

        private void Start()
        {
            EchoRoomUI ui = FindFirstObjectByType<EchoRoomUI>();
            Apply(ui == null ? 0 : ui.LanguageIndex);
        }

        private void OnDisable()
        {
            EchoRoomUI.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(int languageIndex)
        {
            Apply(languageIndex);
        }

        private void Apply(int languageIndex)
        {
            for (int i = 0; i < BoardRules.Cells.Count; i++)
            {
                BoardCell cell = BoardRules.Cells[i];
                Transform cellTransform = FindCellTransform(cell.Id);
                if (cellTransform == null) continue;

                Transform labelTransform = cellTransform.Find("Label");
                if (labelTransform == null) continue;

                TextMesh label = labelTransform.GetComponent<TextMesh>();
                if (label == null) continue;

                label.text = GetCellLabel(cell, languageIndex);
                label.characterSize = GetCharacterSize(languageIndex);
            }
        }

        private Transform FindCellTransform(int id)
        {
            string prefix = string.Format("Cell_{0:00}_", id);
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith(prefix)) return child;
            }
            return null;
        }

        private static string GetCellLabel(BoardCell cell, int languageIndex)
        {
            if (languageIndex == 1) return GetEnglishLabel(cell);
            if (languageIndex == 2) return GetJapaneseLabel(cell);
            return GetChineseLabel(cell);
        }

        private static string GetChineseLabel(BoardCell cell)
        {
            if (cell.Type == CellType.Start) return "出发";
            if (cell.Type == CellType.Truth) return "真心话";
            if (cell.Type == CellType.Dare) return "大冒险";
            if (cell.Type == CellType.Fortune) return "命运";
            if (cell.Type == CellType.Jail) return "禁闭";
            if (cell.Type == CellType.Free) return "休息";
            if (cell.Type == CellType.Double) return "双倍";
            if (cell.Type == CellType.All) return "全体";
            return cell.Label;
        }

        private static string GetEnglishLabel(BoardCell cell)
        {
            if (cell.Type == CellType.Start) return "Start";
            if (cell.Type == CellType.Truth) return "Truth";
            if (cell.Type == CellType.Dare) return "Dare";
            if (cell.Type == CellType.Fortune) return "Fate";
            if (cell.Type == CellType.Jail) return "Jail";
            if (cell.Type == CellType.Free) return "Rest";
            if (cell.Type == CellType.Double) return "x2";
            if (cell.Type == CellType.All) return "All";
            return cell.Label;
        }

        private static string GetJapaneseLabel(BoardCell cell)
        {
            if (cell.Type == CellType.Start) return "開始";
            if (cell.Type == CellType.Truth) return "真実";
            if (cell.Type == CellType.Dare) return "挑戦";
            if (cell.Type == CellType.Fortune) return "運命";
            if (cell.Type == CellType.Jail) return "禁閉";
            if (cell.Type == CellType.Free) return "休み";
            if (cell.Type == CellType.Double) return "倍";
            if (cell.Type == CellType.All) return "全員";
            return cell.Label;
        }

        private static float GetCharacterSize(int languageIndex)
        {
            if (languageIndex == 1) return 0.05f;
            return 0.055f;
        }
    }
}
