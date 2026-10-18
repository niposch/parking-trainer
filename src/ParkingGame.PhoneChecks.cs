using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    async void RunPhoneCheck()
    {
        try {int count=await CheckPhoneFixes();GD.Print($"PHONE COMPLETE: {count} checks");GetTree().Quit();}
        catch(Exception error) {GD.PushError("PHONE FAILED: "+error);GetTree().Quit(1);}
    }
    async System.Threading.Tasks.Task<int> CheckPhoneFixes()
    {
        int passed=0;
        void Check(bool ok,string name) {if(!ok) throw new InvalidOperationException(name);passed++;GD.Print("PHONE PASS: "+name);}
        if(DisplayServer.GetName()=="headless") {GetWindow().Size=new(1280,800);LayoutHud();}
        NewScenario(false);ShowMenuPage("options");
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        menuScroll.ScrollVertical=0;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        var point=steeringButton.GetGlobalRect().GetCenter();bool steeringBefore=RateSteering;
        int before=menuScroll.ScrollVertical;
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=true});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        for(int step=0;step<4;step++)
        {
            point.Y-=30*InterfaceLayout.Unit(GetViewport());
            Input.ParseInputEvent(new InputEventScreenDrag {Index=0,Position=point,Relative=new(0,-30*InterfaceLayout.Unit(GetViewport()))});
            await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        }
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=false});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(menuScroll.ScrollVertical>before+Math.Min(40,menuScroll.GetVScrollBar().MaxValue-menuScroll.GetVScrollBar().Page)*.5,"Swiping on a menu button scrolls the actual container");
        Check(Paused && MenuPage=="options" && RateSteering==steeringBefore,"A menu swipe does not activate the button under the finger");
        menuScroll.ScrollVertical=0;
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        point=steeringButton.GetGlobalRect().GetCenter();
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=true});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Input.ParseInputEvent(new InputEventScreenTouch {Index=0,Position=point,Pressed=false});
        await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
        Check(RateSteering!=steeringBefore,"Normal menu taps still activate their button after a swipe");
        RateSteering=steeringBefore;
        Check(beep.PlaybackType==AudioServer.PlaybackType.Stream,"Alarm uses the stable streamed mixer on native and web");
        foreach(var tone in sensorContinuous)
        {
            var data=tone.Data;var samples=new short[data.Length/2];
            for(int i=0;i<samples.Length;i++) samples[i]=(short)(data[i*2]|data[i*2+1]<<8);
            int peak=samples.Max(v=>Math.Abs((int)v));
            int maxStep=samples.Zip(samples.Skip(1),(a,b)=>Math.Abs(a-b)).Max();
            Check(peak>0 && peak<32768*.14,"Continuous tone retains more than 17 dB of digital headroom");
            Check(Math.Abs(samples[0]-samples[^1])<=maxStep+1,"Continuous tone has no discontinuity at the loop seam");
        }
        Paused=false;Car.Gear=Gear.R;Car.ParkingBrake=true;Muted=false;BeeperVolume=1;FrontRange=2;RearRange=.29;Sound(.01);
        var playback=beep.GetStreamPlayback().GetInstanceId();
        for(int i=0;i<100;i++) {RearRange=i%2==0?.31:.33;Sound(.01);}
        Check(beep.Stream==sensorContinuous[1] && beep.GetStreamPlayback().GetInstanceId()==playback,"Red-zone jitter reuses one continuous playback without clicks from restarts");
        RearRange=.35;Sound(.01);
        Check(beep.Stream==sensorPulses[1],"Continuous warning releases into pulses after four centimetres of clearance");
        ApplyPose();UpdateViews();
        bool Covered(Vector3 point3)=>surroundCameras.Select((cam,i)=>(Cam:cam,Index:i)).Any(pair=>
        {
            if(pair.Index==0 && point3.Z>=CarSpec.CentreZ || pair.Index==1 && point3.Z<=CarSpec.CentreZ || pair.Index==2 && point3.X>=0 || pair.Index==3 && point3.X<=0) return false;
            var world=vehicle.ToGlobal(point3);var pixel=pair.Cam.UnprojectPosition(world);
            return !pair.Cam.IsPositionBehind(world) && pixel.X>3.8 && pixel.Y>3.8 && pixel.X<376.2 && pixel.Y<376.2;
        });
        int uncovered=0;
        for(int x=0;x<=24;x++) for(int z=0;z<=24;z++)
        {
            var point3=new Vector3(-4+x/3f,.015f,(float)CarSpec.CentreZ-4+z/3f);
            if(Math.Abs(point3.X)<.91 && point3.Z>-CarSpec.Front && point3.Z<CarSpec.Rear) continue;
            if(!Covered(point3)) uncovered++;
        }
        foreach(int side in new[]{-1,1}) for(int i=0;i<=100;i++)
        {
            if(!Covered(new(side*.92f,.015f,-3.57f+4.49f*i/100))) uncovered++;
            if(!Covered(new(-.92f+1.84f*i/100,.015f,side==1?.92f:-3.57f))) uncovered++;
        }
        Check(uncovered==0,$"Surround cameras cover the complete nearby ground apron without gaps ({uncovered} missing samples)");
        Check(surroundCameras.All(c=>c.CullMask==1),"Camera composite excludes duplicate player geometry outside the calibrated car silhouette");
        NewScenario(false);
        return passed;
    }
}
