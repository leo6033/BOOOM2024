using System;
using Gameplay.UI;
using UnityEngine;

namespace UI
{
    public class MainWindow: MonoBehaviour
    {
        public Animation cubAnimation;
        public TetrisWindow tetrisWindow;

        private void Start()
        {
            cubAnimation.Play();
        }


        public void OnStartClick()
        {
            gameObject.SetActive(false);
            tetrisWindow.StartGame();
        }
    }
}