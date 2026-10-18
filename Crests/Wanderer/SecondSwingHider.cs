using UnityEngine;

namespace RebalancedCrests.Crests.Wanderer
{
    /// <summary>
    /// Hides the second swing of Charge Slash Basic while <see cref="SingleSwipeNeedleStrikeTweak"/> makes it hit once.
    /// The object's Animator (clip "Hornet Charge Slash Basic") shows the second blade swing (sprites blade0004–0006,
    /// then blade0007) from 0.2167 s, together with "slash 02". The Animator re-enables the SpriteRenderer every frame,
    /// so it is disabled again in LateUpdate, after the Animator ran.
    /// </summary>
    internal sealed class SecondSwingHider : MonoBehaviour
    {
        private const float SecondSwingStart = 0.2167f;

        private Animator _animator;
        private SpriteRenderer _renderer;

        internal SingleSwipeNeedleStrikeTweak Tweak { get; set; }

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (!Tweak.HitsOnceActive)
            {
                return;
            }

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime * state.length >= SecondSwingStart)
            {
                _renderer.enabled = false;
            }
        }
    }
}
