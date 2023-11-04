using ObjectPooling;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Draggables.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class DraggableItemUIController : Selectable, IPoolable
    {
        private const float LongPressDuration = 0.4f;

        [HideInInspector] [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private UnityEvent _onClick = new();
        [SerializeField] private UnityEvent _onDragStart = new();
        [SerializeField] private UnityEvent _onDragEnd = new();

        private Vector2 _size;
        private DraggableItemGrid.Entry _entry;
        private Vector2 velocity;
        private bool positioned;

        private float _pointerDownTime;
        private bool _pointerInside;
        private bool _pointerActive;

        public Vector2 size
        {
            get => _size;
            set
            {
                if (float.IsNaN(value.x) || float.IsNaN(value.y))
                {
                    Debug.LogWarning("Trying to assign NaN value: " + value);
                    return;
                }

                _size = value;
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value.x);
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value.y);
            }
        }

        public DraggableItemGrid.Entry entry
        {
            get => _entry;
            set => ApplyEntry(value);
        }

        public UnityEvent onClick => _onClick;
        public UnityEvent onDragStart => _onDragStart;
        public UnityEvent onDragEnd => _onDragEnd;

        public void Initialize()
        {
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            DoStateTransition(SelectionState.Normal, true);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _pointerInside = false;
            _pointerActive = false;
            _pointerDownTime = 0;
        }

        private void Update()
        {
            if (_pointerActive)
            {
                _pointerDownTime += Time.deltaTime;
                if (_pointerDownTime > LongPressDuration)
                {
                    _pointerActive = false;
                    DoStateTransition(SelectionState.Highlighted, false);
                }
            }
        }

        private void LateUpdate()
        {
            if (entry != null)
            {
                if (float.IsNaN(entry.x) || float.IsNaN(entry.y))
                {
                    Debug.LogWarning("Trying to assign NaN position: " + entry.x + ", " + entry.y);
                    return;
                }

                if (!positioned)
                {
                    JumpToEntryPosition();
                    return;
                }

                _rectTransform.anchoredPosition = Vector2.SmoothDamp(
                    _rectTransform.anchoredPosition,
                    new Vector2(entry.x, entry.y),
                    ref velocity,
                    0.2f
                );
            }
        }

        public void ApplyEntry(DraggableItemGrid.Entry entry)
        {
            positioned = false;
            _entry = entry;
            OnEntryChanged(entry);
        }

        protected virtual void OnEntryChanged(DraggableItemGrid.Entry item)
        {
        }

        public void Activate()
        {
            positioned = false;
        }

        public void ResetForPool()
        {
            positioned = false;
        }

        private void JumpToEntryPosition()
        {
            _rectTransform.anchoredPosition = new Vector2(entry.x, entry.y);
            velocity = Vector2.zero;
            positioned = true;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!_rectTransform) _rectTransform = GetComponent<RectTransform>();
        }
#endif

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            _pointerInside = true;
            DoStateTransition(SelectionState.Highlighted, false);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            _pointerActive = true;
            _pointerDownTime = 0;
            DoStateTransition(SelectionState.Pressed, false);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            _pointerActive = false;
            _pointerInside = false;
            DoStateTransition(SelectionState.Normal, false);
            if (_pointerDownTime < LongPressDuration) OnClick();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            _pointerInside = false;
            DoStateTransition(SelectionState.Normal, false);
        }

        protected virtual void OnClick()
        {
            onClick.Invoke();
        }

        protected virtual void OnDragStart()
        {
            onDragStart.Invoke();
        }
    }

    public abstract class DraggableItemUIController<T> : DraggableItemUIController
    {
        private T _item;

        protected T item
        {
            get => _item;
            private set => ApplyItem(value);
        }

        protected override void OnEntryChanged(DraggableItemGrid.Entry entry)
        {
            this.item = entry.item is T t ? t : default;
        }

        private void ApplyItem(T item)
        {
            _item = item;
            OnItemChanged(item);
        }

        protected virtual void OnItemChanged(T item)
        {
        }
    }
}