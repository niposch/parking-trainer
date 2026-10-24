using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal SubViewport rearViewport=null!,surroundViewport=null!,mapViewport=null!;
    internal SubViewport[] mirrorViewports=new SubViewport[3];
    Camera3D rearCamera=null!,mapCamera=null!;
    Camera3D[] mirrorCameras=new Camera3D[3];
    Camera3D surroundCamera=null!;
    MeshInstance3D guideMesh=null!,fixMesh=null!;
    Label3D landmark=null!;
    internal static ShaderMaterial FlipImage()=>new() { Shader=new Shader {Code="shader_type canvas_item; void fragment(){ COLOR=texture(TEXTURE,vec2(1.0-UV.x,UV.y)); }"} };
    SubViewport View(int width,int height,out Camera3D camera,uint layers=35)
    {
        var viewport=new SubViewport { Size=new(width,height),World3D=GetViewport().World3D,
            RenderTargetUpdateMode=SubViewport.UpdateMode.Always,Msaa3D=Viewport.Msaa.Msaa2X };
        AddChild(viewport);
        camera=new Camera3D {Current=true,Near=.045f,Far=65,CullMask=layers};viewport.AddChild(camera);
        return viewport;
    }
    void SetupViews()
    {
        eye=new Camera3D {Current=true,Near=.035f,Far=75,Fov=Fov,CullMask=27};vehicle.AddChild(eye);
        inspectionCamera=new Camera3D {Near=.08f,Far=75,Fov=60,CullMask=43};vehicle.AddChild(inspectionCamera);
        rearViewport=View(720,420,out rearCamera,39);rearCamera.KeepAspect=Camera3D.KeepAspectEnum.Width;rearCamera.Fov=112;
        for(int i=0;i<3;i++) {mirrorViewports[i]=View(i==1?500:352,i==1?170:216,out mirrorCameras[i]);}
        // Emulate a calibrated surround display with a metric orthographic view.
        // Projecting tall cars onto the road plane caused blind seams/stretching.
        surroundViewport=View(600,600,out surroundCamera,43);
        surroundCamera.Projection=Camera3D.ProjectionType.Orthogonal;surroundCamera.Size=8;
        surroundCamera.KeepAspect=Camera3D.KeepAspectEnum.Height;
        mapViewport=View(700,580,out mapCamera,35);mapCamera.Projection=Camera3D.ProjectionType.Orthogonal;mapCamera.Size=25;
        mapCamera.Position=new(1,28,0);mapCamera.RotationDegrees=new(-90,0,0);
        guideMesh=new MeshInstance3D {Layers=4,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};vehicle.AddChild(guideMesh);
        fixMesh=new MeshInstance3D {Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};AddChild(fixMesh);
        landmark=new Label3D {Text="◉",FontSize=80,PixelSize=.0035f,Modulate=new("d4ff9e"),Billboard=BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest=false,OutlineSize=10,OutlineModulate=new("23332a"),Layers=1};AddChild(landmark);
    }
    void FrameTeachingMap()
    {
        var points=Scene.Obstacles.Where(o=>!o.Curb).SelectMany(o=>o.Shape.Corners())
            .Concat(DrivingModel.Body(Car.Pose).Corners()).Concat(DrivingModel.Body(Scene.Goal).Corners()).ToArray();
        double minX=points.Min(p=>p.X)-2,maxX=points.Max(p=>p.X)+2,minZ=points.Min(p=>p.Z)-2,maxZ=points.Max(p=>p.Z)+2;
        mapCamera.Position=new((float)((minX+maxX)/2),28,(float)((minZ+maxZ)/2));
        mapCamera.KeepAspect=Camera3D.KeepAspectEnum.Height;
        mapCamera.Size=(float)Math.Max(maxZ-minZ,(maxX-minX)*mapViewport.Size.Y/mapViewport.Size.X);
    }
    void UpdateViews(double delta=1.0/120)
    {
        var eyeTarget=PlayerCabin.EyeTarget(LookYaw,EyeHeight);
        eye.Position=eye.Position.Lerp(eyeTarget,1-Mathf.Exp(-(float)delta*10));
        eye.Rotation=new(LookPitch,LookYaw,0);eye.Fov=Fov;
        UpdateInspectionCamera();
        if(autoCapture && captureView.StartsWith("asset"))
        {
            eye.CullMask=43;eye.Fov=60;
            var position=captureView switch {"asset-side"=>new Vector3(5,1.65f,-1.325f),"asset-rear"=>new Vector3(-4,2.5f,4),_=>new Vector3(-4,2.5f,-6)};
            eye.GlobalPosition=vehicle.ToGlobal(position);eye.LookAt(vehicle.ToGlobal(new(0,.8f,-1.325f)));
        }
        rearCamera.GlobalTransform=vehicle.GlobalTransform*new Transform3D(Basis.FromEuler(new(-.45f,Mathf.Pi,0)),new(.04f,1.10f,.72f));
        // Planar virtual-eye mirrors: reflect the eye about each mirror plane, then
        // use off-axis frusta. Side mirrors get a modest convex FOV approximation.
        for(int i=0;i<3;i++)
        {
            var centre=mirrorCentres[i];var toEye=(PlayerCabin.EyeTarget(0,EyeHeight)-centre).Normalized();
            var rearward=new Vector3(i==0?-.26f:i==2?.26f:0,i==1?-.06f:-.03f,1).Normalized();
            var normal=(toEye+rearward).Normalized();
            if(mirrorScreens[i]!=null) mirrorScreens[i].Basis=Basis.LookingAt(normal,Vector3.Up,true);
            UpdateMirrorMount(i);
            var reflected=eye.Position-2*normal*(eye.Position-centre).Dot(normal);
            var cam=mirrorCameras[i];
            cam.GlobalPosition=vehicle.ToGlobal(reflected);
            var n=vehicle.GlobalBasis*normal;
            cam.LookAt(cam.GlobalPosition+n,Vector3.Up);
            var local=cam.GlobalTransform.AffineInverse()*vehicle.ToGlobal(centre);
            float distance=Mathf.Max(.1f,-local.Z),near=distance+.002f;
            // The virtual eye can lie inside the body. Start rendering at the
            // mirror plane so intervening door/roof geometry cannot cover it.
            float height=mirrorSizes[i].Y;
            cam.KeepAspect=Camera3D.KeepAspectEnum.Height;
            cam.SetFrustum(height*near/distance*(i==1?1:1.7f),new Vector2(local.X,local.Y)*near/distance,near,65);
        }
        surroundCamera.GlobalTransform=vehicle.GlobalTransform*new Transform3D(Basis.FromEuler(new(-Mathf.Pi/2,0,0)),new(0,10,(float)CarSpec.CentreZ));
        surroundCamera.CullMask=Car.Gear==Gear.R?47u:43u;
        surroundViewport.RenderTargetUpdateMode=CameraMode==1&&!Paused?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;
        mapViewport.RenderTargetUpdateMode=mapRect.IsVisibleInTree()?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;
        UpdateGuides();
        if(Scene.Landmark is {} corner) landmark.Position=new((float)corner.X,.68f,(float)corner.Z);
        landmark.Visible=Scene.Landmark!=null&&Tutor&&Mode==TrainingMode.Guided&&!Inspection;
    }
    double lastGuide=double.NaN;bool lastDynamic;
    void UpdateGuides()
    {
        if(Math.Abs(lastGuide-Car.Steer)<.0005 && lastDynamic==DynamicGuides) return;
        lastGuide=Car.Steer;lastDynamic=DynamicGuides;
        var mesh=new ImmediateMesh();
        var mat=WorldFactory.Material(Colors.White,true);mat.VertexColorUseAsAlbedo=true;mat.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles,mat);
        void ribbon(Vector3 a,Vector3 b,Color color,float width=.027f)
        {
            var side=(b-a).Cross(Vector3.Up).Normalized()*width/2;
            foreach(var p in new[]{a-side,a+side,b+side,a-side,b+side,b-side}) {mesh.SurfaceSetColor(color);mesh.SurfaceAddVertex(p);}
        }
        double steer=DynamicGuides?Car.Steer:0;
        Vector3 point(double distance,double side)
        {
            var p=new Pose(0,0,0).Advance(-distance,steer).Point(side,CarSpec.Rear);
            return new((float)p.X,.032f,(float)p.Z);
        }
        Color yellow=new("ffd645"),red=new("f54d3c");
        foreach(double side in new[]{-.92,.92})
            for(double d=.05;d<2;d+=.05) ribbon(point(d,side),point(d+.05,side),d<.4?red:yellow);
        foreach(double distance in new[]{.4,1.0,2.0})
            ribbon(point(distance,-.92),point(distance,.92),distance<.5?red:yellow);
        mesh.SurfaceEnd();guideMesh.Mesh=mesh;
    }
}
