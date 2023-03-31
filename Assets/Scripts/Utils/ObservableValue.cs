using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SelStrom.Asteroids
{
    public class ObservableValue<T>
    {
        private T _value;
        public event Action<T> OnChanged;

        [PublicAPI]
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                OnChanged?.Invoke(_value);
            }
        }

        public ObservableValue(T initial)
        {
            _value = initial;
        }

        public ObservableValue() : this(default(T))
        {
            //
        }
    }
}