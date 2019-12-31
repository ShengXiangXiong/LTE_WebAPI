using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.InternalInterference
{
    class GainFactory
    {
        public static AbstrGain GetabstractGain(string type)
        {
            AbstrGain gainClass = null;
            switch (type)
            {
                case "KRE738819_902":
                    gainClass= new KREGain();
                    break;
                case "APX909016-5T0":
                    gainClass=new APX5TOGain();
                    break;
                case "APX906515-CT0":
                    gainClass = new APXCTOGain();
                    break;
            }
            return gainClass;
        }
    }
}
