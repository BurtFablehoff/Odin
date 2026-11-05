using System;
namespace OdinV2
{
    internal struct CrabMotionSample { public double Progress,Lift; public bool Flying; }
    // Both the desktop companion and review renderer use this trajectory.
    internal static class CrabMotion
    {
        public const double PlantedSeconds=.22,FlightSeconds=.36;
        public const double Period=PlantedSeconds+FlightSeconds,Height=36;
        public static CrabMotionSample At(double age,int hops)
        {
            hops=Math.Max(1,hops);age=Math.Max(0,age);
            if(age>=hops*Period)return new CrabMotionSample{Progress=1,Lift=0,Flying=false};
            int hop=(int)(age/Period);double phase=age-hop*Period;
            if(phase<=PlantedSeconds)return new CrabMotionSample{Progress=hop/(double)hops,Lift=0,Flying=false};
            double t=Math.Max(0,Math.Min(1,(phase-PlantedSeconds)/FlightSeconds));
            return new CrabMotionSample{Progress=(hop+t)/hops,Lift=4*Height*t*(1-t),Flying=true};
        }
    }
}
