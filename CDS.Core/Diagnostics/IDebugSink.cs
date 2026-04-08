namespace CDS.Core.Diagnostics
{
    public interface IDebugSink
    {
        void OnEvent(DebugEvent e);
    }
}