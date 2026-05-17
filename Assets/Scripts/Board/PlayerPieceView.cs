using System.Collections;
using System.Collections.Generic;
using EchoRoom.Core;
using UnityEngine;

namespace EchoRoom.Board
{
    public class PlayerPieceView : MonoBehaviour
    {
        [SerializeField] private EchoRoomGameManager gameManager;
        [SerializeField] private float moveDuration = 0.45f;
        [SerializeField] private float pieceHeight = 0.62f;

        private readonly Dictionary<string, Transform> pieces = new Dictionary<string, Transform>();

        private void OnEnable()
        {
            if (gameManager == null) return;
            gameManager.GameStarted += OnGameStarted;
            gameManager.PlayerMoved += OnPlayerMoved;
        }

        private void OnDisable()
        {
            if (gameManager == null) return;
            gameManager.GameStarted -= OnGameStarted;
            gameManager.PlayerMoved -= OnPlayerMoved;
        }

        private void OnGameStarted(GameConfig config)
        {
            pieces.Clear();
            for (int i = 0; i < config.Players.Count; i++)
            {
                var player = config.Players[i];
                var existing = transform.Find("Player Piece " + (i + 1));
                if (existing != null)
                {
                    pieces[player.Id] = existing;
                    existing.position = GetPiecePosition(0, i, config.Players.Count);
                }
            }
        }

        private void OnPlayerMoved(PlayerState player, BoardCell cell)
        {
            if (!pieces.ContainsKey(player.Id)) return;
            var index = GetPlayerIndex(player.Id);
            var target = GetPiecePosition(cell.Id, index, gameManager.Config.Players.Count);
            StopCoroutine("MovePieceRoutine");
            StartCoroutine(MovePieceRoutine(pieces[player.Id], target));
        }

        private IEnumerator MovePieceRoutine(Transform piece, Vector3 target)
        {
            var start = piece.position;
            var elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / moveDuration);
                var arc = Mathf.Sin(t * Mathf.PI) * 0.35f;
                piece.position = Vector3.Lerp(start, target, t) + Vector3.up * arc;
                yield return null;
            }

            piece.position = target;
        }

        private int GetPlayerIndex(string id)
        {
            for (int i = 0; i < gameManager.Config.Players.Count; i++)
            {
                if (gameManager.Config.Players[i].Id == id) return i;
            }
            return 0;
        }

        private Vector3 GetPiecePosition(int cellId, int playerIndex, int playerCount)
        {
            var basePosition = GetBoardPosition(cellId);
            var offset = GetOffset(playerIndex, playerCount);
            return basePosition + offset + Vector3.up * pieceHeight;
        }

        private static Vector3 GetOffset(int index, int count)
        {
            if (count <= 1) return Vector3.zero;
            var angle = (Mathf.PI * 2f * index) / count;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.22f;
        }

        private static Vector3 GetBoardPosition(int id)
        {
            int row;
            int col;
            if (id <= 9) { row = 0; col = id; }
            else if (id <= 16) { row = id - 9; col = 9; }
            else if (id <= 26) { row = 7; col = 26 - id; }
            else { row = 35 - id; col = 0; }
            return new Vector3(col * 1.08f - 4.86f, 0f, row * 1.08f - 3.78f);
        }
    }
}
