using System;
using System.Globalization;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Models;

namespace RebalancedCrests.Core
{
    /// <summary>
    /// ModMenu choice model for a ranged float: left/right step along a grid of 'step' from 'min', clamped to the
    /// range. A value off the grid (e.g. typed into the config file) moves to the next grid value.
    /// </summary>
    internal sealed class FloatStepChoiceModel : AbstractValueModel<float>, IChoiceModel<float>
    {
        private const float Epsilon = 0.0001f;
        private static readonly float[] NiceSteps = { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.25f, 0.5f, 1f, 2f, 5f, 10f };

        private readonly float _min;
        private readonly float _max;
        private readonly float _step;
        private float _value;

        internal FloatStepChoiceModel(float min, float max, int maxSteps)
        {
            _min = min;
            _max = max;
            _step = Array.Find(NiceSteps, step => (max - min) / step <= maxSteps + Epsilon);
            if (_step == 0f)
            {
                _step = (max - min) / maxSteps;
            }

            _value = min;
        }

        public override float GetValue() => _value;

        public override bool SetValue(float value)
        {
            if (value < _min - Epsilon || value > _max + Epsilon)
            {
                return false;
            }

            if (_value != value)
            {
                _value = value;
                InvokeOnValueChanged();
            }

            return true;
        }

        public bool MoveLeft() => MoveTo((float)Math.Ceiling((_value - _min) / _step - Epsilon) - 1);

        public bool MoveRight() => MoveTo((float)Math.Floor((_value - _min) / _step + Epsilon) + 1);

        public override LocalizedText DisplayString() => _value.ToString("0.###", CultureInfo.InvariantCulture);

        private bool MoveTo(float gridIndex)
        {
            var value = (float)Math.Round(Math.Min(_max, Math.Max(_min, _min + gridIndex * _step)), 4);
            if (Math.Abs(value - _value) < Epsilon)
            {
                return false;
            }

            _value = value;
            InvokeOnValueChanged();
            return true;
        }
    }
}
