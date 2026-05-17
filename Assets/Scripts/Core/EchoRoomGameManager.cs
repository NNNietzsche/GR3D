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
        private int languageIndex;

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

        public void SetLanguageIndex(int value)
        {
            languageIndex = Mathf.Clamp(value, 0, 2);
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
            AddLog(T("system"), Config.Scene.OpeningLine);
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
                AddLog(player.Name, T("releaseJail"));
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
            AddLog(player.Name, string.Format(T("rollLanded"), dice, CellLabel(cell)));

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
                    AddLog(player.Name, T("doubleNext"));
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.Jail:
                    player.Jailed = true;
                    AddLog(player.Name, T("enterJail"));
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.Free:
                case CellType.Start:
                    NextTurn();
                    IsBusy = false;
                    break;
                case CellType.All:
                    AddLog(T("host"), Config.Players.Count <= 2 ? T("duoAllDone") : T("allDone"));
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
            AddLog(player.Name, string.Format(T("completeChallenge"), points, ScoreLabel()));
            if (!HasWinner()) NextTurn();
            IsBusy = false;
        }

        public void SkipChallenge(PlayerState player)
        {
            if (player == null) return;

            player.DoubleNext = false;
            AddScore(player, -1);
            AddLog(player.Name, string.Format(T("minusPoint"), ScoreLabel()));
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
                    AddLog(player.Name, T("stealPoint"));
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
                    Challenge = type == CellType.Truth ? T("fallbackTruth") : T("fallbackDare"),
                    Tag = type == CellType.Truth ? T("truth") : T("dare"),
                    Angle = T("livePrompt"),
                    RewardDesc = "+" + BoardRules.GetPoints(type, player.DoubleNext, Config.Scene),
                    Difficulty = 1
                };
            }

            AddLog(T("host"), challenge.Challenge);
            if (ChallengeReady != null) ChallengeReady.Invoke(player, type, challenge);
        }

        private async Task ResolveFortune(PlayerState player)
        {
            FortuneResult fortune = GeminiClient != null
                ? await GeminiClient.GenerateFortune(player, Config.Scene, Config.Players)
                : new FortuneResult { Title = T("smallReward"), EffectType = "bonus", Value = 1, Effect = T("gainOne") };

            AddLog(T("fate"), string.Format("{0}: {1}", fortune.Title, fortune.Effect));
            if (FortuneReady != null) FortuneReady.Invoke(player, fortune);
        }

        private void AddScore(PlayerState player, int delta)
        {
            player.Score = Mathf.Max(0, player.Score + delta);
            if (delta != 0)
            {
                AddLog(player.Name, string.Format(T("scoreNow"), delta > 0 ? "+" : "", delta, player.Score, Config.TargetScore));
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

        private string CellLabel(BoardCell cell)
        {
            if (languageIndex == 1)
            {
                if (cell.Type == CellType.Start) return "Start";
                if (cell.Type == CellType.Truth) return "Truth";
                if (cell.Type == CellType.Dare) return "Dare";
                if (cell.Type == CellType.Fortune) return "Fate";
                if (cell.Type == CellType.Jail) return "Jail";
                if (cell.Type == CellType.Free) return "Rest";
                if (cell.Type == CellType.Double) return "x2";
                if (cell.Type == CellType.All) return "All";
            }
            if (languageIndex == 2)
            {
                if (cell.Type == CellType.Start) return "開始";
                if (cell.Type == CellType.Truth) return "真実";
                if (cell.Type == CellType.Dare) return "挑戦";
                if (cell.Type == CellType.Fortune) return "運命";
                if (cell.Type == CellType.Jail) return "禁閉";
                if (cell.Type == CellType.Free) return "休み";
                if (cell.Type == CellType.Double) return "倍";
                if (cell.Type == CellType.All) return "全員";
            }
            return cell.Label;
        }

        private string ScoreLabel()
        {
            if (languageIndex == 1) return "courage";
            if (languageIndex == 2) return "勇気";
            return Config.Scene == null || string.IsNullOrEmpty(Config.Scene.ScoreLabel) ? "勇气" : Config.Scene.ScoreLabel;
        }

        private string T(string key)
        {
            if (languageIndex == 1) return English(key);
            if (languageIndex == 2) return Japanese(key);
            return Chinese(key);
        }

        private static string Chinese(string key)
        {
            switch (key)
            {
                case "system": return "系统";
                case "host": return "主持人";
                case "fate": return "命运";
                case "rollLanded": return "掷出 {0} 点，落在「{1}」。";
                case "scoreNow": return "{0}{1}，当前 {2}/{3}";
                case "releaseJail": return "本回合解除禁闭，跳过行动。";
                case "doubleNext": return "下一个挑战奖励翻倍。";
                case "enterJail": return "进入禁闭，下回合跳过行动。";
                case "duoAllDone": return "双人同步挑战完成。";
                case "allDone": return "全体同步挑战完成。";
                case "completeChallenge": return "完成挑战，+{0} {1}。";
                case "minusPoint": return "-1 {0}。";
                case "stealPoint": return "从领先者那里偷走 1 分。";
                case "fallbackTruth": return "说出一个和今晚背景有关的具体答案。";
                case "fallbackDare": return "完成一个和今晚背景有关的现场动作。";
                case "truth": return "真心话";
                case "dare": return "大冒险";
                case "livePrompt": return "现场题";
                case "smallReward": return "小奖励";
                case "gainOne": return "获得 1 分。";
            }
            return key;
        }

        private static string English(string key)
        {
            switch (key)
            {
                case "system": return "System";
                case "host": return "Host";
                case "fate": return "Fate";
                case "rollLanded": return "rolled {0} and landed on \"{1}\".";
                case "scoreNow": return "{0}{1}, now {2}/{3}";
                case "releaseJail": return "is released and skips this turn.";
                case "doubleNext": return "will double the next challenge reward.";
                case "enterJail": return "enters Jail and skips the next turn.";
                case "duoAllDone": return "Duo challenge complete.";
                case "allDone": return "Group challenge complete.";
                case "completeChallenge": return "Challenge complete, +{0} {1}.";
                case "minusPoint": return "-1 {0}.";
                case "stealPoint": return "steals 1 point from a leading player.";
                case "fallbackTruth": return "Share one concrete answer connected to tonight's theme.";
                case "fallbackDare": return "Perform one concrete action connected to tonight's theme.";
                case "truth": return "Truth";
                case "dare": return "Dare";
                case "livePrompt": return "Live prompt";
                case "smallReward": return "Small Reward";
                case "gainOne": return "Gain 1 point.";
            }
            return key;
        }

        private static string Japanese(string key)
        {
            switch (key)
            {
                case "system": return "システム";
                case "host": return "司会";
                case "fate": return "運命";
                case "rollLanded": return "{0} を振り、「{1}」に止まりました。";
                case "scoreNow": return "{0}{1}、現在 {2}/{3}";
                case "releaseJail": return "禁閉が解除され、このターンをパスします。";
                case "doubleNext": return "次の挑戦の報酬が倍になります。";
                case "enterJail": return "禁閉に入り、次のターンをパスします。";
                case "duoAllDone": return "二人の同時挑戦を達成。";
                case "allDone": return "全員の挑戦を達成。";
                case "completeChallenge": return "挑戦達成、+{0} {1}。";
                case "minusPoint": return "-1 {0}。";
                case "stealPoint": return "トップのプレイヤーから 1 点を奪います。";
                case "fallbackTruth": return "今夜のテーマに関係する具体的な答えを話してください。";
                case "fallbackDare": return "今夜のテーマに関係する具体的な行動をしてください。";
                case "truth": return "真実";
                case "dare": return "挑戦";
                case "livePrompt": return "現場のお題";
                case "smallReward": return "小さな報酬";
                case "gainOne": return "1 点を獲得。";
            }
            return key;
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
