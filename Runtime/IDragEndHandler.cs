namespace Draggables
{
    public interface IDragEndHandler : IDragComponent
    {
        void OnDragEnd(DraggableCursor cursor);
    }
}