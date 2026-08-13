using System.Collections.Generic;

namespace EvoLife.Common
{
    /// <summary>
    /// Read-only enumeration of currently tracked creatures. Simulation owns the list;
    /// UI/Analytics copy views and must not mutate biology through it.
    /// </summary>
    public interface ILiveCreatureCatalog
    {
        void CopyLiveViews(IList<IAnalyticsCreatureView> destination);
    }
}
