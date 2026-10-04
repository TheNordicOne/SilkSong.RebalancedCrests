using System;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Overrides a value on a game object or asset and restores the vanilla value when the override is turned off.
    /// The vanilla value is captured when the override is created.
    /// </summary>
    internal sealed class ValueOverride<T>
    {
        private readonly Action<T> _set;

        internal ValueOverride(Func<T> get, Action<T> set)
        {
            _set = set;
            Vanilla = get();
        }

        internal T Vanilla { get; }

        internal void Apply(bool active, T value)
        {
            _set(active ? value : Vanilla);
        }
    }
}
