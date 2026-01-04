using UnityEngine;
using UnityEngine.EventSystems;

namespace Holographic_Cards.Scripts
{
    public class BaseCard : MonoBehaviour
    {
        public float selectOffsetAmount = 25f;

        private Camera _mainCamera;

        public CardVisual CardVisual { get; private set; }

        // Update is called once per frame
        private void Start()
        {
            CardVisual = transform.GetChild(0).GetComponent<CardVisual>();

            _mainCamera = Camera.main;
        }
    }
}