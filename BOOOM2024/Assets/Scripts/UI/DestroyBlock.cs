using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class DestroyBlock: MonoBehaviour
    {
        private void Awake()
        {
            var animator = GetComponent<Animator>();
            animator.enabled = true;
            animator.Play("Destroy");

            StartCoroutine(CoDestroy());
        }

        private IEnumerator CoDestroy()
        {
            var waitForSecond = new WaitForSeconds(1);
            yield return waitForSecond;
            Destroy(gameObject);
        }
    }
}