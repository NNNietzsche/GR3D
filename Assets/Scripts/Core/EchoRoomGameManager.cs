using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EchoRoom.LLM;
using UnityEngine;

namespace EchoRoom.Core
{
    public class EchoRoomGameManager : MonoBehaviour
    {
        [Header("Runtime")]
        public GameConfig Config = new GameConfig();
        public int CurrentPlayerIndex;
        public bool IsBusy { get; private set; }

        [Header("Services")]
        public GeminiClient GeminiClient;

        public event Action<GameConfig> GameStarted;
        public event Action<PlayerState> TurnChanged;
        public event Action<PlayerState, BoardCell> PlayerMoved;
        public event Action<PlayerState> WinnerReached;
        public event Action<PlayerState, CellType, ChallengeResult> ChallengeReady;
        public event Action<PlayerState, FortuneResult> FortuneReady;
        public event Action<string> LogAdded;

        public PlayerState CurrentPlayer
        {
            get { return Config.Players[CurrentPlayerIndex]; }
        }

        public async Task StartGame(string prompt, int targetScore, List<PlayerState> players)
        {
            if (players == null || players.Count == 0)
            {
                players = CreateDefaultPlayers();
            }

            Config = new GameConfig
            {
                Prompt = prompt,
                TargetScore = Mathf.Clamp(targetScore, 3, 99),
                Players = players.Select(p => new PlayerState
                {
                    Id = p.Id,
                    Name = p.Name,
                    Emoji = p.Emoji,
                    ColorHex = p.ColorHex,
                    Position = 0,
                    Score = 0,
                    Jailed = false,
                    DoubleNext = false
                }).ToList()
            };

            Config.Scene = GeminiClient != null ? await GeminiClient.BuildScene(prompt) : LocalScene(prompt);
            CurrentPlayerIndex = 0;
            IsBusy = false;

            InvokeGameStarted();
            InvokeTurnChanged(CurrentPlayer);
            AddLog("系统", Config.Scene.OpeningLine);
        }

        public async Task RollAndResolve(int dice)
        {
            if (IsBusy || Config.Players.Count == 0) return;

            IsBusy = true;
            dice = Mathf.Clamp(dice, 1, 6);
            PlayerState player = CurrentPlayer;

            if (player.Jailed)
            {
                player.Jailed = false;
                AddLog(player.Name, "本回合解除禁闭，跳过行动。");
                NextTurn();
                IsBusy = false;
                return;
            }

            int newPosition;
            bool passedStart;
            BoardRules.Move(player, dice, out newPosition, out passedStart);
            if (passedStart) AddScore(player, 3);

            player.Position = newPosition;
            BoardCell cell = BoardRules.Cells[newPosition];
            if (PlayerMoved != null) PlayerMoved.Invoke(player, cell);
            AddLog(player.Name, string.Format("掷出 {0} 点，落在「{1}」。", dice, cell.Label));

            switch (cell.Type)
            {
                case CellType.Truth:
                case CellType.Dare:
                    await ResolveChallenge(player, cell.Type);
                    break;
                case CellType.Fortune:
                    await ResolveFortune(player);
                    break;
                case CellType.Double:
                    player.DoubleNext = true;
                    AddLog(player.Name, "下一个挑战奖励翻倍。");
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.Jail:
                    player.Jailed = true;
                    AddLog(player.Name, "进入禁闭，下回合跳过行动。");
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.Free:
                case CellType.Start:
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.All:
                    AddLog("主持人", Config.Players.Count <= 2 ? "双人同步挑战完成。" : "全体同步挑战完成。");
                    AddScore(player, 1);
                    if (!HasWinner()) NextTurn();
                    IsBusy = false;
                    break;
                default:
                    NextTurn();
                    IsBusy = false;
                    break;
            }
        }

        public void CompleteChallenge(PlayerState player, CellType type)
        {
            if (player == null) return;

            int points = BoardRules.GetPoints(type, player.DoubleNext, Config.Scene);
            player.DoubleNext = false;
            AddScore(player, points);
            AddLog(player.Name, string.Format("完成挑战，+{0} {1}。", points, Config.Scene.ScoreLabel));
            if (!HasWinner()) NextTurn();
            IsBusy = false;
        }

        public void SkipChallenge(PlayerState player)
        {
            if (player == null) return;

            player.DoubleNext = false;
            AddScore(player, -1);
            AddLog(player.Name, string.Format("-1 {0}。", Config.Scene.ScoreLabel));
            if (!HasWinner()) NextTurn();
            IsBusy = false;
        }

        public void ConfirmFortune(PlayerState player, FortuneResult fortune)
        {
            if (player == null || fortune == null) return;

            if (fortune.EffectType == "bonus") AddScore(player, Mathf.Abs(fortune.Value));
            if (fortune.EffectType == "penalty") AddScore(player, -Mathf.Abs(fortune.Value));
            if (fortune.EffectType == "skip") player.Jailed = true;
            if (fortune.EffectType == "steal")
            {
                PlayerState target = Config.Players.FirstOrDefault(p => p.Id != player.Id && p.Score > 0);
                if (target != null)
                {
                    AddScore(target, -1);
                    AddScore(player, 1);
                    AddLog(player.Name, "从领先者那里偷走 1 分。");
                }
            }

            if (!HasWinner()) NextTurn();
            IsBusy = false;
        }

        private async Task ResolveChallenge(PlayerState player, CellType type)
        {
            ChallengeResult challenge = null;
            if (GeminiClient != null)
            {
                challenge = await GeminiClient.GenerateChallenge(type, player, Config.Players, Config.Scene);
            }

            if (challenge == null)
            {
                challenge = new ChallengeResult
                {
                    Challenge = type == CellType.Truth
                        ? "说出一个和今晚背景有关的具体答案。"
                        : "完成一个和今晚背景有关的现场动作。",
                    Tag = type == CellType.Truth ? "真心话" : "大冒险",
                    Angle = "现场题",
                    RewardDesc = "+" + BoardRules.GetPoints(type, player.DoubleNext, Config.Scene),
                    Difficulty = 1
                };
            }

            AddLog("主持人", challenge.Challenge);
            if (ChallengeReady != null) ChallengeReady.Invoke(player, type, challenge);
        }

        private async Task ResolveFortune(PlayerState player)
        {
            FortuneResult fortune = GeminiClient != null
                ? await GeminiClient.GenerateFortune(player, Config.Scene, Config.Players)
                : new FortuneResult { Title = "小奖励", EffectType = "bonus", Value = 1, Effect = "获得 1 分。" };

            AddLog("命运", string.Format("{0}: {1}", fortune.Title, fortune.Effect));
            if (FortuneReady != null) FortuneReady.Invoke(player, fortune);
        }

        private void AddScore(PlayerState player, int delta)
        {
            player.Score = Mathf.Max(0, player.Score + delta);
            if (delta != 0)
            {
                AddLog(player.Name, string.Format("{0}{1}，当前 {2}/{3}", delta > 0 ? "+" : "", delta, player.Score, Config.TargetScore));
            }

            if (player.Score >= Config.TargetScore)
            {
                if (WinnerReached != null) WinnerReached.Invoke(player);
            }
        }

        private bool HasWinner()
        {
            return Config.Players.Any(p => p.Score >= Config.TargetScore);
        }

        private void NextTurn()
        {
            if (Config.Players.Count == 0) return;
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Config.Players.Count;
            InvokeTurnChanged(CurrentPlayer);
        }

        private void AddLog(string speaker, string message)
        {
            if (LogAdded != null) LogAdded.Invoke(string.Format("{0}: {1}", speaker, message));
        }

        private void InvokeGameStarted()
        {
            if (GameStarted != null) GameStarted.Invoke(Config);
        }

        private void InvokeTurnChanged(PlayerState player)
        {
            if (TurnChanged != null) TurnChanged.Invoke(player);
        }

        private static List<PlayerState> CreateDefaultPlayers()
        {
            return new List<PlayerState>
            {
                new PlayerState { Id = "p1", Name = "Player 1", Emoji = "P1", ColorHex = "#A78BFA" },
                new PlayerState { Id = "p2", Name = "Player 2", Emoji = "P2", ColorHex = "#F87171" },
                new PlayerState { Id = "p3", Name = "Player 3", Emoji = "P3", ColorHex = "#34D399" },
                new PlayerState { Id = "p4", Name = "Player 4", Emoji = "P4", ColorHex = "#FBBF24" }
            };
        }

        private static SceneProfile LocalScene(string prompt)
        {
            string safePrompt = string.IsNullOrWhiteSpace(prompt) ? "围坐一桌，游戏开始。" : prompt;
            return new SceneProfile
            {
                SceneName = "今晚局",
                Atmosphere = safePrompt.Length > 20 ? safePrompt.Substring(0, 20) : safePrompt,
                ChallengeStyle = "围绕真实关系和现场细节出题。",
                SceneContext = safePrompt,
                OpeningLine = "游戏开始。先到目标分的人获胜。",
                ScoreLabel = "勇气",
                OriginalPrompt = safePrompt,
                MoodTags = new List<string> { "具体", "现场", "互动" }
            };
        }
    }
}
