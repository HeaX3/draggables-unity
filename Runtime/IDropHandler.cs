namespace Draggables
{
    public interface IDropHandler : IDragComponent
    {
        bool AllowDrop(DraggableCursor cursor);
        void OnDrop(DraggableCursor cursor);
    }
}