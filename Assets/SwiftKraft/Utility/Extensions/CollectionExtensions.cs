using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SwiftKraft.Utils
{
    public static class CollectionExtensions
    {
        public static T GetRandom<T>(this IList<T> values) => values.Count > 0 ? values[Random.Range(0, values.Count)] : default;

        public static T GetRandom<T>(this IList<T> values, ref int lastRandom)
        {
            if (values.Count <= 0)
                return default;

            int choice = Random.Range(0, values.Count);
            if (lastRandom == choice)
                choice = values.WrapIndex(choice + (Random.Range(0, 2) == 0 ? -1 : 1));
            lastRandom = choice;
            return values[choice];
        }

        public static T GetWeightedRandom<T>(this IList<T> values) where T : IWeight
        {
            if (values.Count <= 0)
                return default;

            int totalWeight = 0;
            foreach (T item in values)
                totalWeight += Mathf.Clamp(item.Weight, 1, int.MaxValue);

            int randomValue = Random.Range(0, totalWeight + 1);
            int cumulativeWeight = 0;

            foreach (T item in values)
            {
                cumulativeWeight += Mathf.Clamp(item.Weight, 1, int.MaxValue);
                if (randomValue <= cumulativeWeight)
                    return item;
            }

            return values[0];
        }

        /// <summary>
        /// Shifts the elements in the array by the specified amount to the right.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values">The source array.</param>
        /// <param name="amount">The amount to shift.</param>
        /// <returns>The source array.</returns>
        public static T[] Shift<T>(this T[] values, int amount = 1)
        {
            if (values == null || values.Length == 0 || amount == 0)
                return values;

            int n = Math.Min(Math.Abs(amount), values.Length);
            int len = values.Length;
            bool right = amount > 0;

            int src = right ? 0 : n;
            int dst = right ? n : 0;
            int clr = right ? 0 : len - n;

            Array.Copy(values, src, values, dst, len - n);
            Array.Clear(values, clr, n);

            return values;
        }

        public static bool InRange<T>(this ICollection<T> values, int id) => id >= 0 && id < values.Count;

        public static int WrapIndex<T>(this ICollection<T> values, int index) => index.Wrap(0, values.Count);

        public static T GetWrap<T>(this IList<T> values, int index) => values[values.WrapIndex(index)];

        public static int Wrap(this int number, int min, int max) => ((((number - min) % (max - min)) + (max - min)) % (max - min)) + min;
    }
}
