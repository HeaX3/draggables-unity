using System;
using Draggables.UI;

namespace Draggables
{
    public class DraggableItem
    {
        public readonly Guid id;
        public readonly object data;

        public DraggableItem(Guid id, object data)
        {
            this.id = id;
            this.data = data;
        }
    }

    public class DraggableItem<T> : DraggableItem
    {
        public T item { get; }
        
        public DraggableItem(Guid id, T item) : base(id, item)
        {
            this.item = item;
        }

        public void Apply(DraggableItemUIController controller)
        {
            
        }
    }
}