using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModernMinas.Launcher
{
    static class RandomizeExtension
    {
        // Edited from http://stackoverflow.com/questions/1901606/collection-randomization-using-extension-method
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source, Random generator = null)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (generator == null) generator = new Random();

            var result = source.ToList();
            for (int i = result.Count - 1; i > 0; i--)
            {
                yield return result[generator.Next(i + 1)];
            }
        }
    }
}
