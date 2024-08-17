using System.Collections;
using System.Collections.Generic;
using Gameplay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettlementWindow : MonoBehaviour
    {
        public TetrisWindow tetrisWindow;
        public MainWindow mainWindow;
        
        public Text scoreText;

        public void Open(int score)
        {
            scoreText.text = $"{score}";
            gameObject.SetActive(true);
        }

        public void OnRestartClick()
        {
            gameObject.SetActive(false);
            tetrisWindow.StartGame();
        }

        public void OnBackToMenuClick()
        {
            gameObject.SetActive(false);
            mainWindow.gameObject.SetActive(true);
        }
    }
}

