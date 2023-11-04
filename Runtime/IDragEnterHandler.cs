namespace Draggables
{
    public interface IDragEnterHandler : IDragComponent
    {
        void OnDragEnter(DraggableCursor cursor);
    }
}