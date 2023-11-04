using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Linq;

namespace Draggables
{
    [RequireComponent(typeof(RectTransform))]
    public class DraggableCursor : MonoBehaviour
    {
        public delegate void DragStartEvent(Vector2 dragStartPosition);

        public delegate void DragEndEvent();

        public delegate void CursorEvent(object data);

        public event CursorEvent cursorUpdated = delegate { };
        public event DragStartEvent dragStarted = delegate { };
        public event DragEndEvent dragEnded = delegate { };

        [SerializeField] [HideInInspector] private RectTransform _rectTransform;
        [SerializeField] private UnityEvent _onDrop = new();

        private RectTransform area;
        private Canvas canvas;

        private DraggableController draggable;

        private IDragComponent dragComponent;
        private IDragEnterHandler enterHandler;
        private IDragExitHandler exitHandler;
        private IDropHandler dropHandler;
        private GameObject target;

        private Vector2 startPosition;
        private Vector2 cursorStartPosition;
        private InputAction pointAction;
        private InputAction interactAction;

        public bool dragging { get; private set; }

        /// <summary>
        /// The current screen position of the cursor
        /// </summary>
        public Vector2 position { get; private set; }

        private RectTransform rectTransform => _rectTransform;
        public UnityEvent onDrop => _onDrop;

        public object data { get; private set; }

        private void OnEnable()
        {
            StartCoroutine(UpdateRoutine());
        }

        private void Update()
        {
            if (interactAction == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (!interactAction.inProgress)
            {
                UpdateInteraction();
                Drop(dropHandler);
                return;
            }

            var position = pointAction.ReadValue<Vector2>();
            ApplyPosition(startPosition + (position - cursorStartPosition));
        }

        private IEnumerator UpdateRoutine()
        {
            while (enabled)
            {
                yield return new WaitForSeconds(0.1f);
                UpdateInteraction();
            }
        }

        private void UpdateInteraction()
        {
            var position = pointAction.ReadValue<Vector2>();
            ApplyPosition(startPosition + (position - cursorStartPosition));
            if (IsPointerOverUIObject(position, this.target, out var target))
            {
                if (target == this.target) return;
                var dragComponent = target.GetComponentInParent<IDragComponent>();
                if (dragComponent != null && dragComponent == this.dragComponent) return;

                if (exitHandler != null) exitHandler.OnDragExit(this);

                this.target = target;
                exitHandler = target.GetComponentInParent<IDragExitHandler>();
                dropHandler = target.GetComponentInParent<IDropHandler>();
                var enterHandler = target.GetComponentInParent<IDragEnterHandler>();
                if (enterHandler != null) enterHandler.OnDragEnter(this);

                this.dragComponent = dragComponent;
            }
            else if (this.target)
            {
                if (exitHandler != null) exitHandler.OnDragExit(this);
                this.target = default;
                exitHandler = default;
                dropHandler = default;
                dragComponent = default;
            }
        }

        private void OnDisable()
        {
            if (exitHandler != null) exitHandler.OnDragExit(this);
            target = default;
            exitHandler = default;
            dropHandler = default;
            dragComponent = default;
            if (dragging)
            {
                dragging = false;
                dragEnded();
            }
        }

        public bool Is<T>() => data is T;

        public T GetData<T>() => data is T t ? t : default;

        public void Drag<T>(DraggableController controller, Vector2 fromScreenPosition, T data)
        {
            draggable = controller;
            target = default;
            exitHandler = default;
            dropHandler = default;
            dragComponent = default;
            this.data = data;
            gameObject.SetActive(true);
            area = (RectTransform)rectTransform.parent;
            canvas = area.GetComponentInParent<Canvas>().rootCanvas;
            pointAction ??= DraggablesInput.pointAction;
            interactAction ??= DraggablesInput.interactAction;
            cursorStartPosition = pointAction.ReadValue<Vector2>();
            startPosition = fromScreenPosition;
            ApplyPosition(fromScreenPosition);
            cursorUpdated(data);
            foreach (var component in controller.GetComponentsInChildren<IDragStartHandler>())
            {
                try
                {
                    component.OnDragStart(this);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            dragging = true;
            dragStarted(fromScreenPosition);
        }

        public void Drop(IDropHandler handler)
        {
            if (draggable)
            {
                foreach (var component in draggable.GetComponentsInChildren<IDragEndHandler>())
                {
                    try
                    {
                        component.OnDragEnd(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }

            if (handler != null) handler.OnDrop(this);
            gameObject.SetActive(false);
            onDrop.Invoke();
            dragging = false;
            dragEnded();
        }

        private void ApplyPosition(Vector2 screenPosition)
        {
            position = screenPosition;
            Canvas.ForceUpdateCanvases();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, canvas.worldCamera,
                    out var point))
            {
                rectTransform.localPosition = point;
            }
        }

        private void OnValidate()
        {
            if (!_rectTransform || _rectTransform.gameObject != gameObject)
            {
                _rectTransform = GetComponent<RectTransform>();
            }
        }

        private readonly List<RaycastResult> scanResults = new();

        private bool IsPointerOverUIObject(Vector2 point, GameObject previousTarget, out GameObject target)
        {
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current) { position = point };
            scanResults.Clear();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, scanResults);
            if (scanResults.Count == 0)
            {
                target = default;
                return false;
            }

            var hit = scanResults[0].gameObject;
            if (hit == previousTarget)
            {
                target = hit;
                return target;
            }

            var draggable = hit.GetComponentInParent<IDragComponent>();
            if (draggable == null)
            {
                target = hit;
                return target;
            }

            target = ((MonoBehaviour)draggable).gameObject;
            return target;
        }
    }
}