using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Essentials;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Draggables
{
    public class DraggableController : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler,
        IClickable
    {
        private const float minDragDistance = 20;
        private const float minDragDistanceSquared = minDragDistance * minDragDistance;
        private const float maxTapDuration = 0.2f;
        private const float longPress = 0.4f;

        private static readonly int NormalProperty = Animator.StringToHash("Normal");
        private static readonly int HighlightedProperty = Animator.StringToHash("Highlighted");
        private static readonly int PressedProperty = Animator.StringToHash("Pressed");

        [SerializeField] [HideInInspector] private Animator _animator;
        [SerializeField] public bool clickable = true;
        [SerializeField] public bool draggable;
        [SerializeField] public bool dragHorizontal;
        [SerializeField] public bool dragVertical;
        [SerializeField] public bool hasTooltip;

        [SerializeField] private UnityEvent _onTooltip = new();
        [SerializeField] private UnityEvent _offTooltip = new();
        [SerializeField] private UnityEvent _onClick = new();
        [SerializeField] private UnityEvent<Vector2> _onDragStart = new();

        private readonly List<Func<DraggableController, bool>> _rules = new();
        private bool _pointerInside;
        private bool _pointerActive;
        private bool _showingTooltip;

        public UnityEvent onTooltip => _onTooltip;
        public UnityEvent offTooltip => _offTooltip;
        public UnityEvent onClick => _onClick;
        public UnityEvent<Vector2> onDragStart => _onDragStart;

        private void OnEnable()
        {
            if (!_animator || _animator.gameObject != gameObject) _animator = GetComponent<Animator>();
            if (_animator) _animator.SetTrigger(NormalProperty);
        }

        private void OnDisable()
        {
            if (this && Application.isPlaying && _showingTooltip) HideTooltip();
            _pointerInside = false;
            _pointerActive = false;
        }

        public void AddDragRule(Func<DraggableController, bool> rule)
        {
            _rules.Add(rule);
        }

        public void RemoveDragRule(Func<DraggableController, bool> rule)
        {
            _rules.Remove(rule);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsMouseInput(eventData)) ShowTooltip();
            _pointerInside = true;
            UpdateAnimationState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsMouseInput(eventData) && !_showingTooltip) return;
            HideTooltip();
            _pointerInside = false;
            UpdateAnimationState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsMouseInput(eventData))
            {
                StartCoroutine(MouseInputRoutine());
            }
            else if (IsTouchInput(eventData)) StartCoroutine(TouchInputRoutine());
            _pointerActive = true;
            UpdateAnimationState();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pointerActive = false;
            UpdateAnimationState();
        }

        private void ShowTooltip()
        {
            if (_showingTooltip) return;
            _showingTooltip = true;
            onTooltip.Invoke();
        }

        private void HideTooltip()
        {
            if (!_showingTooltip) return;
            _showingTooltip = false;
            offTooltip.Invoke();
        }

        private IEnumerator MouseInputRoutine()
        {
            var inputStartPosition = DraggablesInput.pointAction.ReadValue<Vector2>();
            Vector2 currentPosition = default;
            var inputDuration = 0f;
            var drag = false;
            yield return null;
            var scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect) scrollRect.enabled = false;
            while (DraggablesInput.interactAction.inProgress)
            {
                inputDuration += Time.deltaTime;
                currentPosition = DraggablesInput.pointAction.ReadValue<Vector2>();
                var delta = currentPosition - inputStartPosition;
                if (draggable && dragHorizontal && dragVertical && delta.sqrMagnitude >= minDragDistanceSquared)
                {
                    drag = true;
                    break;
                }

                if (draggable && dragHorizontal && Mathf.Abs(delta.x) >= minDragDistance)
                {
                    drag = true;
                    break;
                }

                if (draggable && dragVertical && Mathf.Abs(delta.y) >= minDragDistance)
                {
                    drag = true;
                    break;
                }

                if (draggable && inputDuration >= longPress)
                {
                    drag = true;
                    break;
                }

                yield return null;
            }
            
            if (scrollRect) scrollRect.enabled = true;

            if (clickable && !drag && inputDuration < maxTapDuration)
            {
                onClick.Invoke();
                yield break;
            }

            if (drag && _rules.All(r => r(this))) onDragStart.Invoke(currentPosition);
        }

        private IEnumerator TouchInputRoutine()
        {
            var inputStartPosition = DraggablesInput.pointAction.ReadValue<Vector2>();
            Vector2 currentPosition = default;
            var inputDuration = 0f;
            var drag = false;
            yield return null;
            while (DraggablesInput.interactAction.inProgress)
            {
                inputDuration += Time.deltaTime;
                currentPosition = DraggablesInput.pointAction.ReadValue<Vector2>();
                var delta = currentPosition - inputStartPosition;
                if (draggable && dragHorizontal && dragVertical && delta.sqrMagnitude >= minDragDistanceSquared)
                {
                    drag = true;
                    break;
                }

                if (draggable && dragHorizontal && Mathf.Abs(delta.x) >= minDragDistance)
                {
                    drag = true;
                    break;
                }

                if (draggable && dragVertical && Mathf.Abs(delta.y) >= minDragDistance)
                {
                    drag = true;
                    break;
                }

                if (!_showingTooltip && hasTooltip && inputDuration >= longPress)
                {
                    ShowTooltip();
                }

                if (!hasTooltip && draggable && inputDuration >= longPress)
                {
                    drag = true;
                    break;
                }

                yield return null;
            }

            if (_showingTooltip) HideTooltip();

            if (clickable && !drag && !_showingTooltip && inputDuration < maxTapDuration)
            {
                onClick.Invoke();
                yield break;
            }

            if (drag && _rules.All(r => r(this)))
            {
                var scrollRect = GetComponentInParent<ScrollRect>();
                if (scrollRect) scrollRect.StopMovement();
                onDragStart.Invoke(currentPosition);
            }
        }

        private bool IsMouseInput(PointerEventData eventData)
        {
            return eventData is ExtendedPointerEventData { pointerType: UIPointerType.MouseOrPen } ||
                   eventData.pointerId < 0;
        }

        private bool IsTouchInput(PointerEventData eventData)
        {
            return eventData.pointerId >= 0;
        }

        private void UpdateAnimationState()
        {
            if (!_animator) return;
            if (_pointerActive)
            {
                _animator.SetTrigger(PressedProperty);
            }
            else if (_pointerInside)
            {
                _animator.SetTrigger(HighlightedProperty);
            }
            else
            {
                _animator.SetTrigger(NormalProperty);
            }
        }

        private void OnValidate()
        {
            if (!_animator || _animator.gameObject != gameObject) _animator = GetComponent<Animator>();
        }
    }
}