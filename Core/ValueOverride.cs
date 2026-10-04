using System;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// Overrides a value on a game object or asset and restores the vanilla value when the override is turned off.
    /// The vanilla value is captured the first time the override is applied.
    /// </summary>
    internal sealed class ValueOverride<T>
    {
        private readonly Func<T> _get;
        private readonly Action<T> _set;
        private bool _captured;
        private T _vanilla;

        internal ValueOverride(Func<T> get, Action<T> set)
        {
            _get = get;
            _set = set;
        }

        internal void Apply(bool active, T value)
        {
            if (!_captured)
            {
                _vanilla = _get();
                _captured = true;
            }

            _set(active ? value : _vanilla);
        }
    }
}
