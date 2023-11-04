using ObjectPooling;
using UnityEngine;

namespace Draggables.UI
{
    public class DraggableItemUIPlaceholder : MonoBehaviour, IPoolable
    {
        [HideInInspector] [SerializeField] private RectTransform _rectTransform;
        
        public void Initialize(Vector2 size)
        {
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }
        
        public void Activate()
        {
            
        }

        public void ResetForPool()
        {
            
        }

        private void OnValidate()
        {
            _rectTransform = GetComponent<RectTransform>();
        }
    }
}