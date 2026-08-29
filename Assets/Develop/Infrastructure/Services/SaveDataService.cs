using System;
using UnityEngine;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class SaveDataService
    {
        private const string SaveKey = "neon-seven-save-v1";
        private SaveData _data;

        public void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            _data = string.IsNullOrEmpty(json) ? SaveData.CreateDefault() : JsonUtility.FromJson<SaveData>(json);
            if (_data == null || _data.levelStars == null || _data.levelStars.Length < 50)
                _data = SaveData.CreateDefault();
        }

        public int BestScore => _data.bestScore;
        public int UnlockedLevel => _data.unlockedLevel;
        public bool IsMuted => _data.isMuted;
        public bool HasCompletedTutorial => _data.tutorialCompleted;

        public int StarsForLevel(int levelNumber)
        {
            if (levelNumber < 1 || levelNumber > _data.levelStars.Length)
                return 0;

            return _data.levelStars[levelNumber - 1];
        }

        public void SetBestScore(int score)
        {
            if (score <= _data.bestScore)
                return;

            _data.bestScore = score;
            Save();
        }

        public void CompleteLevel(int levelNumber, int stars)
        {
            if (levelNumber < 1 || levelNumber > _data.levelStars.Length)
                return;

            _data.levelStars[levelNumber - 1] = Mathf.Max(_data.levelStars[levelNumber - 1], Mathf.Clamp(stars, 1, 3));
            _data.unlockedLevel = Mathf.Max(_data.unlockedLevel, Mathf.Min(levelNumber + 1, _data.levelStars.Length));
            Save();
        }

        public void SetMuted(bool muted)
        {
            if (_data.isMuted == muted)
                return;

            _data.isMuted = muted;
            Save();
        }

        public void CompleteTutorial()
        {
            if (_data.tutorialCompleted)
                return;

            _data.tutorialCompleted = true;
            Save();
        }

        private void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class SaveData
        {
            public int bestScore;
            public int unlockedLevel;
            public bool isMuted;
            public bool tutorialCompleted;
            public int[] levelStars;

            public static SaveData CreateDefault()
            {
                return new SaveData
                {
                    bestScore = 0,
                    unlockedLevel = 1,
                    isMuted = false,
                    tutorialCompleted = false,
                    levelStars = new int[50]
                };
            }
        }
    }
}
