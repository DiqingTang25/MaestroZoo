using UnityEngine;

namespace MaestroZoo
{
    /// <summary>
    /// 难度配置 — Easy / Normal / Hard 三级，控制判定窗口和分数倍率。
    /// ScriptableObject asset, editable in Unity Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "MaestroZoo/Difficulty Profile", fileName = "Difficulty_")]
    public class DifficultyProfile : ScriptableObject
    {
        public DifficultyLevel level = DifficultyLevel.Normal;

        [Header("Timing Windows (seconds)")]
        [Tooltip("Perfect hit window. Smaller = harder.")]
        [Range(0.02f, 0.20f)]
        public float perfectWindow = 0.08f;

        [Tooltip("Good hit window. Smaller = harder.")]
        [Range(0.06f, 0.30f)]
        public float goodWindow = 0.18f;

        [Tooltip("Miss deadline. Notes expire this far past their time.")]
        [Range(0.20f, 0.60f)]
        public float missWindow = 0.35f;

        [Header("Scoring")]
        [Tooltip("Multiplier applied to all scores.")]
        [Range(0.5f, 3.0f)]
        public float scoreMultiplier = 1.0f;

        [Tooltip("Points awarded for Perfect judgment (before multiplier).")]
        public int perfectScore = 1000;

        [Tooltip("Points awarded for Good judgment (before multiplier).")]
        public int goodScore = 500;

        [Header("Gesture Complexity")]
        [Tooltip("On Easy, certain gesture types may be excluded.")]
        public bool allowExpandClose = true;

        [Tooltip("Minimum cooldown between gestures (seconds).")]
        public float gestureMinCooldown = 0.15f;

        [Tooltip("Max notes per second in charts for this difficulty.")]
        [Range(0.5f, 8f)]
        public float maxNoteDensity = 4f;

        public string DisplayName => level switch
        {
            DifficultyLevel.Easy   => "Easy (简单)",
            DifficultyLevel.Normal => "Normal (普通)",
            DifficultyLevel.Hard   => "Hard (困难)",
            _                     => "Normal"
        };

        /// <summary>Apply these timing windows to a JudgeManager at runtime.</summary>
        public void ApplyTo(JudgeManager judge)
        {
            if (judge == null) return;
            judge.perfectWindow = perfectWindow;
            judge.goodWindow = goodWindow;
            judge.missWindow = missWindow;
            judge.scorePerPerfect = Mathf.RoundToInt(perfectScore * scoreMultiplier);
            judge.scorePerGood = Mathf.RoundToInt(goodScore * scoreMultiplier);
        }

        // --- Factory for programmatic creation ---
        public static DifficultyProfile Create(DifficultyLevel level)
        {
            return level switch
            {
                DifficultyLevel.Easy   => CreateEasy(),
                DifficultyLevel.Normal => CreateNormal(),
                DifficultyLevel.Hard   => CreateHard(),
                _                      => CreateNormal()
            };
        }

        private static DifficultyProfile CreateEasy()
        {
            var p = CreateInstance<DifficultyProfile>();
            p.name = "Difficulty_Easy";
            p.level = DifficultyLevel.Easy;
            p.perfectWindow = 0.12f;
            p.goodWindow = 0.24f;
            p.missWindow = 0.45f;
            p.scoreMultiplier = 0.8f;
            p.perfectScore = 1000;
            p.goodScore = 500;
            p.allowExpandClose = false;
            p.gestureMinCooldown = 0.25f;
            p.maxNoteDensity = 2f;
            return p;
        }

        private static DifficultyProfile CreateNormal()
        {
            var p = CreateInstance<DifficultyProfile>();
            p.name = "Difficulty_Normal";
            p.level = DifficultyLevel.Normal;
            p.perfectWindow = 0.08f;
            p.goodWindow = 0.18f;
            p.missWindow = 0.35f;
            p.scoreMultiplier = 1.0f;
            p.perfectScore = 1000;
            p.goodScore = 500;
            p.allowExpandClose = true;
            p.gestureMinCooldown = 0.20f;
            p.maxNoteDensity = 4f;
            return p;
        }

        private static DifficultyProfile CreateHard()
        {
            var p = CreateInstance<DifficultyProfile>();
            p.name = "Difficulty_Hard";
            p.level = DifficultyLevel.Hard;
            p.perfectWindow = 0.05f;
            p.goodWindow = 0.12f;
            p.missWindow = 0.25f;
            p.scoreMultiplier = 1.5f;
            p.perfectScore = 1000;
            p.goodScore = 500;
            p.allowExpandClose = true;
            p.gestureMinCooldown = 0.10f;
            p.maxNoteDensity = 6f;
            return p;
        }
    }

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }
}
