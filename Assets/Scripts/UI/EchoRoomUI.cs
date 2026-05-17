using System.Collections.Generic;
using System.Text;
using EchoRoom.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EchoRoom.UI
{
    public class EchoRoomUI : MonoBehaviour
    {
        [SerializeField] private EchoRoomGameManager gameManager;
        [SerializeField] private Text sceneTitle;
        [SerializeField] private Text currentTurn;
        [SerializeField] private Text targetScore;
        [SerializeField] private Text scoreboardText;
        [SerializeField] private Text logText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private Text setupTitle;
        [SerializeField] private Text setupHint;
        [SerializeField] private Text promptLabel;
        [SerializeField] private Text playerLabel;
        [SerializeField] private Text setupTargetLabel;
        [SerializeField] private InputField promptInput;
        [SerializeField] private InputField targetScoreInput;
        [SerializeField] private InputField playerCountInput;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject winnerPanel;
        [SerializeField] private Text winnerText;
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private Text cardTitle;
        [SerializeField] private Text cardBody;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Button secondaryButton;

        private readonly List<string> logs = new List<string>();
        private bool started;
        private PlayerState pendingPlayer;
        private CellType pendingChallengeType;
        private FortuneResult pendingFortune;
        private int languageIndex;

        private void Awake()
        {
            EnsureEventSystem();
            if (rollButton != null) rollButton.onClick.AddListener(OnRollClicked);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (primaryButton != null) primaryButton.onClick.AddListener(OnPrimaryClicked);
            if (secondaryButton != null) secondaryButton.onClick.AddListener(OnSecondaryClicked);
        }

        private void OnEnable()
        {
            if (gameManager == null) return;
            gameManager.GameStarted += OnGameStarted;
            gameManager.TurnChanged += OnTurnChanged;
            gameManager.WinnerReached += OnWinnerReached;
            gameManager.ChallengeReady += OnChallengeReady;
            gameManager.FortuneReady += OnFortuneReady;
            gameManager.LogAdded += OnLogAdded;
        }

        private void OnDisable()
        {
            if (gameManager == null) return;
            gameManager.GameStarted -= OnGameStarted;
            gameManager.TurnChanged -= OnTurnChanged;
            gameManager.WinnerReached -= OnWinnerReached;
            gameManager.ChallengeReady -= OnChallengeReady;
            gameManager.FortuneReady -= OnFortuneReady;
            gameManager.LogAdded -= OnLogAdded;
        }

        private void Start()
        {
            if (started || gameManager == null) return;
            ConfigureLanguageDropdown();
            ApplyLanguage();
            if (setupPanel != null) setupPanel.SetActive(true);
            if (cardPanel != null) cardPanel.SetActive(false);
            if (winnerPanel != null) winnerPanel.SetActive(false);
            RefreshRollButton();
        }

        private async void OnStartClicked()
        {
            if (started || gameManager == null) return;
            started = true;

            if (startButton != null) startButton.interactable = false;

            string prompt = promptInput != null && !string.IsNullOrWhiteSpace(promptInput.text)
                ? promptInput.text
                : DefaultPrompt();

            int target = ParseInput(targetScoreInput, 10, 3, 99);
            int playerCount = ParseInput(playerCountInput, 4, 2, 6);
            await gameManager.StartGame(prompt, target, CreatePlayers(playerCount));

            if (setupPanel != null) setupPanel.SetActive(false);
            RefreshRollButton();
        }

        private async void OnRollClicked()
        {
            if (gameManager == null || gameManager.IsBusy) return;

            RefreshRollButton();
            await gameManager.RollAndResolve(Random.Range(1, 7));
            RefreshRollButton();
            UpdateScoreboard();
        }

        private void OnGameStarted(GameConfig config)
        {
            if (sceneTitle != null) sceneTitle.text = config.Scene.SceneName;
            if (targetScore != null) targetScore.text = T("target") + " " + config.TargetScore;
            if (winnerPanel != null) winnerPanel.SetActive(false);
            if (cardPanel != null) cardPanel.SetActive(false);
            logs.Clear();
            UpdateScoreboard();
            RefreshRollButton();
        }

        private void OnTurnChanged(PlayerState player)
        {
            if (currentTurn != null) currentTurn.text = T("current") + player.Name;
            UpdateScoreboard();
            RefreshRollButton();
        }

        private void OnWinnerReached(PlayerState player)
        {
            if (winnerPanel != null) winnerPanel.SetActive(true);
            if (winnerText != null) winnerText.text = player.Name + " " + T("wins");
            if (cardPanel != null) cardPanel.SetActive(false);
            if (rollButton != null) rollButton.interactable = false;
            UpdateScoreboard();
        }

        private void OnChallengeReady(PlayerState player, CellType type, ChallengeResult challenge)
        {
            pendingPlayer = player;
            pendingChallengeType = type;
            pendingFortune = null;
            if (cardPanel != null) cardPanel.SetActive(true);
            if (cardTitle != null) cardTitle.text = type == CellType.Truth ? T("truth") : T("dare");
            if (cardBody != null) cardBody.text = challenge.Challenge;
            SetButtonText(primaryButton, T("complete"));
            SetButtonText(secondaryButton, T("skipPenalty"));
            RefreshRollButton();
        }

        private void OnFortuneReady(PlayerState player, FortuneResult fortune)
        {
            pendingPlayer = player;
            pendingFortune = fortune;
            if (cardPanel != null) cardPanel.SetActive(true);
            if (cardTitle != null) cardTitle.text = string.IsNullOrEmpty(fortune.Title) ? T("fortune") : fortune.Title;
            if (cardBody != null) cardBody.text = fortune.Effect;
            SetButtonText(primaryButton, T("confirm"));
            SetButtonText(secondaryButton, T("close"));
            RefreshRollButton();
        }

        private void OnPrimaryClicked()
        {
            ResolvePendingCard(true);
        }

        private void OnSecondaryClicked()
        {
            ResolvePendingCard(false);
        }

        private void ResolvePendingCard(bool completed)
        {
            if (gameManager == null || pendingPlayer == null) return;

            if (cardPanel != null) cardPanel.SetActive(false);
            if (pendingFortune != null)
            {
                gameManager.ConfirmFortune(pendingPlayer, pendingFortune);
            }
            else if (completed)
            {
                gameManager.CompleteChallenge(pendingPlayer, pendingChallengeType);
            }
            else
            {
                gameManager.SkipChallenge(pendingPlayer);
            }

            pendingPlayer = null;
            pendingFortune = null;
            UpdateScoreboard();
            RefreshRollButton();
        }

        private void OnLogAdded(string message)
        {
            logs.Add(message);
            if (logs.Count > 20) logs.RemoveAt(0);
            if (logText != null) logText.text = string.Join("\n", logs.ToArray());
            UpdateScoreboard();
        }

        private void UpdateScoreboard()
        {
            if (scoreboardText == null || gameManager == null || gameManager.Config == null) return;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(T("scoreboard"));
            for (int i = 0; i < gameManager.Config.Players.Count; i++)
            {
                PlayerState player = gameManager.Config.Players[i];
                string marker = i == gameManager.CurrentPlayerIndex ? ">" : " ";
                builder.AppendLine(string.Format("{0} {1}  {2}/{3}", marker, player.Name, player.Score, gameManager.Config.TargetScore));
            }

            scoreboardText.text = builder.ToString();
        }

        private void RefreshRollButton()
        {
            if (rollButton == null || gameManager == null) return;
            rollButton.interactable = started && !gameManager.IsBusy && !HasWinner();
        }

        private bool HasWinner()
        {
            if (gameManager == null || gameManager.Config == null) return false;
            for (int i = 0; i < gameManager.Config.Players.Count; i++)
            {
                if (gameManager.Config.Players[i].Score >= gameManager.Config.TargetScore) return true;
            }
            return false;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>();
            if (label != null) label.text = text;
        }

        private void OnLanguageChanged(int value)
        {
            languageIndex = Mathf.Clamp(value, 0, 2);
            if (gameManager != null && gameManager.GeminiClient != null)
            {
                gameManager.GeminiClient.SetLanguageIndex(languageIndex);
            }
            ApplyLanguage();
        }

        private void ConfigureLanguageDropdown()
        {
            if (languageDropdown == null) return;
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new List<string> { "中文", "English", "日本語" });
            languageDropdown.value = languageIndex;
            languageDropdown.RefreshShownValue();
        }

        private void ApplyLanguage()
        {
            if (setupTitle != null) setupTitle.text = T("setupTitle");
            if (setupHint != null) setupHint.text = T("setupHint");
            if (promptLabel != null) promptLabel.text = T("prompt");
            if (playerLabel != null) playerLabel.text = T("players");
            if (setupTargetLabel != null) setupTargetLabel.text = T("winScore");
            SetButtonText(startButton, T("start"));
            SetButtonText(rollButton, T("roll"));
            if (!started && promptInput != null && string.IsNullOrWhiteSpace(promptInput.text))
            {
                promptInput.text = DefaultPrompt();
            }
            if (gameManager != null && gameManager.Config != null)
            {
                if (targetScore != null) targetScore.text = T("target") + " " + gameManager.Config.TargetScore;
                if (gameManager.Config.Players.Count > 0 && currentTurn != null)
                {
                    currentTurn.text = T("current") + gameManager.CurrentPlayer.Name;
                }
                UpdateScoreboard();
            }
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
                case "setupTitle": return "开始 Echo Room";
                case "setupHint": return "设置今晚的氛围、人数和胜利分";
                case "prompt": return "背景主题";
                case "players": return "人数";
                case "winScore": return "胜利分";
                case "start": return "开始游戏";
                case "roll": return "掷骰子";
                case "target": return "目标";
                case "current": return "当前：";
                case "scoreboard": return "分数榜";
                case "truth": return "真心话";
                case "dare": return "大冒险";
                case "fortune": return "命运卡";
                case "complete": return "完成";
                case "skipPenalty": return "跳过 -1";
                case "confirm": return "确认";
                case "close": return "关闭";
                case "wins": return "获胜！";
                default: return key;
            }
        }

        private static string English(string key)
        {
            switch (key)
            {
                case "setupTitle": return "Start Echo Room";
                case "setupHint": return "Set the mood, players, and winning score";
                case "prompt": return "Theme";
                case "players": return "Players";
                case "winScore": return "Win Score";
                case "start": return "Start Game";
                case "roll": return "Roll Dice";
                case "target": return "Target";
                case "current": return "Turn: ";
                case "scoreboard": return "Scoreboard";
                case "truth": return "Truth";
                case "dare": return "Dare";
                case "fortune": return "Fortune";
                case "complete": return "Complete";
                case "skipPenalty": return "Skip -1";
                case "confirm": return "Confirm";
                case "close": return "Close";
                case "wins": return "wins!";
                default: return key;
            }
        }

        private static string Japanese(string key)
        {
            switch (key)
            {
                case "setupTitle": return "Echo Room 開始";
                case "setupHint": return "雰囲気、人数、勝利点を設定";
                case "prompt": return "テーマ";
                case "players": return "人数";
                case "winScore": return "勝利点";
                case "start": return "ゲーム開始";
                case "roll": return "サイコロ";
                case "target": return "目標";
                case "current": return "現在：";
                case "scoreboard": return "スコア";
                case "truth": return "真実";
                case "dare": return "挑戦";
                case "fortune": return "運命";
                case "complete": return "達成";
                case "skipPenalty": return "パス -1";
                case "confirm": return "確認";
                case "close": return "閉じる";
                case "wins": return "勝利！";
                default: return key;
            }
        }

        private string DefaultPrompt()
        {
            if (languageIndex == 1) return "Friends sit around a living room table playing truth or dare with drinks and dice.";
            if (languageIndex == 2) return "友人たちがリビングのテーブルを囲み、飲み物とサイコロで真実か挑戦を遊ぶ。";
            return "朋友在客厅围坐玩真心话大冒险，桌上有饮料和骰子。";
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static int ParseInput(InputField input, int fallback, int min, int max)
        {
            int value;
            if (input == null || !int.TryParse(input.text, out value)) value = fallback;
            return Mathf.Clamp(value, min, max);
        }

        private static List<PlayerState> CreatePlayers(int count)
        {
            string[] colors = { "#A78BFA", "#F87171", "#34D399", "#FBBF24", "#60A5FA", "#F472B6" };
            List<PlayerState> players = new List<PlayerState>();
            for (int i = 0; i < count; i++)
            {
                players.Add(new PlayerState
                {
                    Id = "p" + (i + 1),
                    Name = "Player " + (i + 1),
                    Emoji = "P" + (i + 1),
                    ColorHex = colors[i % colors.Length]
                });
            }
            return players;
        }
    }
}
