using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.InternalInterference
{
    abstract class AbstrGain
    {
        public double[] HAGain = new double[360];
        public double[] VAGain = new double[360];
        public abstract double[] GetHAGain();
        public abstract double[] GetVAGain();
    }
}
