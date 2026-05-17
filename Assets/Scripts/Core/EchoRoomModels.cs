using System;
using System.Collections.Generic;

namespace EchoRoom.Core
{
    public enum CellType
    {
        Start,
        Truth,
        Dare,
        Fortune,
        Jail,
        Free,
        Double,
        All
    }

    [Serializable]
    public class PlayerState
    {
        public string Id;
        public string Name;
        public string Emoji;
        public string ColorHex;
        public int Position;
        public int Score;
        public bool Jailed;
        public bool DoubleNext;
    }

    [Serializable]
    public class BoardCell
    {
        public int Id;
        public string Label;
        public CellType Type;
        public string Icon;
        public string Description;

        public BoardCell(int id, string label, CellType type, string icon, string description)
        {
            Id = id;
            Label = label;
            Type = type;
            Icon = icon;
            Description = description;
        }
    }

    [Serializable]
    public class SceneProfile
    {
        public string SceneName;
        public string Atmosphere;
        public string ChallengeStyle;
        public string SceneContext;
        public string OpeningLine;
        public int TruthPoints = 1;
        public int DarePoints = 2;
        public string ScoreLabel = "分";
        public List<string> MoodTags = new List<string>();
        public string PreviewChallengeTruth;
        public string PreviewChallengeDare;
        public string OriginalPrompt;
    }

    [Serializable]
    public class ChallengeResult
    {
        public string Challenge;
        public string Tag;
        public string Angle;
        public string RewardDesc;
        public int Difficulty = 1;
    }

    [Serializable]
    public class FortuneResult
    {
        public string Title;
        public string Effect;
        public string EffectType;
        public int Value;
        public string FlavorText;
    }

    [Serializable]
    public class GameConfig
    {
        public int TargetScore = 10;
        public string Prompt;
        public List<PlayerState> Players = new List<PlayerState>();
        public SceneProfile Scene;
    }
}
