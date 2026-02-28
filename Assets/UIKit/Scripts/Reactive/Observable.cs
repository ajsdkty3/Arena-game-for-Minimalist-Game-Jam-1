using System;

namespace UIKit.Reactive {
    public class Observable<T> {
        private T _value;
        public event Action<T> Changed;

        public Observable(T initialValue = default) {
            _value = initialValue;
        }

        public T Value {
            get => _value;
            set {
                if (Equals(_value, value))
                    return;
                _value = value;
                Changed?.Invoke(_value);
            }
        }

        public void SetSilently(T value) {
            _value = value;
        }
    }
}