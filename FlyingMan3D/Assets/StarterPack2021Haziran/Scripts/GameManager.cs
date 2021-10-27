using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BhorGames
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public Transform player;

        void Awake()
        {
            if (Instance == null) Instance = this;
            Application.targetFrameRate = 60;
        }
        public void StartGame()
        {
            LevelManager.Instance.StartLevel();
            //player.startlevel
        }
    }
}