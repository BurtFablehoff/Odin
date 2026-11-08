#if ODIN_DEV
using System;

namespace OdinV2
{
    // V12 2c (DEV): a jump leans along its flight arc, nose up while rising and nose down while dropping, and draws
    // its slots by flight role (planning/run-style-jump-v11-3.json flightRoles). Production keeps the upright jump.
    internal sealed partial class CatMind
    {
        public const double JumpTiltLimit=45;
        // The least forward speed the angle assumes (px per unit of flight progress), so a hop straight up or down
        // leans at most 45 degrees instead of standing on its tail.
        public const double JumpTiltReach=480;
        // Fraction of the flight over which the lean eases in after takeoff and out before touchdown.
        public const double JumpTiltEase=.3;
        public bool IsJumpFlight {get{return IsAirborne && Clip.StartsWith("jump");}}
        // Velocity along Step's arc per unit of flight progress t: the derivative of
        // X=sourceX+(landingX-sourceX)*s(t), FootY=sourceY+(landingY-sourceY)*s(t)-flightArc*sin(pi*t), s(t)=t*t*(3-2t).
        private void FlightVelocity(double t,out double vx,out double vy)
        {
            double ds=6*t*(1-t);vx=(landingX-sourceX)*ds;vy=(landingY-sourceY)*ds-flightArc*Math.PI*Math.Cos(Math.PI*t);
        }
        private double FlightRise(double t){double vx,vy;FlightVelocity(t,out vx,out vy);return -vy;}
        private static double Ease(double u){u=Math.Max(0,Math.Min(1,u));return u*u*(3-2*u);}
        // Degrees, positive = nose up for both facings: the arc's direction of travel (screen up is positive),
        // clamped to +/-45, times an ease that is 0 on the ground (crouch, push-off, landing) and 1 mid-flight.
        public double JumpTilt
        {
            get
            {
                if(!IsJumpFlight)return 0;double t=FlightProgress;if(t<=0 || t>=1)return 0;
                double vx,vy;FlightVelocity(t,out vx,out vy);
                double angle=Math.Atan2(-vy,Math.Max(Math.Abs(vx),JumpTiltReach))*180/Math.PI;
                angle=Math.Max(-JumpTiltLimit,Math.Min(JumpTiltLimit,angle));
                return angle*Ease(t/JumpTiltEase)*Ease((1-t)/JumpTiltEase);
            }
        }
        // Flight progress where the arc stops rising. The rise is +flightArc*pi at takeoff and -flightArc*pi at
        // touchdown and crosses zero once, so bisection finds it.
        public double FlightApex
        {
            get
            {
                if(FlightRise(0)<=0)return 0;if(FlightRise(1)>=0)return 1;
                double low=0,high=1;for(int i=0;i<40;i++){double mid=(low+high)/2;if(FlightRise(mid)>0)low=mid;else high=mid;}
                return (low+high)/2;
            }
        }
        // The jump slot to draw: crouch/push-off slots spread over the time before takeoff, rising slots over the
        // climb, the apex slot held around the top of the arc, descending slots over the drop, landing slots after
        // touchdown. Up onto a ledge most of the flight rises; down to the floor most of it drops.
        public int JumpSlot(DevJumpFlight roles)
        {
            double phase=Math.Max(0,Math.Min(1,Age/Duration)),t=FlightProgress;
            if(t<=0)return Spread(roles.preFlight,phase/JumpTakeoff);
            if(t>=1)return Spread(roles.landing,(phase-JumpTakeoff-JumpFlightSpan)/(1-JumpTakeoff-JumpFlightSpan));
            double apex=FlightApex,start=apex-roles.apexHold/2,end=apex+roles.apexHold/2;
            if(t<start)return Spread(roles.rising,t/start);
            if(t>end)return Spread(roles.descending,(t-end)/(1-end));
            return roles.apex[0];
        }
        private static int Spread(int[] slots,double u){return slots[Math.Max(0,Math.Min(slots.Length-1,(int)Math.Floor(Math.Max(0,u)*slots.Length)))];}
    }
    internal sealed partial class OdinWindow
    {
        // The drawn lean of the current frame (LayoutScene rotates the sprite about its anchor by it).
        private double DevJumpTilt(SpriteChoice choice)
        {
            if(devPreviewClip!=null || devStillPoseId>0 || latestArt.JumpFlight==null || !choice.Clip.StartsWith("jump_") || !latestArt.HasActionFamily(choice.Clip))return 0;
            return mind.JumpTilt;
        }
    }
}
#endif
