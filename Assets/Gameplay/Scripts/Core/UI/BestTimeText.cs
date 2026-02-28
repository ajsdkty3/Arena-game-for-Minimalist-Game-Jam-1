using TMPro;
using UnityEngine;
using Gameplay.Core;

namespace Gameplay.UI {
    [RequireComponent(typeof(TMP_Text))]
    public class BestTimeText : MonoBehaviour {
        TMP_Text _text;

        void Awake() {
            _text = GetComponent<TMP_Text>();
        }

        void OnEnable() {
            float best = SurvivalTimer.BestSeconds();
            _text.text = $"Longest Survival: {best:F4}";
        }
    }
}