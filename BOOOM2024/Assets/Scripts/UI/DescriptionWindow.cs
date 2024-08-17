using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UI
{
    public class DescriptionWindow : MonoBehaviour
    {
        public void Open()
        {
            Time.timeScale = 0;
            gameObject.SetActive(true);
        }

        public void OnBackClick()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1;
        }
    }
}

