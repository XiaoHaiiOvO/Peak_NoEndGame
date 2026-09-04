using TMPro;
using UnityEngine;

namespace Peak_NoEndGame
{
    public sealed class ReviewUI : MonoBehaviour
    {
        private const float DisplayDuration = 10f;
        private static ReviewUI _instance;
        private static bool _visible;
        private static int _remainingRespawns;

        private TMP_Text _text;
        private float _hideAt;
        private int _lastRenderedValue = int.MinValue;

        private void Awake()
        {
            _instance = this;
            _text = GetComponentInChildren<TMP_Text>(true);
            if (_text != null)
            {
                _text.richText = true;
                _text.alignment = TextAlignmentOptions.TopLeft;
                _text.fontSize = 40f;
                _text.color = Color.white;
            }

            if (_visible)
            {
                _hideAt = Time.unscaledTime + DisplayDuration;
            }

            ApplyVisibility();
            RenderValueIfNeeded();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (!_visible)
            {
                return;
            }

            if (Time.unscaledTime >= _hideAt)
            {
                Hide();
                return;
            }

            RenderValueIfNeeded();
        }

        internal static void Show(int remainingRespawns)
        {
            _remainingRespawns = remainingRespawns;
            _visible = true;
            if (_instance != null)
            {
                _instance._lastRenderedValue = int.MinValue;
                _instance._hideAt = Time.unscaledTime + DisplayDuration;
                _instance.ApplyVisibility();
                _instance.RenderValueIfNeeded();
            }
        }

        internal static void Hide()
        {
            _visible = false;
            if (_instance != null)
            {
                _instance._lastRenderedValue = int.MinValue;
                _instance.ApplyVisibility();
            }
        }

        private void ApplyVisibility()
        {
            if (_text != null)
            {
                _text.text = _visible ? _text.text : string.Empty;
            }
        }

        private void RenderValueIfNeeded()
        {
            if (_text == null || _lastRenderedValue == _remainingRespawns)
            {
                return;
            }

            _lastRenderedValue = _remainingRespawns;
            _text.text = "<color=red><size=45>♥</size></color>" + _remainingRespawns;
        }
    }
}
