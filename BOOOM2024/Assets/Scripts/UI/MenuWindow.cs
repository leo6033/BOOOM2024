using System;
using System.Collections;
using System.Collections.Generic;
using Gameplay.UI;
using UnityEngine;

namespace UI
{
    public class MenuWindow : MonoBehaviour
    {
        public MainWindow mainWindow;
        public TetrisWindow teTrisWindow;
        
        public void Open()
        {
            Time.timeScale = 0;
            gameObject.SetActive(true);
        }

        public void OnBtnBackToGameClick()
        {
            Time.timeScale = 1;
            gameObject.SetActive(false);
        }

        public void OnBtnBackToMainCLick()
        {
            Time.timeScale = 1;
            
            teTrisWindow.StopAllCoroutines();
            gameObject.SetActive(false);
            mainWindow.gameObject.SetActive(true);
        }
    }
}

