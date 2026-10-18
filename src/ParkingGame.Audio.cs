using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal float BeeperVolume=1;
    readonly AudioStreamWav[] sensorPulses=new AudioStreamWav[2],sensorContinuous=new AudioStreamWav[2];
    bool beepFront;
    void SetupSensorAudio()
    {
        for(int channel=0;channel<2;channel++)
        {
            int frequency=channel==0?650:500;
            string side=channel==0?"front":"rear";
            sensorPulses[channel]=IsWeb?GD.Load<AudioStreamWav>($"res://assets/audio/sensor-{side}-pulse.wav"):SensorTone(frequency,.075,false);
            sensorContinuous[channel]=IsWeb?GD.Load<AudioStreamWav>($"res://assets/audio/sensor-{side}-loop.wav"):SensorTone(frequency,.20,true);
            if(IsWeb)
            {
                var tone=sensorContinuous[channel];tone.LoopMode=AudioStreamWav.LoopModeEnum.Forward;
                tone.LoopBegin=0;tone.LoopEnd=(int)(tone.MixRate*.20);
            }
        }
    }
    static AudioStreamWav SensorTone(int frequency,double duration,bool loop)
    {
        const int rate=22050;int count=(int)(rate*duration);var data=new byte[count*2];
        for(int i=0;i<count;i++)
        {
            // Rounded pulse edges avoid the hard clicks of an abruptly gated sine.
            // Loop lengths contain whole periods, so their joins stay continuous.
            double envelope=loop?1:Math.Min(1,Math.Min(i/(rate*.012),(count-1-i)/(rate*.018)));
            short sample=(short)(Math.Sin(i*2*Math.PI*frequency/rate)*4200*envelope);
            data[i*2]=(byte)(sample&255);data[i*2+1]=(byte)((sample>>8)&255);
        }
        return new AudioStreamWav {Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data,
            LoopMode=loop?AudioStreamWav.LoopModeEnum.Forward:AudioStreamWav.LoopModeEnum.Disabled,LoopBegin=0,LoopEnd=count};
    }
    void Sound(double dt)
    {
        beepTime-=dt;double range=Math.Min(FrontRange,RearRange);
        bool continuous=range<.30;
        beep.VolumeDb=BeeperVolume<=0?-80:Mathf.LinearToDb(BeeperVolume);
        if(Muted||Paused||range>=1.6||Car.Gear==Gear.P||BeeperVolume<=0)
        {beep.Stop();beepTime=0;return;}
        // A small dead band avoids pitch chatter between nearly equal ranges.
        if(FrontRange+.08<RearRange) beepFront=true;
        else if(RearRange+.08<FrontRange) beepFront=false;
        int channel=beepFront?0:1;
        if(continuous)
        {
            if(beep.Stream!=sensorContinuous[channel] || !beep.Playing)
            {beep.Stream=sensorContinuous[channel];beep.Play();}
            return;
        }
        if(beep.Stream==sensorContinuous[0]||beep.Stream==sensorContinuous[1]) {beep.Stop();beepTime=0;}
        if(beepTime>0) return;
        beep.Stream=sensorPulses[channel];beep.Play();
        beepTime=.16+.65*Math.Clamp((range-.30)/1.30,0,1);
    }
}
