using UnityEngine;
using UnityEngine.Events;

namespace Draggables.UI
{
    public class DragTriggerZone : MonoBehaviour, IDragEnterHandler, IDragExitHandler
    {
        [SerializeField] private int _priority;
        [SerializeField] private UnityEvent _onDragEnter = new();
        [SerializeField] private UnityEvent _onDragExit = new();

        public UnityEvent onDragEnter => _onDragEnter;
        public UnityEvent onDragExit => _onDragExit;

        public void OnDragEnter(DraggableCursor cursor)
        {
            onDragEnter.Invoke();
        }

        public void OnDragExit(DraggableCursor cursor)
        {
            onDragExit.Invoke();
        }
    }
}