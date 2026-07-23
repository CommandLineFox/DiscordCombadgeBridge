namespace GFC_ComBadge_Keybind
{
    /// <summary>
    /// Thread-safe holder for the desired Discord mute state.
    /// </summary>
    public sealed class MuteState
    {
        private readonly object gate = new();
        private bool muted;
        private long version;

        public MuteState(bool initialMuted)
        {
            this.muted = initialMuted;
            this.version = 0;
        }

        public bool Set(bool muted, string source)
        {
            lock (this.gate)
            {
                if (this.muted == muted)
                    return false;

                this.muted = muted;
                this.version++;

                Console.WriteLine(muted
                    ? $"Desired state changed by {source}: muted."
                    : $"Desired state changed by {source}: unmuted.");

                return true;
            }
        }

        public MuteSnapshot Get()
        {
            lock (this.gate)
            {
                return new MuteSnapshot(this.muted, this.version);
            }
        }
    }

    /// <summary>
    /// Immutable view of the desired Discord mute state at a point in time.
    /// </summary>
    public readonly record struct MuteSnapshot(bool Muted, long Version);
}