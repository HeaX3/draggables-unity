namespace Draggables
{
    public interface IDragStartHandler : IDragComponent
    {
        void OnDragStart(DraggableCursor cursor);
    }
}