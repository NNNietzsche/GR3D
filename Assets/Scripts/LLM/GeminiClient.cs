using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using EchoRoom.Core;
using UnityEngine;

namespace EchoRoom.LLM
{
    public class GeminiClient : MonoBehaviour
    {
        [Header("Local LLM Proxy")]
        [SerializeField] private bool useProxy = true;
        [SerializeField] private string proxyUrl = "http://127.0.0.1:8787/echo-room";
        [SerializeField] private int proxyTimeoutSeconds = 20;
        [SerializeField] private int languageIndex;

        private static readonly HttpClient HttpClient = new HttpClient();

        public void SetLanguageIndex(int value)
        {
            languageIndex = Mathf.Clamp(value, 0, 2);
        }

        public async Task<SceneProfile> BuildScene(string prompt)
        {
            SceneProfile remote = await RequestScene(prompt);
            if (remote != null) return remote;
            return BuildLocalScene(prompt);
        }

        public async Task<ChallengeResult> GenerateChallenge(CellType type, PlayerState player, List<PlayerState> players, SceneProfile scene)
        {
            ChallengeResult remote = await RequestChallenge(type, player, players, scene);
            if (remote != null) return remote;

            bool isDuo = players != null && players.Count <= 2;
            bool isTruth = type == CellType.Truth;
            string context = scene != null && !string.IsNullOrEmpty(scene.SceneContext) ? scene.SceneContext : FallbackContext();

            return new ChallengeResult
            {
                Challenge = BuildChallengeText(player, context, isTruth, isDuo),
                Tag = isTruth ? Text("truth") : Text("action"),
                Angle = isTruth ? Text("truthAngle") : Text("actionAngle"),
                RewardDesc = isTruth ? "+1" : "+2",
                Difficulty = isTruth ? 1 : 2
            };
        }

        public async Task<FortuneResult> GenerateFortune(PlayerState player, SceneProfile scene, List<PlayerState> players)
        {
            FortuneResult remote = await RequestFortune(player, scene, players);
            if (remote != null) return remote;

            int playerCount = players == null ? 0 : players.Count;
            if (playerCount <= 2)
            {
                return new FortuneResult
                {
                    Title = "默契加成",
                    Effect = FortuneText(player == null ? "Player" : player.Name, 2),
                    EffectType = "bonus",
                    Value = 2,
                    FlavorText = "刚好对味"
                };
            }

            return new FortuneResult
            {
                Title = Text("fortuneTitle"),
                Effect = FortuneText(player == null ? "Player" : player.Name, 1),
                EffectType = "bonus",
                Value = 1,
                FlavorText = "全场注意"
            };
        }

        private async Task<SceneProfile> RequestScene(string prompt)
        {
            ProxyRequest request = new ProxyRequest
            {
                kind = "scene",
                prompt = prompt,
                language = LanguageName()
            };

            string body = await PostProxy(request);
            if (string.IsNullOrEmpty(body)) return null;

            ProxySceneResponse response = JsonUtility.FromJson<ProxySceneResponse>(body);
            return response == null ? null : response.scene;
        }

        private async Task<ChallengeResult> RequestChallenge(CellType type, PlayerState player, List<PlayerState> players, SceneProfile scene)
        {
            ProxyRequest request = new ProxyRequest
            {
                kind = "challenge",
                type = type.ToString(),
                playerName = player == null ? "当前玩家" : player.Name,
                playerCount = players == null ? 0 : players.Count,
                sceneContext = scene == null ? "" : scene.SceneContext,
                scoreLabel = scene == null ? Text("point") : scene.ScoreLabel,
                language = LanguageName()
            };

            string body = await PostProxy(request);
            if (string.IsNullOrEmpty(body)) return null;

            ProxyChallengeResponse response = JsonUtility.FromJson<ProxyChallengeResponse>(body);
            return response == null ? null : response.challenge;
        }

        private async Task<FortuneResult> RequestFortune(PlayerState player, SceneProfile scene, List<PlayerState> players)
        {
            ProxyRequest request = new ProxyRequest
            {
                kind = "fortune",
                playerName = player == null ? "当前玩家" : player.Name,
                playerCount = players == null ? 0 : players.Count,
                sceneContext = scene == null ? "" : scene.SceneContext,
                scoreLabel = scene == null ? Text("point") : scene.ScoreLabel,
                language = LanguageName()
            };

            string body = await PostProxy(request);
            if (string.IsNullOrEmpty(body)) return null;

            ProxyFortuneResponse response = JsonUtility.FromJson<ProxyFortuneResponse>(body);
            return response == null ? null : response.fortune;
        }

        private async Task<string> PostProxy(ProxyRequest request)
        {
            if (!useProxy || string.IsNullOrEmpty(proxyUrl)) return null;

            try
            {
                HttpClient.Timeout = TimeSpan.FromSeconds(Mathf.Clamp(proxyTimeoutSeconds, 3, 60));
                string json = JsonUtility.ToJson(request);
                using (StringContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage response = await HttpClient.PostAsync(proxyUrl, content);
                    if (!response.IsSuccessStatusCode) return null;
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning("LLM proxy unavailable, using local fallback. " + error.Message);
                return null;
            }
        }

        private string BuildChallengeText(PlayerState player, string context, bool isTruth, bool isDuo)
        {
            string name = player == null ? Text("currentPlayer") : player.Name;
            string shortContext = context.Length > 28 ? context.Substring(0, 28) : context;

            if (languageIndex == 1)
            {
                if (isTruth) return string.Format("{0}, based on \"{1}\", share one concrete thought you can explain right now.", name, shortContext);
                if (isDuo) return string.Format("{0}, with the other player, recreate the most visual one-second moment from \"{1}\".", name, shortContext);
                return string.Format("{0}, choose one player and act out a specific scene from \"{1}\" with one line and one gesture.", name, shortContext);
            }

            if (languageIndex == 2)
            {
                if (isTruth) return string.Format("{0}、「{1}」について、今すぐ具体例を出せる本音を一つ話してください。", name, shortContext);
                if (isDuo) return string.Format("{0}、もう一人と一緒に「{1}」で一番絵になる一瞬を動きで再現してください。", name, shortContext);
                return string.Format("{0}、一人を選び、「{1}」の具体的な場面を一言と一つの動きで演じてください。", name, shortContext);
            }

            if (isTruth)
            {
                return string.Format("{0}，围绕“{1}”，说一个你现在能立刻举例说明的真实想法。", name, shortContext);
            }

            if (isDuo)
            {
                return string.Format("{0}，和另一位玩家用一个动作复刻“{1}”里最有画面感的一秒。", name, shortContext);
            }

            return string.Format("{0}，选一位玩家，用一句台词和一个动作演出“{1}”里的具体场面。", name, shortContext);
        }

        private SceneProfile BuildLocalScene(string prompt)
        {
            string safePrompt = string.IsNullOrEmpty(prompt) ? FallbackContext() : prompt;
            return new SceneProfile
            {
                SceneName = Text("sceneName"),
                Atmosphere = safePrompt.Length > 20 ? safePrompt.Substring(0, 20) : safePrompt,
                ChallengeStyle = Text("style"),
                SceneContext = safePrompt,
                OpeningLine = Text("opening"),
                TruthPoints = 1,
                DarePoints = 2,
                ScoreLabel = Text("scoreLabel"),
                MoodTags = new List<string> { Text("specific"), Text("live"), Text("interactive") },
                PreviewChallengeTruth = Text("previewTruth"),
                PreviewChallengeDare = Text("previewDare"),
                OriginalPrompt = safePrompt
            };
        }

        private string LanguageName()
        {
            if (languageIndex == 1) return "English";
            if (languageIndex == 2) return "Japanese";
            return "Chinese";
        }

        private string FallbackContext()
        {
            if (languageIndex == 1) return "Friends sit around a living room table playing truth or dare with drinks and dice.";
            if (languageIndex == 2) return "友人たちがリビングのテーブルを囲み、飲み物とサイコロで真実か挑戦を遊ぶ。";
            return "围坐一桌，游戏开始。";
        }

        private string FortuneText(string playerName, int points)
        {
            if (languageIndex == 1) return string.Format("{0} catches the rhythm of the room and gains {1} point(s).", playerName, points);
            if (languageIndex == 2) return string.Format("{0} は場の流れをつかみ、{1} 点を獲得します。", playerName, points);
            return string.Format("{0} 抓住了今晚的节奏，获得 {1} 分。", playerName, points);
        }

        private string Text(string key)
        {
            if (languageIndex == 1)
            {
                switch (key)
                {
                    case "truth": return "Truth";
                    case "action": return "Action";
                    case "truthAngle": return "Concrete confession";
                    case "actionAngle": return "Live interaction";
                    case "fortuneTitle": return "Mood Shift";
                    case "point": return "point";
                    case "currentPlayer": return "Current player";
                    case "sceneName": return "Tonight";
                    case "style": return "Concrete interaction";
                    case "opening": return "Game start. First to the target score wins.";
                    case "scoreLabel": return "courage";
                    case "specific": return "specific";
                    case "live": return "live";
                    case "interactive": return "interactive";
                    case "previewTruth": return "Share one concrete thought connected to tonight's theme.";
                    case "previewDare": return "Recreate tonight's signature moment with one action.";
                }
            }

            if (languageIndex == 2)
            {
                switch (key)
                {
                    case "truth": return "真実";
                    case "action": return "行動";
                    case "truthAngle": return "具体的な本音";
                    case "actionAngle": return "その場の交流";
                    case "fortuneTitle": return "空気の流れ";
                    case "point": return "点";
                    case "currentPlayer": return "現在のプレイヤー";
                    case "sceneName": return "今夜";
                    case "style": return "具体的な交流";
                    case "opening": return "ゲーム開始。先に目標点へ到達した人が勝利です。";
                    case "scoreLabel": return "勇気";
                    case "specific": return "具体";
                    case "live": return "現場";
                    case "interactive": return "交流";
                    case "previewTruth": return "今夜のテーマに関係する具体的な本音を話す。";
                    case "previewDare": return "今夜を象徴する瞬間を一つの動きで再現する。";
                }
            }

            switch (key)
            {
                case "truth": return "真心话";
                case "action": return "行动";
                case "truthAngle": return "具体坦白";
                case "actionAngle": return "现场互动";
                case "fortuneTitle": return "气氛流转";
                case "point": return "分";
                case "currentPlayer": return "当前玩家";
                case "sceneName": return "今晚局";
                case "style": return "具体互动";
                case "opening": return "游戏开始。先到目标分的人获胜。";
                case "scoreLabel": return "勇气";
                case "specific": return "具体";
                case "live": return "现场";
                case "interactive": return "互动";
                case "previewTruth": return "说出一个和今晚背景有关的具体想法。";
                case "previewDare": return "用一个动作复刻今晚的代表性瞬间。";
            }
            return key;
        }

        [Serializable]
        private class ProxyRequest
        {
            public string kind;
            public string prompt;
            public string type;
            public string playerName;
            public int playerCount;
            public string sceneContext;
            public string scoreLabel;
            public string language;
        }

        [Serializable]
        private class ProxySceneResponse
        {
            public SceneProfile scene;
        }

        [Serializable]
        private class ProxyChallengeResponse
        {
            public ChallengeResult challenge;
        }

        [Serializable]
        private class ProxyFortuneResponse
        {
            public FortuneResult fortune;
        }
    }
}
