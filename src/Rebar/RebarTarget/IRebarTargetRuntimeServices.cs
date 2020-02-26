namespace Rebar.RebarTarget
{
    public interface IRebarTargetRuntimeServices
    {
        void Output(string value);
        void FakeDrop(int id);
        bool PanicOccurred { get; set; }
    }
}
