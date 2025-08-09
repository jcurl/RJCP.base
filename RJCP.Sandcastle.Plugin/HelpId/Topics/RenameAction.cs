namespace RJCP.Sandcastle.Plugin.Topics
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("{Current}->{Updated}")]
    internal class RenameAction
    {
        public RenameAction(string current, string updated)
        {
            if (current is null)
                throw new ArgumentNullException(nameof(current));
            if (updated is null)
                throw new ArgumentNullException(nameof(updated));
            if (string.IsNullOrWhiteSpace(current))
                throw new ArgumentException("Empty topic identifier", nameof(current));
            if (string.IsNullOrWhiteSpace(updated))
                throw new ArgumentException("Empty topic identifier", nameof(updated));
            if (string.Compare(current, updated, StringComparison.OrdinalIgnoreCase) == 0)
                throw new ArgumentException("Rename of topic only changes case");

            Current = current;
            Updated = updated;
        }

        public string Current { get; }

        public string Updated { get; }
    }
}
