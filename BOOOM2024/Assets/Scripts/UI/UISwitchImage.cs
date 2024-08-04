using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Disc0ver.Engine
{
    [RequireComponent(typeof(Image))]
    public class UISwitchImage: MonoBehaviour
    {
        public int index;

        public List<Sprite> images;

        private Image _image;

        public void Awake()
        {
            _image = GetComponent<Image>();
        }

        public void SetIndex(int idx)
        {
            if (idx >= images.Count)
            {
                Debug.LogError($"[UISwitchImage][SetIndex] {idx} >= images count");
                return;
            }

            index = idx;
            UpdateImage();
        }
        
        private void UpdateImage()
        {
#if UNITY_EDITOR
            _image = GetComponent<Image>();
#endif
            if (index >= images.Count)
            {
                Debug.LogError($"[UISwitchImage][SetIndex] {index} >= images count");
                return;
            }
            
            _image.sprite = images[index];
        }
    }
}