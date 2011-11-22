namespace Nyaml
{
    public interface IEmitter
    {
        void Reset();
        void Emit(Events.Base @event);
    }
}