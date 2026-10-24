namespace ParkingTrainer;

// One sample clock for every warning state. Never splice or restart waveforms.
internal sealed class ParkingAlarm
{
    internal const int SampleRate=22050;
    internal const double Peak=4200.0/32768;
    internal bool Enabled {get;private set;}
    internal bool Continuous {get;private set;}
    internal bool Front {get;private set;}
    internal double PulseInterval {get;private set;}=.81;
    double phase,frequency=500,level,pulseTime,volume;
    bool wasEnabled,wasContinuous;
    internal void Configure(double front,double rear,bool enabled,double gain)
    {
        double range=Math.Min(front,rear);
        if(range<.30) Continuous=true;else if(range>=.34) Continuous=false;
        if(front+.08<rear) Front=true;else if(rear+.08<front) Front=false;
        volume=Math.Clamp(gain,0,1);
        Enabled=enabled && range<1.6 && volume>0;
        if(!Enabled) Continuous=false;
        PulseInterval=.16+.65*Math.Clamp((range-.30)/1.30,0,1);
    }
    internal float NextSample()
    {
        if(Enabled && (!wasEnabled || wasContinuous&&!Continuous)) pulseTime=0;
        bool sounding=Enabled && (Continuous || pulseTime<.075);
        double target=sounding?volume:0;
        // Sample-level 12 ms attack / 18 ms release also smooth mute and volume.
        double step=1.0/(SampleRate*(target>level?.012:.018));
        level+=Math.Clamp(target-level,-step,step);
        // Slew pitch over roughly 30 ms while retaining the oscillator's phase.
        frequency+=( (Continuous?(Front?325:250):(Front?650:500))-frequency )*(1-Math.Exp(-1.0/(SampleRate*.010)));
        float sample=(float)(Math.Sin(phase)*Peak*level);
        phase=(phase+2*Math.PI*frequency/SampleRate)%(2*Math.PI);
        if(Enabled && !Continuous) {pulseTime+=1.0/SampleRate;if(pulseTime>=PulseInterval) pulseTime-=PulseInterval;}
        wasEnabled=Enabled;wasContinuous=Continuous;
        return sample;
    }
}
