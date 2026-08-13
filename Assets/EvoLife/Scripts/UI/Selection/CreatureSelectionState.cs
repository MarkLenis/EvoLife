using System;
using UnityEngine;
using EvoLife.Common;

namespace EvoLife.UI
{
    /// <summary>
    /// Holds the currently selected creature's Common contracts.
    /// Destroyed hosts clear selection; dead hosts remain inspectable until destroyed.
    /// </summary>
    public sealed class CreatureSelectionState
    {
        public event Action Changed;

        UnityEngine.Object host;
        SelectedCreatureSnapshot snapshot = new SelectedCreatureSnapshot();

        public bool HasSelection => snapshot.HasSelection && !snapshot.HostDestroyed;

        public UnityEngine.Object Host => host;

        public SelectedCreatureSnapshot Snapshot => snapshot;

        public void Select(UnityEngine.Object selectedHost, SelectedCreatureSnapshot selected)
        {
            host = selectedHost;
            snapshot = selected ?? new SelectedCreatureSnapshot();
            snapshot.HasSelection = selectedHost != null;
            snapshot.HostDestroyed = selectedHost == null;
            Changed?.Invoke();
        }

        public void UpdateSnapshot(SelectedCreatureSnapshot selected)
        {
            snapshot = selected ?? new SelectedCreatureSnapshot();
            snapshot.HasSelection = host != null;
            snapshot.HostDestroyed = host == null;
        }

        public void Clear()
        {
            if (!snapshot.HasSelection && host == null)
            {
                return;
            }

            host = null;
            snapshot = new SelectedCreatureSnapshot
            {
                HasSelection = false,
                HostDestroyed = false
            };
            Changed?.Invoke();
        }

        public bool RefreshDestroyed()
        {
            if (host == null && snapshot.HasSelection)
            {
                snapshot.HostDestroyed = true;
                snapshot.HasSelection = false;
                host = null;
                Changed?.Invoke();
                return true;
            }

            if (host == null)
            {
                return false;
            }

            return false;
        }
    }
}
