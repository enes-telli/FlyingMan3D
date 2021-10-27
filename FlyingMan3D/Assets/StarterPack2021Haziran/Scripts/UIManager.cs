using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

namespace BhorGames
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance;
        public Image finger;
        public RectTransform winLosePanel;
        public TMP_Text levelText;
        void Awake()
        {
            if (Instance == null) Instance = this;
        }
        private void Update()
        {
            /*  
            VideoCaptureHandle();
              */
        }

        public void UpdateLevelTxt()
        {
            LevelManager.Instance.currentLevel = PlayerPrefs.GetInt("level", 0);
            levelText.text = "LEVEL " + (LevelManager.Instance.currentLevel + 1).ToString();
        }

        public void VideoCaptureHandle()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                finger.DOFade(1, 0.2f).SetId(0);
            }
            if (Input.GetKey(KeyCode.A))
            {
                finger.GetComponent<RectTransform>().anchoredPosition = (Input.mousePosition - new Vector3(Screen.width / 2, Screen.height / 2)) / 1.2f - Vector3.up * 50;
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                finger.DOFade(0, 0.2f).SetId(0);
            }
            if (Input.GetMouseButtonDown(0))
            {
                finger.transform.eulerAngles = new Vector3(40, finger.transform.eulerAngles.y, finger.transform.eulerAngles.z);
            }
            if (Input.GetMouseButtonUp(0))
            {
                finger.transform.eulerAngles = new Vector3(0, finger.transform.eulerAngles.y, finger.transform.eulerAngles.z);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (winLosePanel.GetChild(0).GetComponent<Image>().color.a < 0.1f)
                {
                    OpenWinPanel();
                }
                else
                {
                    CloseWinLosePanel();
                }
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                if (winLosePanel.GetChild(0).GetComponent<Image>().color.a < 0.1f)
                {
                    OpenLosePanel();
                }
                else
                {
                    CloseWinLosePanel();
                }
            }
        }

        public void OpenLosePanel()
        {
            winLosePanel.GetChild(0).GetComponent<Image>().DOFade(0.4f, 0.2f);
            winLosePanel.GetChild(1).DOScale(1, 0.5f).SetEase(Ease.OutBounce);
        }

        public void OpenWinPanel()
        {
            winLosePanel.GetChild(0).GetComponent<Image>().DOFade(0.4f, 0.2f);
            winLosePanel.GetChild(2).DOScale(1, 0.5f).SetEase(Ease.OutBounce);
        }

        public void CloseWinLosePanel()
        {

            winLosePanel.GetChild(0).GetComponent<Image>().DOFade(0, 0.2f);
            winLosePanel.GetChild(1).DOScale(0, 0.3f).SetEase(Ease.Linear);
            winLosePanel.GetChild(2).DOScale(0, 0.3f).SetEase(Ease.Linear);
        }
    }
}