using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTE.InternalInterference
{
    public class DTInfo
    {
        public double pwrDbm;
        public double distance = 0;

        public DTInfo(double pwrDbm,double distance){
            this.pwrDbm = pwrDbm;
            this.distance = distance;
        }
    }
}
