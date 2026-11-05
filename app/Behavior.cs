using System;
using System.Collections.Generic;
using System.Drawing;

namespace OdinV2
{
    internal sealed class ClickGesture
    {
        private bool initialized,wasDown,pressed,dragged;private Point origin;private double began;
        public bool Observe(bool down,Point point,double seconds)
        {
            if(!initialized){initialized=true;wasDown=down;return false;}
            if(down && !wasDown){origin=point;began=seconds;pressed=true;dragged=false;}
            if(down && pressed && Math.Abs(point.X-origin.X)+Math.Abs(point.Y-origin.Y)>10)dragged=true;
            bool click=wasDown && !down && pressed && !dragged && seconds-began<=.6;
            if(!down)pressed=false;wasDown=down;return click;
        }
    }
    internal sealed class WindowSurface
    {
        public long Id; public Rectangle Bounds;
        public WindowSurface(long id, Rectangle bounds) { Id = id; Bounds = bounds; }
    }
    internal sealed class StepPlan
    {
        public string Clip; public double Seconds; public double TargetX;
        public bool Moving; public double Speed; public int CrabHops;
        public StepPlan(string clip, double seconds) { Clip = clip; Seconds = seconds; }
    }
    internal sealed partial class CatMind
    {
        public Rectangle Area;
        public double X, FootY, Clock, Age, Duration, AnimationAge, VisualLift;
        public string Clip = "sit", Mode = "Relaxed";
        public bool Paused, PerchesEnabled = true, ClickPouncesEnabled = true;
        public bool AttentionActive;
        public bool WatchEnabled=true, SittingPrototype;
        private bool hovering;private double hoverUntil;
        public bool ViewerAcknowledgement { get { return IsRest(Clip) && (hovering || Clock<hoverUntil); } }
        public double AttentionRestUntil { get { return attentionRestUntil; } }
        public PointF AttentionTarget;
        public double AttentionUntil;
        public long PerchedWindow;
        public double Energy = .65;
        public string Reason = "Settling in";
        public int CompletedZoomies, AcceptedPounces, AcceptedPerches;
        public readonly Dictionary<string, int> ActivityCounts = new Dictionary<string, int>();
        public readonly Dictionary<string, double> ActivitySeconds = new Dictionary<string, double>();
        private HashSet<string> availableClips;
        public bool FacingRight=true;
        private readonly Random random;
        private readonly Queue<StepPlan> plans = new Queue<StepPlan>();
        private StepPlan current;
        private double zoomCooldown, pounceCooldown, perchCooldown, lookCooldown, attentionDue;
        private PointF previousPointer, pendingAttention;
        private bool attentionPending, movingAttention;private double attentionRestUntil;
        private double sourceX, sourceY, landingX, landingY, flightArc, crabStartX;
        private long landingWindow;
        private Rectangle perchedBounds;
        private string lastRest = "sit";
        private List<WindowSurface> windows = new List<WindowSurface>();
        public const double BodyMargin = 210;
        public double Margin=BodyMargin,TopClearance=320;
        public double LeftMargin=-1,RightMargin=-1;
        public CatMind(Rectangle area, int seed)
        {
            Area = area; random = new Random(seed); X = area.Left + area.Width * .65; FootY = area.Bottom;
            zoomCooldown = Range(180,420); pounceCooldown = 45; perchCooldown = Range(120,300);
            Begin(new StepPlan("sit", Range(90,240)), "Getting comfortable"); Clamp();
        }
        public void ConfigureArt(IEnumerable<string> clips)
        {
            availableClips=clips==null?null:new HashSet<string>(clips);
            ClickPouncesEnabled=HasPounces;PerchesEnabled=HasJumps;
        }
        public bool Supports(string clip){return availableClips==null || availableClips.Contains(clip);}
        public bool HasPounces {get{return Supports("pounce_left") && Supports("pounce_right");}}
        public bool HasCrab {get{return Supports("crab_left") && Supports("crab_right");}}
        public bool HasJumps {get{return Supports("jump") || (Supports("jump_left") && Supports("jump_right"));}}
        public static bool IsSleeping(string clip){return clip=="sleep" || clip=="belly_sleep";}
        private void Queue(StepPlan step){if(Supports(step.Clip))plans.Enqueue(step);}
        private string JumpClip(double target){string name="jump_"+(target>=X?"right":"left");return Supports(name)?name:"jump";}
        private double Range(double min, double max) { return min + random.NextDouble() * (max-min); }
        public static bool IsRest(string clip) { return clip == "sit" || clip == "loaf" || clip == "lay" || clip == "sleep" || clip == "belly" || clip == "belly_sleep"; }
        public bool IsAirborne { get { return Clip == "jump" || Clip.StartsWith("jump_") || Clip.StartsWith("pounce_"); } }
        public bool IsCrabBounce { get { return Clip.StartsWith("crab_"); } }
        public double ShadowSurfaceY
        {
            get
            {
                if(!IsAirborne)return FootY;
                double phase=Age/Duration;
                return landingY<sourceY?(phase>.78?landingY:sourceY):(phase<.22?sourceY:landingY);
            }
        }
        public double MinX { get { double left=LeftMargin<0?Margin:LeftMargin,right=RightMargin<0?Margin:RightMargin;return Area.Left+left*Math.Min(1,Area.Width/Math.Max(1,left+right)); } }
        public double MaxX { get { double left=LeftMargin<0?Margin:LeftMargin,right=RightMargin<0?Margin:RightMargin;return Area.Right-right*Math.Min(1,Area.Width/Math.Max(1,left+right)); } }
        public void Clamp()
        {
            if (IsAirborne) return;
            X = Math.Max(MinX, Math.Min(MaxX, X));
            FootY = Math.Max(Math.Min(Area.Bottom,Area.Top+TopClearance), Math.Min(Area.Bottom, FootY));
        }
        private void Count(string clip)
        {
            if (!ActivityCounts.ContainsKey(clip)) ActivityCounts[clip] = 0;
            ActivityCounts[clip]++;
        }
        private void Begin(StepPlan step, string reason)
        {
            if(!Supports(step.Clip))throw new InvalidOperationException("Unavailable animation: "+step.Clip);
            if(step.Clip.EndsWith("_left"))FacingRight=false;else if(step.Clip.EndsWith("_right"))FacingRight=true;
            current = step; Clip = step.Clip; Duration = Math.Max(.05, step.Seconds); Age = 0; AnimationAge = 0; VisualLift=0; Reason = reason;
            if(IsCrabBounce)crabStartX=X;
            Count(Clip);
            if (IsRest(Clip)) lastRest = Clip;
            if (!IsRest(Clip) || IsSleeping(Clip)) { AttentionActive = false; attentionPending = false; }
        }
        public void Rest(string pose, double seconds)
        {
            if(pose=="belly" || pose=="belly_sleep")return;
            plans.Clear();
            string from = IsRest(Clip) ? Clip : lastRest;
            if (from == "sleep") from = "lay";else if(from=="belly_sleep")from="belly";
            if (pose == "sleep")
            {
                EnqueueRestTransition(from,"lay"); Queue(new StepPlan("sleep",seconds));
            }
            else { EnqueueRestTransition(from,pose); Queue(new StepPlan(pose,seconds)); }
            AdvancePlan("Settling into " + pose);
        }
        private void EnqueueRestTransition(string from,string to)
        {
            if (from == to) return;
            if (from == "belly") { Queue(new StepPlan("belly_to_lay",1.7)); from = "lay"; }
            if (to == "belly")
            {
                if (from == "loaf") { Queue(new StepPlan("loaf_to_sit",1.4)); from = "sit"; }
                if (from == "sit") Queue(new StepPlan("sit_to_lay",1.6));
                Queue(new StepPlan("lay_to_belly",1.7)); return;
            }
            if (from == "loaf") { Queue(new StepPlan("loaf_to_sit",1.4)); from = "sit"; }
            if (from == "lay") { Queue(new StepPlan("lay_to_sit",1.6)); from = "sit"; }
            if (to == "loaf") Queue(new StepPlan("sit_to_loaf",1.4));
            if (to == "lay") Queue(new StepPlan("sit_to_lay",1.6));
        }
        private void GetUp()
        {
            string from = IsRest(Clip) ? Clip : lastRest;
            if (from == "sleep") from = "lay";else if(from=="belly_sleep")from="belly";
            EnqueueRestTransition(from,"sit");
        }
        private void AdvancePlan(string reason)
        {
#if ODIN_DEV
            if(DevReturnToHeldState())return;
#endif
            if (plans.Count > 0) Begin(plans.Dequeue(), reason);
            else Decide();
        }
        private StepPlan Travel(double target, string kind, double speed)
        {
            target = Math.Max(MinX,Math.Min(MaxX,target));
            string direction = target >= X ? "right" : "left";
            return new StepPlan(kind + "_" + direction, Math.Abs(target-X) / speed + .06) { Moving = true, TargetX = target, Speed = speed };
        }
        private StepPlan CrabTravel(double from,double target,int hops)
        {
            target=Math.Max(MinX,Math.Min(MaxX,target));
            return new StepPlan("crab_"+(target>=from?"right":"left"),Math.Max(1,hops)*CrabMotion.Period){Moving=true,TargetX=target,CrabHops=Math.Max(1,hops)};
        }
        public void CrabBounce()
        {
            if(!HasCrab)return;

            if(PerchedWindow!=0){LeavePerch();return;}
            plans.Clear();GetUp();bool right=MaxX-X>=X-MinX;
            double available=right?MaxX-X:X-MinX;
            if(available<60){Queue(new StepPlan("sit",Range(30,70)));AdvancePlan("Settling down");return;}
            double target=X+(right?1:-1)*Math.Min(360,available);
            Queue(CrabTravel(X,target,3));Queue(new StepPlan("sit",Range(35,85)));AdvancePlan("A few playful sideways bounces");
        }
        public void Wander()
        {
            // Walking is disabled until its animation is approved.
            return;
        }
        public void Zoomies()
        {

            if (PerchedWindow != 0) { LeavePerch(); return; }
            plans.Clear(); GetUp();
            double target = X < (MinX+MaxX)/2 ? MaxX : MinX;
            Queue(Travel(target,"run",650));
            Queue(new StepPlan("run_"+(target>X?"left":"right"),Math.Abs(MaxX-MinX)/720+.06) { Moving=true,TargetX=target==MaxX?MinX:MaxX,Speed=720 });
            double afterRuns=target==MaxX?MinX:MaxX;
            double crabTarget=afterRuns+(target>X?1:-1)*Math.Min(330,MaxX-MinX);
            if(HasCrab)Queue(CrabTravel(afterRuns,crabTarget,3));
            else
            {
                // Finish the preview's run in an interior spot, leaving room for quiet walking.
                double settleX=MinX+(MaxX-MinX)*Range(.25,.55);
                Queue(new StepPlan("run_"+(settleX>=afterRuns?"right":"left"),Math.Abs(settleX-afterRuns)/650+.06){Moving=true,TargetX=settleX,Speed=650});
            }
            Queue(new StepPlan("sit",Range(15,35))); Queue(new StepPlan("sit_to_lay",1.6));Queue(new StepPlan("lay",Range(70,160)));
            zoomCooldown=Clock+Range(600,1100);Energy=.25;CompletedZoomies++;AdvancePlan("A short burst of zoomies");
        }
        public void Groom()
        {

            plans.Clear();GetUp();Queue(new StepPlan("groom",Range(10,23)));Queue(new StepPlan("sit",Range(35,85)));AdvancePlan("Washing a paw");
        }
        public void Stretch()
        {

            plans.Clear();GetUp();Queue(new StepPlan("stretch",4.6));Queue(new StepPlan("sit",Range(30,75)));AdvancePlan("Stretching out");
        }
        public void UpdateWindows(List<WindowSurface> candidates)
        {
            windows=candidates ?? new List<WindowSurface>();
            if(IsAirborne && landingWindow!=0)
            {
                WindowSurface destination=windows.Find(w=>w.Id==landingWindow);
                if(destination==null || !EligiblePerch(destination)){landingWindow=0;landingY=Area.Bottom;}
                else {landingX=Math.Max(MinX,Math.Min(MaxX,Math.Max(destination.Bounds.Left+90,Math.Min(destination.Bounds.Right-90,landingX))));landingY=destination.Bounds.Top;}
            }
            if (PerchedWindow==0) return;
            WindowSurface surface=windows.Find(w=>w.Id==PerchedWindow);
            if (surface==null || !EligiblePerch(surface)) { LeavePerch(); return; }
            double delta=surface.Bounds.Top-FootY;
            if (!perchedBounds.IsEmpty) X += surface.Bounds.Left-perchedBounds.Left;
            FootY=surface.Bounds.Top;
            X=Math.Max(surface.Bounds.Left+85,Math.Min(surface.Bounds.Right-85,X));Clamp();
            perchedBounds=surface.Bounds;
            if (Math.Abs(delta)>2) Notice(new PointF((float)X,(float)FootY),true);
        }
        public bool EligiblePerch(WindowSurface w)
        {
            return w!=null && w.Bounds.Width>=300 && w.Bounds.Height>=140 && w.Bounds.Top>=Area.Top+TopClearance && w.Bounds.Top<Area.Bottom-120 && w.Bounds.Right>MinX+80 && w.Bounds.Left<MaxX-80;
        }
        public bool TryPerch(bool force)
        {
            if ((Clip!="sit" && Clip!="lay") || !HasJumps || !PerchesEnabled || PerchedWindow!=0 || IsAirborne || (!force && Clock<perchCooldown)) return false;
            List<WindowSurface> choices=windows.FindAll(EligiblePerch);if(choices.Count==0)return false;
            WindowSurface target=choices[random.Next(choices.Count)];
            double x=Math.Max(MinX,Math.Min(MaxX,Math.Max(target.Bounds.Left+90,Math.Min(target.Bounds.Right-90,X))));
            BeginFlight(JumpClip(x),x,target.Bounds.Top,target.Id,Math.Min(150,Math.Max(50,(FootY-target.Bounds.Top)*.25)),1.5);
            perchCooldown=Clock+Range(450,900);AcceptedPerches++;return true;
        }
        public void LeavePerch()
        {
            long old=PerchedWindow;PerchedWindow=0;
            if (old!=0) BeginFlight(JumpClip(X+(FacingRight?1:-1)),Math.Max(MinX,Math.Min(MaxX,X)),Area.Bottom,0,55,1.5);
        }
        private void BeginFlight(string clip,double x,double y,long window,double arc,double seconds)
        {
            plans.Clear(); sourceX=X;sourceY=FootY;landingX=x;landingY=y;landingWindow=window;flightArc=arc;
            Begin(new StepPlan(clip,seconds),clip.StartsWith("pounce")?"Pouncing at a click":"Changing perches");
        }
        public bool ClickReleased(Point point,bool force)
        {
            if (!HasPounces || !ClickPouncesEnabled || Paused || IsAirborne || IsSleeping(Clip) || (!force && Clock<pounceCooldown)) return false;
            double margin=Math.Min(Margin,Area.Width/2.0);
            if (!Area.Contains(point) || Math.Abs(point.X-X)>650 || point.Y<Area.Top+TopClearance || Math.Abs(point.Y-FootY)>340 || point.X<Area.Left+margin || point.X>Area.Right-margin) return false;
            if (!force && random.NextDouble()>.007) return false;
            pounceCooldown=Clock+Range(480,900);
            double x=Math.Max(Area.Left+margin,Math.Min(Area.Right-margin,point.X));double y=Math.Max(Area.Top+210,Math.Min(Area.Bottom,point.Y));
            PerchedWindow=0;BeginFlight(x>=X?"pounce_right":"pounce_left",x,y,0,65,.95);AcceptedPounces++;return true;
        }
        public void Notice(PointF point,bool movingWindow)
        {
            if (!WatchEnabled || Paused || ViewerAcknowledgement || IsSleeping(Clip) || !IsRest(Clip) || Clock<attentionRestUntil) return;
            if(AttentionActive){AttentionTarget=point;movingAttention=movingWindow;return;}
            if(Clock<lookCooldown && !movingWindow)return;
            pendingAttention=point;movingAttention=movingWindow;
            if (!attentionPending) attentionDue=Clock+Range(.25,.65);
            attentionPending=true;lookCooldown=Clock+Range(12,22);
        }
        public void ObserveHover(bool directlyOverCat)
        {
            if(hovering && !directlyOverCat && IsRest(Clip))hoverUntil=Clock+Range(.9,1.8);
            hovering=directlyOverCat;
            if(ViewerAcknowledgement){AttentionActive=false;attentionPending=false;}
        }
        public void SetWatching(bool enabled)
        {
            WatchEnabled=enabled;
            if(!enabled){AttentionActive=false;attentionPending=false;}
        }
        public void LookAtYou()
        {
            Rest("sit",Range(60,120));AttentionActive=false;attentionPending=false;attentionRestUntil=Clock+Range(25,40);lookCooldown=attentionRestUntil;
        }
        private void Decide()
        {
            if(SittingPrototype){Rest("sit",Range(120,300));return;}
            if (IsSleeping(Clip)) { Stretch();return; }
            if (PerchedWindow!=0)
            {
                if (random.NextDouble()<.20) { LeavePerch();return; }
                Rest(random.NextDouble()<.6?"loaf":"sit",Range(60,160));return;
            }
            if (Clock>=zoomCooldown && Energy>.55 && random.NextDouble()<.28) { Zoomies();return; }
            if (Clock>=perchCooldown && random.NextDouble()<.18 && TryPerch(false)) return;
            double pick=random.NextDouble();
            // Automatic walking is disabled until its animation is approved.
            if (pick<.25) { Groom();return; }
            if (pick<.30) { Stretch();return; }
            string pose=pick<.45?"sleep":pick<.63?"loaf":pick<.79?"lay":"sit";
            Rest(pose,IsSleeping(pose)?Range(120,300):Range(90,300));
        }
        public void Step(double dt,Point pointer)
        {
            if (Paused)return;
            dt=Math.Min(.1,Math.Max(0,dt));Clock+=dt;Age+=dt;if(!IsCrabBounce)AnimationAge+=dt;
            if(!ActivitySeconds.ContainsKey(Clip))ActivitySeconds[Clip]=0;ActivitySeconds[Clip]+=dt;
            Energy=Math.Max(0,Math.Min(1,Energy+dt*(IsRest(Clip) ? 0.00035 : -0.0018)));
            if (current.Moving)
            {
                if(IsCrabBounce)
                {
                    CrabMotionSample bounce=CrabMotion.At(Age,current.CrabHops);
                    X=crabStartX+(current.TargetX-crabStartX)*bounce.Progress;VisualLift=bounce.Lift;
                }
                else
                {
                    double distance=current.TargetX-X;X+=Math.Sign(distance)*Math.Min(Math.Abs(distance),current.Speed*dt);
                    if (Math.Abs(current.TargetX-X)<.5) Age=Duration;
                }
            }
            if (IsAirborne)
            {
                double t=Math.Min(1,Age/Duration);if(Clip.StartsWith("jump"))t=Math.Max(0,Math.Min(1,(t-.22)/.55));double smooth=t*t*(3-2*t);
                X=sourceX+(landingX-sourceX)*smooth;FootY=sourceY+(landingY-sourceY)*smooth-flightArc*Math.Sin(Math.PI*t);
                if (Age>=Duration)
                {
                    X=landingX;FootY=landingY;PerchedWindow=landingWindow;
                    WindowSurface landed=windows.Find(w=>w.Id==PerchedWindow);
                    perchedBounds=landed==null?Rectangle.Empty:landed.Bounds;
                    if(Clip.StartsWith("pounce"))
                    {
                        // A click on open desktop is a brief visit, then return to the floor.
                        if(FootY<Area.Bottom-20){sourceX=X;sourceY=FootY;landingX=X;landingY=Area.Bottom;landingWindow=0;flightArc=25;Begin(new StepPlan(JumpClip(X),1.5),"Returning to a comfortable spot");return;}
                    }
                    Begin(new StepPlan("sit",Range(35,90)),"Enjoying the view");return;
                }
            }
            else Clamp();
            if(ViewerAcknowledgement || !WatchEnabled || !IsRest(Clip) || IsSleeping(Clip)){AttentionActive=false;attentionPending=false;}
            if (attentionPending && Clock>=attentionDue)
            {
                AttentionTarget=pendingAttention;AttentionUntil=Clock+Range(18,25);AttentionActive=true;attentionPending=false;
            }
            if(AttentionActive)
            {
                if(!movingAttention)AttentionTarget=pointer;
                if(Clock>=AttentionUntil || !IsRest(Clip)){AttentionActive=false;attentionPending=false;attentionRestUntil=Clock+Range(25,45);lookCooldown=Math.Max(lookCooldown,attentionRestUntil);}
            }
            if(IsRest(Clip) && !IsSleeping(Clip))
            {
                double dx=pointer.X-previousPointer.X,dy=pointer.Y-previousPointer.Y;
                if(dx*dx+dy*dy>500 && Math.Abs(pointer.X-X)<850 && Clock>=lookCooldown)Notice(pointer,false);
            }
            previousPointer=pointer;
            if(Age>=Duration && !IsAirborne)AdvancePlan("Following the relaxed routine");
        }
        public void MoveHome(Rectangle area,double x,double floor)
        {
            Area=area;PerchedWindow=0;plans.Clear();X=x;FootY=floor;Rest("sit",Range(45,100));Clamp();
        }
    }
}
