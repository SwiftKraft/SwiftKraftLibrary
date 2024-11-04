using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SwiftKraft.Utils
{
    public static class ArrayExtensions
    {
        public static T GetRandom<T>(this T[] values) => values[Random.Range(0, values.Length)];

        public static T GetRandom<T>(this T[] values, ref int lastRandom)
        {
            int choice = Random.Range(0, values.Length);
            if (lastRandom == choice)
                choice = values.WrapIndex(choice + (Random.Range(0, 2) == 0 ? -1 : 1));
            lastRandom = choice;
            return values[choice];
        }

        public static T GetRandom<T>(this List<T> values, ref int lastRandom)
        {
            int choice = Random.Range(0, values.Count);
            if (lastRandom == choice)
                choice = values.WrapIndex(choice + (Random.Range(0, 2) == 0 ? -1 : 1));
            lastRandom = choice;
            return values[choice];
        }

        public static bool InRange(this Array values, int id) => id >= 0 && id < values.Length;
        public static bool InRange(this ICollection values, int id) => id >= 0 && id < values.Count;

        public static int WrapIndex(this Array values, int index) => index.Wrap(0, values.Length);
        public static int WrapIndex(this ICollection values, int index) => index.Wrap(0, values.Count);
        public static int Wrap(this int number, int min, int max) => ((((number - min) % (max - min)) + (max - min)) % (max - min)) + min;
    }
}
