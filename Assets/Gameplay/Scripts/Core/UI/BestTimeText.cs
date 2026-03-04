using TMPro;
using UnityEngine;
using System.Globalization;
using Gameplay.Core;

namespace Gameplay.UI {

    [RequireComponent(typeof(TMP_Text))]
    public class BestTimeText : MonoBehaviour {

        TMP_Text _text;

        void Awake() {
            _text = GetComponent<TMP_Text>();
        }

        void Update() {

            float current = BestTimeManager.CurrentTime;
            float best = BestTimeManager.BestTime;

            string currentStr = current.ToString("F4", CultureInfo.InvariantCulture);
            string bestStr = best.ToString("F4", CultureInfo.InvariantCulture);

            _text.text =
                $"Time  {currentStr}\n" +
                $"Best  {bestStr}";
        }
    }
}