using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [HideInInspector] public bool isPlayerEntered;
    [HideInInspector] public bool isGameStarted;
    public bool CanSmoke = true;

    [SerializeField] private GameObject GameSuccessPanel;
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private GameObject tapToThrow;
    [SerializeField] private TextMeshProUGUI levelText;
    public Colors[] ColorArray;

    [Serializable]
    public class Colors{
        public Color RingColor;
        public Color RingTransColor;
        public Color PlatformColor;
    }
    public int currentLevel;
    private void Awake()
    {
        if(Instance == null)Instance = this;
        Application.targetFrameRate = 60;
    }
    /// <summary>
    /// Start is called on the frame when a script is enabled just before
    /// any of the Update methods is called the first time.
    /// </summary>
    void Start()
    {
        currentLevel = PlayerPrefs.GetInt("level",0);
        levelText.text = "LEVEL " + (currentLevel+1);
    }

    private void Update()
    {
        if (Instance.isPlayerEntered)
        {
            if (PlayerController.players.Count == 0 && Spawner.enemies.Count >= 0)
            {
                foreach (var item in Spawner.enemies)
                {
                    item.GetComponent<Animator>().SetBool("Win", true);
                    Destroy(item.GetComponent<Rigidbody>());
                }
                GameOver();
                isPlayerEntered = false;

            }
            else if (Spawner.enemies.Count == 0 && PlayerController.players.Count > 0)
            {
                foreach (var item in PlayerController.players)
                {
                    item.GetComponent<Animator>().SetBool("Win", true);
                    Destroy(item.GetComponent<Rigidbody>());                 
                }
                GameSuccess();
                isPlayerEntered = false;
            }
        }
    }

    public void CloseTapText()
    {
        tapToThrow.SetActive(false);
    }

    public void GameOver()
    {
        PlayerController.players = null;
        GameOverPanel.SetActive(true);
    }

    public void GameSuccess()
    {
        PlayerController.players = null;
        GameSuccessPanel.SetActive(true);
    }

    public void LoadNextLevel()
    {
        currentLevel+=1;
        PlayerPrefs.SetInt("level",currentLevel);
        SceneManager.LoadScene((currentLevel%(SceneManager.sceneCountInBuildSettings-1))+1);
    }

    public void LoadAgain()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
