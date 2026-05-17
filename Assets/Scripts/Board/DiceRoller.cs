using System;
using System.Collections;
using UnityEngine;

namespace EchoRoom.Board
{
    public class DiceRoller : MonoBehaviour
    {
        public event Action<int> Rolled;

        [SerializeField] private float rollDuration = 0.8f;

        public void Roll()
        {
            StartCoroutine(RollRoutine());
        }

        private IEnumerator RollRoutine()
        {
            var elapsed = 0f;
            var value = 1;
            while (elapsed < rollDuration)
            {
                value = UnityEngine.Random.Range(1, 7);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (Rolled != null) Rolled.Invoke(value);
        }
    }
}
