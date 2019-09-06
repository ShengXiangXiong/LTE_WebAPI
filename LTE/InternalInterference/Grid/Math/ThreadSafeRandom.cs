using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LTE.InternalInterference
{
    public class ThreadSafeRandom
    {

        private static Random random = new Random();

        public static double NextDouble()
        {

            lock (random)
            {

                return random.NextDouble();

            }

        }

        public static int Next()
        {

            lock (random)
            {
                return random.Next();

            }
        }

        public static int Next(int max)
        {

            lock (random)
            {
                return random.Next(max);

            }
        }

        public static void NextBytes(byte[] buffer)
        {

            lock (random)
            {
                random.NextBytes(buffer);
            }
        }

        public static int Next(int min, int max)
        {

            lock (random)
            {

                return random.Next(min, max);
            }
        }
    }
}
