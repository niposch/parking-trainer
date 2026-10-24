namespace ParkingTrainer;

internal static class AlarmChecks
{
    internal static int Run(Action<bool,string> check)
    {
        int count=0;
        void Check(bool ok,string label) {check(ok,label);count++;}
        var tone=new ParkingAlarm();float previous=0;double maximumJump=0,peak=0;
        var stages=new[]{(.7,2.0,true,1.0),(.25,2.0,true,1.0),(.35,2.0,true,1.0),(2.0,.24,true,1.0),
            (2.0,.75,true,1.0),(2.0,.28,true,1.0),(2.0,.28,false,1.0),(2.0,.28,true,1.0),
            (.25,2.0,true,.1),(.25,2.0,true,1.0),(2.0,2.0,true,1.0)};
        foreach(var (front,rear,enabled,volume) in stages)
        {
            tone.Configure(front,rear,enabled,volume);
            // Off-grid stage lengths exercise changes away from zero crossings.
            for(int i=0;i<4937;i++)
            {
                float sample=tone.NextSample();peak=Math.Max(peak,Math.Abs(sample));
                maximumJump=Math.Max(maximumJump,Math.Abs(sample-previous));previous=sample;
            }
        }
        Check(peak>.12 && peak<=ParkingAlarm.Peak+1e-7,"Alarm preserves its quiet PCM peak and digital headroom");
        Check(maximumJump<.026,"Pulse/continuous, front/rear, mute and volume transitions have no abrupt waveform step");
        Check(previous==0,"Warning releases to exact silence after leaving sensor range");
        foreach(bool front in new[]{false,true})
        {
            tone.Configure(front?.2:2,front?2:.2,true,1);
            for(int i=0;i<ParkingAlarm.SampleRate/10;i++) tone.NextSample();
            int crossings=0;previous=tone.NextSample();
            for(int i=0;i<ParkingAlarm.SampleRate;i++) {float sample=tone.NextSample();if(previous<0 && sample>=0) crossings++;previous=sample;}
            Check(Math.Abs(crossings-(front?325:250))<=1,"Continuous warning uses a softer, distinct front/rear pitch");
        }
        tone.Configure(2,.29,true,1);tone.Configure(2,.33,true,1);Check(tone.Continuous,"Warning retains red-zone release hysteresis");
        tone.Configure(2,.35,true,1);Check(!tone.Continuous,"Warning releases red after sufficient clearance");
        return count;
    }
}
