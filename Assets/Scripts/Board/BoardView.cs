using System.Collections.Generic;
using EchoRoom.Core;
using UnityEngine;

namespace EchoRoom.Board
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private Transform cellRoot;
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private float spacing = 1.2f;

        private readonly Dictionary<int, Transform> cells = new Dictionary<int, Transform>();

        public void BuildBoard()
        {
            cells.Clear();
            if (cellRoot == null || cellPrefab == null) return;

            foreach (var cell in BoardRules.Cells)
            {
                var go = Instantiate(cellPrefab, GetCellPosition(cell.Id), Quaternion.identity, cellRoot);
                go.name = $"Cell_{cell.Id:00}_{cell.Type}";
                cells[cell.Id] = go.transform;
            }
        }

        public Vector3 GetCellPosition(int id)
        {
            var row = 0;
            var col = 0;

            if (id <= 9) { row = 0; col = id; }
            else if (id <= 16) { row = id - 9; col = 9; }
            else if (id <= 26) { row = 7; col = 26 - id; }
            else { row = 35 - id; col = 0; }

            return new Vector3(col * spacing, 0f, row * spacing);
        }

        public Transform GetCellTransform(int id)
        {
            return cells.TryGetValue(id, out var cell) ? cell : null;
        }
    }
}
