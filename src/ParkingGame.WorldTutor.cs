using Godot;
namespace ParkingTrainer;

// Cues drawn in the world rather than on panels: checkpoints where the reference
// solution changes gear or steering, predicted wheel tracks for the current
// steering, and a clearance tag at the nearest obstacle point.
public partial class ParkingGame
{
    internal bool WorldCues=true;
    Node3D worldCues=null!;
    MeshInstance3D checkpointMesh=null!,checkpointPost=null!,trackMesh=null!,clearanceDot=null!;
    Label3D checkpointLabel=null!,clearanceLabel=null!;
    StandardMaterial3D checkpointMaterial=null!;
    internal readonly List<(int Index,Pose Pose,string Text)> Checkpoints=new();
    List<PathStep>? checkpointSource;
    internal int ActiveCheckpoint=-1;
    internal double NearestClearance=double.PositiveInfinity;
    readonly StandardMaterial3D?[] clearanceMaterials=new StandardMaterial3D?[3];
    double cueClock,lastTrackSteer=double.NaN;Pose lastTrackPose;Gear lastTrackGear;
    void SetupWorldCues()
    {
        worldCues=new Node3D {Name="WorldCues"};AddChild(worldCues);
        checkpointMaterial=WorldFactory.Material(new Color(.37f,.84f,.94f,.9f),true);
        checkpointMaterial.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;checkpointMaterial.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        checkpointMesh=new MeshInstance3D {Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,MaterialOverride=checkpointMaterial};worldCues.AddChild(checkpointMesh);
        checkpointPost=new MeshInstance3D {Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off,MaterialOverride=checkpointMaterial,Mesh=new CylinderMesh {TopRadius=.035f,BottomRadius=.035f,Height=1.5f}};worldCues.AddChild(checkpointPost);
        trackMesh=new MeshInstance3D {Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};worldCues.AddChild(trackMesh);
        // Fixed on-screen size keeps distant checkpoints readable.
        Label3D Label(Color color)=>new() {FontSize=44,PixelSize=.0011f,FixedSize=true,Modulate=color,Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,NoDepthTest=true,
            OutlineSize=12,OutlineModulate=new("14262b"),Layers=1,HorizontalAlignment=HorizontalAlignment.Center};
        checkpointLabel=Label(new("dff8ff"));worldCues.AddChild(checkpointLabel);
        clearanceLabel=Label(Colors.White);clearanceLabel.FontSize=40;worldCues.AddChild(clearanceLabel);
        clearanceDot=new MeshInstance3D {Mesh=new SphereMesh {Radius=.06f,Height=.12f},Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};worldCues.AddChild(clearanceDot);
    }
    // Key poses of a route: wherever the gear or the steering changes, plus its end.
    internal static List<(int Index,Pose Pose,string Text)> KeyPoses(List<PathStep> path,int from,Func<string,string> tr,Func<double,string> wheel)
    {
        var result=new List<(int Index,Pose Pose,string Text)>();
        for(int i=Math.Max(1,from);i+1<path.Count;i++)
        {
            int direction=Math.Sign(path[i].Distance),next=Math.Sign(path[i+1].Distance);
            bool gear=next!=0 && direction!=0 && next!=direction,steer=Math.Abs(path[i+1].Steering-path[i].Steering)>.05;
            if(!gear && !steer) continue;
            string text=gear?string.Format(tr("Stop · select {0}\nwheel {1}"),next>0?"D":"R",wheel(path[i+1].Steering)):string.Format(tr("Now: wheel {0}"),wheel(path[i+1].Steering));
            // Arcs traced in short steps can split one change into neighbours; keep the first.
            if(result.Count>0 && Distance(result[^1].Pose,path[i].Pose)<.3) {if(gear) result[^1]=(result[^1].Index,result[^1].Pose,text);continue;}
            result.Add((i,path[i].Pose,text));
        }
        if(path.Count>1) result.Add((path.Count-1,path[^1].Pose,tr("Stop here · P + parking brake")));
        return result;
    }
    void RebuildCheckpoints()
    {
        Checkpoints.Clear();
        if(FixPlan!=null)
        {
            var plan=new List<PathStep> {new(fixPlannedFrom,0,0)};plan.AddRange(FixPlan);
            Checkpoints.AddRange(KeyPoses(plan,1,s=>Tr(s),SteerWord));
        }
        else
        {
            int from=Scene.Drill==Drill.Park?Scene.ApproachEnd:1;
            // The drive-up is the learner's choice: its end is a zone, not an exact spot.
            if(Scene.Drill==Drill.Park && Scene.ApproachEnd>0 && Scene.ApproachEnd+1<Scene.Path.Count)
            {
                int next=Math.Sign(Scene.Path[Scene.ApproachEnd+1].Distance);
                Checkpoints.Add((Scene.ApproachEnd,Scene.SetupPose,string.Format(Tr("Stop around here\nthen select {0}"),next>0?"D":"R")));
                from=Scene.ApproachEnd+1;
            }
            foreach(var key in KeyPoses(Scene.Path,from,s=>Tr(s),SteerWord)) if(Checkpoints.Count==0 || key.Index>Checkpoints[^1].Index) Checkpoints.Add(key);
        }
        checkpointSource=FixPlan;ActiveCheckpoint=-1;
    }
    void UpdateWorldCues(double delta)
    {
        cueClock+=delta;
        bool show=WorldCues && Mode!=TrainingMode.Test && !Paused && !Inspection && !Demo;
        if(checkpointSource!=FixPlan || Checkpoints.Count==0) RebuildCheckpoints();
        UpdateCheckpoint(show);
        UpdateTracks(show && Mode==TrainingMode.Guided && Car.Gear is Gear.D or Gear.R);
        UpdateClearance(show && Car.Gear!=Gear.P && Mode==TrainingMode.Guided);
    }
    void UpdateCheckpoint(bool show)
    {
        int progress;double offLine;
        if(FixPlan!=null)
        {
            progress=0;double best=double.MaxValue;
            for(int i=0;i<FixPlan.Count;i++) {double d=Distance(FixPlan[i].Pose,Car.Pose);if(d<best) {best=d;progress=i+1;}}
            offLine=0;
        }
        else {progress=Scene.ClosestPath(Car.Pose);offLine=Distance(Scene.Path[progress].Pose,Car.Pose);}
        int active=Checkpoints.FindIndex(c=>c.Index>progress+2);
        // Far off the reference line its checkpoints no longer fit; rely on the other cues.
        bool visible=show && active>=0 && offLine<1.2;
        if(visible && active!=ActiveCheckpoint) DrawCheckpoint(Checkpoints[active].Pose,active==0&&Scene.Drill==Drill.Park&&FixPlan==null&&Checkpoints[0].Index==Scene.ApproachEnd);
        ActiveCheckpoint=visible?active:-1;
        checkpointMesh.Visible=checkpointPost.Visible=visible;
        checkpointLabel.Visible=visible && Mode==TrainingMode.Guided;
        if(!visible) return;
        var c=Checkpoints[active];var centre=c.Pose.Point(0,CarSpec.CentreZ);
        checkpointLabel.Text=c.Text;checkpointLabel.Position=new((float)centre.X,1.75f,(float)centre.Z);
        checkpointPost.Position=new((float)centre.X,.75f,(float)centre.Z);
        checkpointMaterial.AlbedoColor=new Color(.37f,.84f,.94f,.55f+.35f*Mathf.Sin((float)cueClock*4));
    }
    void DrawCheckpoint(Pose pose,bool zone)
    {
        var mesh=new ImmediateMesh();mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
        void Ribbon(Vector3 a,Vector3 b,float width)
        {
            var side=(b-a).Cross(Vector3.Up).Normalized()*width/2;
            foreach(var v in new[]{a-side,a+side,b+side,a-side,b+side,b-side}) mesh.SurfaceAddVertex(v);
        }
        Vector3 V((double X,double Z) p,float y=.04f)=>new((float)p.X,y,(float)p.Z);
        var centre=pose.Point(0,CarSpec.CentreZ);float radius=zone?1.6f:.45f;
        for(int i=0;i<32;i++)
        {
            float a=Mathf.Tau*i/32,b=Mathf.Tau*(i+1)/32;
            Ribbon(V(centre)+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,V(centre)+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius,.07f);
        }
        if(!zone)
        {
            // Ghost outline of the car at the checkpoint, with a front marker.
            var corners=DrivingModel.Body(pose).Corners();
            for(int i=0;i<4;i++) Ribbon(V(corners[i]),V(corners[(i+1)%4]),.05f);
            Ribbon(V(pose.Point(0,CarSpec.CentreZ)),V(pose.Point(0,-CarSpec.Front+.3)),.05f);
        }
        mesh.SurfaceEnd();checkpointMesh.Mesh=mesh;
    }
    // Where each wheel and front corner goes over the next three metres with the
    // current steering: shows the rear cutting in and the nose swinging out.
    void UpdateTracks(bool show)
    {
        trackMesh.Visible=show;
        if(!show || (lastTrackSteer==Car.Steer && lastTrackGear==Car.Gear && Distance(lastTrackPose,Car.Pose)<.02 && Math.Abs(lastTrackPose.Yaw-Car.Pose.Yaw)<.005)) return;
        lastTrackSteer=Car.Steer;lastTrackGear=Car.Gear;lastTrackPose=Car.Pose;
        var mesh=new ImmediateMesh();
        var mat=WorldFactory.Material(Colors.White,true);mat.VertexColorUseAsAlbedo=true;mat.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles,mat);
        int direction=Car.Gear==Gear.R?-1:1;
        var points=new (double X,double Z,Color Color)[]{(-.76,0,new(1,.84f,.27f,.8f)),(.76,0,new(1,.84f,.27f,.8f)),(-.76,-CarSpec.Wheelbase,new(1,1,1,.8f)),(.76,-CarSpec.Wheelbase,new(1,1,1,.8f)),
            (-CarSpec.Width/2,-CarSpec.Front,new(1,.42f,.35f,.6f)),(CarSpec.Width/2,-CarSpec.Front,new(1,.42f,.35f,.6f))};
        foreach(var point in points)
        {
            Vector3 At(double distance) {var p=Car.Pose.Advance(direction*distance,Car.Steer).Point(point.X,point.Z);return new((float)p.X,.03f,(float)p.Z);}
            for(double d=0;d<3;d+=.15)
            {
                var a=At(d);var b=At(d+.15);var side=(b-a).Cross(Vector3.Up).Normalized()*.025f;
                var color=point.Color;color.A*=1-(float)(d/3);
                foreach(var v in new[]{a-side,a+side,b+side,a-side,b+side,b-side}) {mesh.SurfaceSetColor(color);mesh.SurfaceAddVertex(v);}
            }
        }
        mesh.SurfaceEnd();trackMesh.Mesh=mesh;
    }
    // Clearance from the body or a mirror to the nearest obstacle point, including
    // curbs and low objects that the sensors ignore.
    internal static (double Distance,(double X,double Z) Point) Nearest(Pose pose,IReadOnlyList<Obstacle> obstacles,double range)
    {
        double c=Math.Cos(pose.Yaw),s=Math.Sin(pose.Yaw);
        double Body(double x,double z)
        {
            double dx=x-pose.X,dz=z-pose.Z,lx=c*dx-s*dz,lz=s*dx+c*dz;
            double Box(double minX,double maxX,double minZ,double maxZ)=>Math.Sqrt(Math.Pow(Math.Max(0,Math.Max(minX-lx,lx-maxX)),2)+Math.Pow(Math.Max(0,Math.Max(minZ-lz,lz-maxZ)),2));
            double mirror=Math.Min(Box(-CarSpec.MirrorX-CarSpec.MirrorWidth/2,-CarSpec.MirrorX+CarSpec.MirrorWidth/2,CarSpec.MirrorZ-CarSpec.MirrorLength/2,CarSpec.MirrorZ+CarSpec.MirrorLength/2),
                Box(CarSpec.MirrorX-CarSpec.MirrorWidth/2,CarSpec.MirrorX+CarSpec.MirrorWidth/2,CarSpec.MirrorZ-CarSpec.MirrorLength/2,CarSpec.MirrorZ+CarSpec.MirrorLength/2));
            return Math.Min(Box(-CarSpec.Width/2,CarSpec.Width/2,-CarSpec.Front,CarSpec.Rear),mirror);
        }
        var centre=pose.Point(0,CarSpec.CentreZ);
        (double Distance,(double X,double Z) Point) best=(double.PositiveInfinity,(0,0));
        foreach(var o in obstacles)
        {
            // Skip obstacles that cannot be within range of the car.
            double reach=Math.Sqrt(o.Shape.Width*o.Shape.Width+o.Shape.Length*o.Shape.Length)/2+CarSpec.Length/2+range+.3;
            if(Math.Abs(o.Shape.X-centre.X)>reach || Math.Abs(o.Shape.Z-centre.Z)>reach) continue;
            var corners=o.Shape.Corners();
            for(int i=0;i<4;i++)
            {
                var a=corners[i];var b=corners[(i+1)%4];double ex=b.X-a.X,ez=b.Z-a.Z,length=Math.Sqrt(ex*ex+ez*ez);
                if(length<1e-9) continue;
                // Sample only the part of long edges (curbs) near the car.
                double t0=((centre.X-a.X)*ex+(centre.Z-a.Z)*ez)/length;
                for(double t=Math.Max(0,t0-3.5);t<=Math.Min(length,t0+3.5);t+=.05)
                {
                    double x=a.X+ex*t/length,z=a.Z+ez*t/length,d=Body(x,z);
                    if(d<best.Distance) best=(d,(x,z));
                }
            }
        }
        return best.Distance<range?best:(double.PositiveInfinity,(0,0));
    }
    void UpdateClearance(bool show)
    {
        var nearest=Nearest(Car.Pose,Scene.Obstacles,1.0);
        NearestClearance=nearest.Distance;
        bool visible=show && double.IsFinite(nearest.Distance);
        clearanceLabel.Visible=clearanceDot.Visible=visible;
        if(!visible) return;
        var color=nearest.Distance<.3?new Color("ff6b57"):nearest.Distance<.6?new Color("ffb340"):new Color("cff59f");
        var p=new Vector3((float)nearest.Point.X,.45f,(float)nearest.Point.Z);
        clearanceLabel.Text=$"{nearest.Distance:0.00} m";clearanceLabel.Modulate=color;clearanceLabel.Position=p+new Vector3(0,.25f,0);
        clearanceDot.Position=p;
        int level=nearest.Distance<.3?0:nearest.Distance<.6?1:2;
        clearanceMaterials[level]??=WorldFactory.Material(color,true);clearanceDot.MaterialOverride=clearanceMaterials[level];
    }
}
