namespace EvoLife.Common
{
    /// <summary>
    /// AI-facing request seam. Policies may ask to reproduce; Simulation decides
    /// whether a local eligible mate exists and whether a birth occurs.
    /// Unattached handlers are a safe no-op.
    /// </summary>
    public interface IReproductionRequestHandler
    {
        void HandleReproduceRequest();
    }
}
