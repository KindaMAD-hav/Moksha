using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Holographic_Cards.Scripts
{
    /// <summary>
    /// Handles the visual behavior of a card, including tilting and selection animations.
    /// </summary>
    public class CardVisual : MonoBehaviour,
        IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler,
        IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
    {
        private BaseCard card;
        private Canvas canvas;
        private Camera mainCamera;

        [Header("References")]
        [SerializeField] private GameObject originalCardGameObject;
        [SerializeField] private Transform visualShadow;
        [SerializeField] private Transform shakeParent;
        [SerializeField] private Transform tiltParent;
        [SerializeField] private Transform imageParent;

        [Header("Rotation Parameters")]
        [SerializeField] private float rotationAmount = 20f;
        [SerializeField] private float rotationSpeed = 20f;
        [SerializeField] private float autoTiltAmount = 30f;
        [SerializeField] private float manualTiltAmount = 20f;
        [SerializeField] private float tiltSpeed = 20f;

        [Header("Scale Parameters")]
        [SerializeField] private float scaleOnHover = 1.2f;
        [SerializeField] private float scaleTransition = 0.25f;
        [SerializeField] private Ease scaleEase = Ease.OutBack;

        [Header("Select & Hover Parameters")]
        [SerializeField] private float selectPunchAmount = 20f;
        [SerializeField] private float hoverPunchAngle = 5f;
        [SerializeField] private float hoverTransition = 0.15f;

        // Variation offset for tilt animation.
        private int savedIndex;
        private float randomOffset;
        private bool isHovering = false;
        private bool isSelected = false;

        // Tween IDs for managing DOTween animations.
        private const int TweenIdRotation = 2;
        private const int TweenIdScale = 1;

        private void Start()
        {
            card = originalCardGameObject.GetComponent<BaseCard>();
            canvas = GetComponent<Canvas>();
            mainCamera = Camera.main;
            randomOffset = Random.Range(0f, 5f);
        }

        private void Update()
        {
            ApplyCardTilt();
        }

        /// <summary>
        /// Applies a tilt effect based on mouse position and time-based oscillation.
        /// </summary>
        private void ApplyCardTilt()
        {
            float sine = Mathf.Sin(Time.time + savedIndex + randomOffset) * (isHovering ? 0.2f : 1f);
            float cosine = Mathf.Cos(Time.time + savedIndex + randomOffset) * (isHovering ? 0.2f : 1f);

            // Cache mouse world position.
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Vector3 offset = transform.position - mouseWorldPos;
            float tiltX = isHovering ? -offset.y * manualTiltAmount : 0f;
            float tiltY = isHovering ? offset.x * manualTiltAmount : 0f;
            float tiltZ = isHovering ? tiltParent.eulerAngles.z : 1f;

            float targetX = tiltX + (sine * autoTiltAmount);
            float targetY = tiltY + (cosine * autoTiltAmount);

            float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, targetX, tiltSpeed * Time.deltaTime);
            float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, targetY, tiltSpeed * Time.deltaTime);
            float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, (tiltSpeed / 2f) * Time.deltaTime);

            tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
        }

        #region Pointer Event Handlers

        /// <summary>
        /// Called when a drag operation starts.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            // TODO: Implement drag start behavior if needed.
        }

        /// <summary>
        /// Called during a drag operation.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            // TODO: Implement dragging behavior if needed.
        }

        /// <summary>
        /// Called when a drag operation ends.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            canvas.overrideSorting = false;
            transform.DOScale(1f, scaleTransition).SetEase(scaleEase);
        }

        /// <summary>
        /// Called when the pointer enters the card's area.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            imageParent.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
            isHovering = true;
            DOTween.Kill(TweenIdRotation, true);
            shakeParent.DOPunchRotation(Vector3.forward * 2f, 0.1f, 20, 1).SetId(TweenIdRotation);
        }

        /// <summary>
        /// Called when the pointer exits the card's area.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            imageParent.DOScale(1f, scaleTransition).SetEase(scaleEase);
        }

        /// <summary>
        /// Called when the pointer is released over the card.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            DOTween.Kill(TweenIdScale, true);
        }

        /// <summary>
        /// Called when the pointer is pressed down on the card.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            // Toggle selection state on pointer down.
            isSelected = !isSelected;
            ToggleSelect(isSelected);
        }

        #endregion

        /// <summary>
        /// Triggers a visual selection effect on the card.
        /// </summary>
        /// <param name="state">True if selecting, false if deselecting.</param>
        private void ToggleSelect(bool state)
        {
            DOTween.Kill(TweenIdRotation, true);
            var direction = state ? 1f : 0f;
            shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * direction, scaleTransition, 10, 1);
            shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2f), hoverTransition, 20, 1).SetId(TweenIdRotation);
            transform.DOScale(state ? scaleOnHover : 1f, scaleTransition).SetEase(scaleEase);
        }
    }
}
