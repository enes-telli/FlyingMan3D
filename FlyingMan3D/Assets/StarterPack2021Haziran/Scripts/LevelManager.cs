using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace BhorGames
{
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance;
        public int currentLevel;
        public int stage = 0;
        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        void Awake()
        {
            if (Instance == null) Instance = this;
        }
        void Start()
        {
            UIManager.Instance.UpdateLevelTxt();
        }
        public void StartLevel()
        {
        }
        public void NextLevel()
        {
            currentLevel += 1;
            PlayerPrefs.SetInt("level", currentLevel);
            SceneManager.LoadScene((currentLevel % (SceneManager.sceneCountInBuildSettings - 1)) + 1);
        }
        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void EndLevel()
        {
            UIManager.Instance.OpenWinPanel();
        }
        public void EndLevelWithFail()
        {

            UIManager.Instance.OpenLosePanel();
        }
    }
}