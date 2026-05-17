using System.Collections.Generic;

namespace EchoRoom.Core
{
    public static class BoardRules
    {
        public static readonly List<BoardCell> Cells = new List<BoardCell>
        {
            new BoardCell(0, "出发", CellType.Start, "S", "经过时 +3 分"),
            new BoardCell(1, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(2, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(3, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(4, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(5, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(6, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(7, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(8, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(9, "禁闭", CellType.Jail, "J", "下回合跳过"),
            new BoardCell(10, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(11, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(12, "双倍", CellType.Double, "X2", "下次挑战翻倍"),
            new BoardCell(13, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(14, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(15, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(16, "全体", CellType.All, "A", "一起完成"),
            new BoardCell(17, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(18, "休息", CellType.Free, "R", "安全区域"),
            new BoardCell(19, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(20, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(21, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(22, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(23, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(24, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(25, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(26, "禁闭", CellType.Jail, "J", "下回合跳过"),
            new BoardCell(27, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(28, "双倍", CellType.Double, "X2", "下次挑战翻倍"),
            new BoardCell(29, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(30, "全体", CellType.All, "A", "一起完成"),
            new BoardCell(31, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(32, "命运", CellType.Fortune, "F", "随机效果"),
            new BoardCell(33, "大冒险", CellType.Dare, "D", "现场挑战"),
            new BoardCell(34, "真心话", CellType.Truth, "T", "诚实回答"),
            new BoardCell(35, "命运", CellType.Fortune, "F", "随机效果")
        };

        public static int GetPoints(CellType type, bool isDouble, SceneProfile scene)
        {
            int basePoints = type == CellType.Dare ? scene.DarePoints : scene.TruthPoints;
            return basePoints * (isDouble ? 2 : 1);
        }

        public static bool Move(PlayerState player, int dice, out int newPosition, out bool passedStart)
        {
            int total = Cells.Count;
            int raw = player.Position + dice;
            passedStart = raw >= total;
            newPosition = raw % total;
            return passedStart;
        }
    }
}
