using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal float BeeperVolume=1;
    readonly ParkingAlarm alarm=new();
    AudioStreamGeneratorPlayback alarmPlayback=null!;
    readonly Vector2[] alarmBuffer=new Vector2[1024];
    void SetupSensorAudio()
    {
        beep.PlaybackType=AudioServer.PlaybackType.Stream;
        beep.Stream=new AudioStreamGenerator {MixRate=ParkingAlarm.SampleRate,BufferLength=.12f};
        beep.Play();
        alarmPlayback=(AudioStreamGeneratorPlayback)beep.GetStreamPlayback();
        FillAlarmBuffer();
    }
    void FillAlarmBuffer()
    {
        if(alarmPlayback==null) return;
        int available=alarmPlayback.GetFramesAvailable();
        while(available>0)
        {
            int count=Math.Min(available,alarmBuffer.Length);
            for(int i=0;i<count;i++) {float sample=alarm.NextSample();alarmBuffer[i]=new(sample,sample);}
            alarmPlayback.PushBuffer(alarmBuffer.AsSpan(0,count));
            available-=count;
        }
    }
    void Sound(double dt)
    {
        alarm.Configure(FrontRange,RearRange,!Muted&&!Paused&&Car.Gear!=Gear.P,BeeperVolume);
        FillAlarmBuffer();
    }
}
