namespace Draggables
{
    public interface IDragExitHandler : IDragComponent
    {
        void OnDragExit(DraggableCursor cursor);
    }
}