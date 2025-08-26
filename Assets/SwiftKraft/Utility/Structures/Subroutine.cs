using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public class Subroutine : ITickable
    {
        public readonly List<Func<float, float>> States = new();

        public int CurrentStateIndex { get; private set; }

        public float Tick(float deltaTime) => !States.InRange(CurrentStateIndex) ? default : States[CurrentStateIndex].Invoke(deltaTime);

        public void AddRange(IEnumerable<Func<float, float>> funcs) => States.AddRange(funcs.Where(x => x != null));

        public void Add(Func<float, float> state)
        {
            if (state == null)
                return;
            States.Add(state);
        }

        public bool Remove(Func<float, float> state) => States.Remove(state);

        public void Set(int state)
        {
            if (States.Count <= 0)
                return;
            CurrentStateIndex = Mathf.Clamp(state, 0, States.Count - 1);
        }
    }

    public class Subroutine<E> : Subroutine where E : Enum
    {
        public E CurrentState => (E)(object)CurrentStateIndex;

        public void Set(E state) => Set(Convert.ToInt32(state));
    }
}
