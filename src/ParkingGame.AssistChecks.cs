using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    async System.Threading.Tasks.Task<int> CheckParkingAssist()
    {
        int passed=0;
        void Check(bool ok,string name) {if(!ok) throw new InvalidOperationException(name);passed++;GD.Print("ASSIST PASS: "+name);}
        NewScenario(false);ApplyPose();UpdateViews();
        foreach(int i in new[]{0,2})
        {
            var arm=mirrorArms[i]!;var box=arm.GetAabb();
            var corners=Enumerable.Range(0,8).Select(k=>mirrorScreens[i].ToLocal(arm.ToGlobal(box.GetEndpoint(k)))).ToArray();
            Check(corners.All(p=>p.Z<.010f),$"Mirror {i} holder stays behind the reflective surface");
            var housing=mirrorScreens[i].GetChild<MeshInstance3D>(0);
            var envelope=DrivingModel.Mirror(new(0,0,0),i==0?-1:1);
            var housingCorners=Enumerable.Range(0,8).Select(k=>vehicle.ToLocal(housing.ToGlobal(housing.GetAabb().GetEndpoint(k))));
            Check(housingCorners.All(p=>Math.Abs(p.X-envelope.X)<=envelope.Width/2 && Math.Abs(p.Z-envelope.Z)<=envelope.Length/2),$"Mirror {i} housing fits inside the collision and sensor envelope");
            var tip=mirrorScreens[i].ToLocal(arm.ToGlobal(new(0,0,-box.Size.Z/2)));
            Check(Math.Abs(tip.Z+.008f)<.0001 && Math.Abs(tip.Y+.05f)<.0001 && Math.Abs(tip.X)<.0001,$"Mirror {i} holder enters the back of its housing without a gap");
        }
        for(int i=0;i<3;i++)
        {
            var size=mirrorViewports[i].Size;
            Check(Math.Abs((float)size.X/size.Y-mirrorSizes[i].X/mirrorSizes[i].Y)<.001,$"Mirror {i} reflection matches its glass aspect ratio");
            var plane=mirrorCameras[i].ToLocal(vehicle.ToGlobal(mirrorCentres[i]));
            Check(Math.Abs(mirrorCameras[i].Near+plane.Z-.002f)<.0001,$"Mirror {i} clips at its glass plane, excluding intervening cabin geometry");
        }
        sensorGraphic.SetRanges(new[]{2.0,.25,.8},new[]{.5,1.2,2.0});
        sensorGraphic.SetRanges(new[]{2.0,.31,.8},new[]{.5,1.2,2.0});
        Check(sensorGraphic.Level(1)==4,"Sensor release hysteresis prevents flickering at a threshold");
        sensorGraphic.SetRanges(new[]{2.0,.35,.8},new[]{.5,1.2,2.0});
        Check(sensorGraphic.Level(1)==3,"Sensor display releases after four centimetres of extra clearance");
        sensorGraphic.SetRanges(new[]{2.0,.29,.8},new[]{.5,1.2,2.0});
        Check(sensorGraphic.Level(1)==4,"Approaching danger changes the sensor display immediately");
        Pause(false);Car.Pose=new(0,0,0);Car.Gear=Gear.R;Car.ParkingBrake=true;
        Scene.Obstacles.Clear();
        Scene.Obstacles.Add(new(new(-(CarSpec.MirrorX+CarSpec.MirrorWidth/2+.03),CarSpec.MirrorZ,.02,.02,0),"test"));
        Scene.Obstacles.Add(new(new(-1.01,CarSpec.Rear+.11,.02,.02,0),"test"));
        Scene.Obstacles.Add(new(new(0,CarSpec.Rear+.10,8,.10,0),"curb",.15,true));
        Muted=false;BeeperVolume=1;
        await PhysicsFrames(4);
        Check(Math.Abs(FrontSectors[0]-.02)<1e-8 && Math.Abs(RearSectors[0]-Math.Sqrt(.02))<1e-8,"Mirror and diagonal rear-corner clearances reach the running game");
        Check(FrontSectors[2]==2 && RearSectors[2]==2,"Opposite-side indicators stay clear and ignore the low curb");
        Check(sensorGraphic.Level(0)==4 && sensorGraphic.Level(3)==4 && sensorGraphic.Level(2)==0 && sensorGraphic.Level(5)==0,"Side and corner dangers activate the corresponding console zones");
        Check(beep.Stream==sensorContinuous[0] && beep.Playing,"Side danger activates the continuous distance warning before contact");
        NewScenario(false);
        return passed;
    }
}
