namespace Nyaml
{
    using System.IO;

    public class Dumper : IDumper
    {
        private readonly IEmitter emitter;

        public Dumper(IEmitter emitter)
        {
            this.emitter = emitter;
        }

        public Dumper(Stream stream, bool isCanonical = false, int indent = 2, 
            int width = 80,
            bool allowUnicode = true, LineBreak lineBreak = LineBreak.LineFeed)
        {
            this.emitter = new Emitter(stream, isCanonical, indent, width,
                allowUnicode, lineBreak);
        }

        public void Reset()
        {
            this.emitter.Reset();
        }

        public void Emit(Events.Base @event)
        {
            this.emitter.Emit(@event);
        }
    }
}
