using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Malaco
{
    public static class Dice
    {
        public static RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        public static int RollDie(int sides)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                byte[] four_bytes = new byte[4];
                provider.GetBytes(four_bytes);
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }
            return (int)(1 + (sides) * (scale / (double)uint.MaxValue));
        }
        public static List<int> RollMultipleDice(int sides, int count)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < count; i++)
            {
                list.Add(RollDie(sides));
            }
            return list;
        }
    }

    static class LevenshteinDistance
    {
        public static int Compute(string s, string t)
        {
            var bounds = new { Height = s.Length + 1, Width = t.Length + 1 };

            int[,] matrix = new int[bounds.Height, bounds.Width];

            for (int height = 0; height < bounds.Height; height++) { matrix[height, 0] = height; };
            for (int width = 0; width < bounds.Width; width++) { matrix[0, width] = width; };

            for (int height = 1; height < bounds.Height; height++)
            {
                for (int width = 1; width < bounds.Width; width++)
                {
                    int cost = (s[height - 1] == t[width - 1]) ? 0 : 1;
                    int insertion = matrix[height, width - 1] + 1;
                    int deletion = matrix[height - 1, width] + 1;
                    int substitution = matrix[height - 1, width - 1] + cost;

                    int distance = Math.Min(insertion, Math.Min(deletion, substitution));

                    if (height > 1 && width > 1 && s[height - 1] == t[width - 2] && s[height - 2] == t[width - 1])
                    {
                        distance = Math.Min(distance, matrix[height - 2, width - 2] + cost);
                    }

                    matrix[height, width] = distance;
                }
            }

            return matrix[bounds.Height - 1, bounds.Width - 1];
        }
    }

    static class TextFormatExtensions
    {
        public static string Strikethrough(this string value)
        {
            char strike = '\u0338';
            List<string> characters = new List<string>();
            foreach(char c in value.ToCharArray())
            {
                characters.Add($"{strike}{c}");
            }
            return string.Join(String.Empty, characters);
        }
    }

    static class ListExtensions
    {
        private static Random rng = new Random();
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
        {
            var l = list.ToList();
            int n = l.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = l[k];
                l[k] = l[n];
                l[n] = value;
            }
            return l;
        }
    }
    
    static class OtherExtensions
    {
        public static Random rand = new Random();
        public static ulong LongRandom(int min, int max)
        {
            ulong result = (ulong)rand.Next(min, max);
            return result;
        }
    }
}

