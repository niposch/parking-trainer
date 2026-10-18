using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal SubViewport rearViewport=null!,surroundViewport=null!,mapViewport=null!;
    internal SubViewport[] mirrorViewports=new SubViewport[3];
    Camera3D rearCamera=null!,mapCamera=null!;
    Camera3D[] mirrorCameras=new Camera3D[3],surroundCameras=new Camera3D[4];
    SubViewport[] surroundSources=new SubViewport[4];
    MeshInstance3D guideMesh=null!;
    Label3D landmark=null!;
    internal ShaderMaterial surroundMaterial=null!;
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
        for(int i=0;i<4;i++) {surroundSources[i]=View(380,380,out surroundCameras[i],1);surroundCameras[i].Fov=i<2?135:140;}
        mapViewport=View(700,580,out mapCamera,35);mapCamera.Projection=Camera3D.ProjectionType.Orthogonal;mapCamera.Size=25;
        mapCamera.Position=new(1,28,0);mapCamera.RotationDegrees=new(-90,0,0);
        // Ground-plane reconstruction from four finite, vehicle-mounted cameras.
        // Raised objects stretch and coverage gaps remain dark, as in a camera composite.
        surroundMaterial=new ShaderMaterial {Shader=new Shader {Code=@"
shader_type canvas_item;
uniform sampler2D image0:filter_linear; uniform sampler2D image1:filter_linear;
uniform sampler2D image2:filter_linear; uniform sampler2D image3:filter_linear;
uniform mat4 view0; uniform mat4 view1; uniform mat4 view2; uniform mat4 view3;
uniform mat4 proj0; uniform mat4 proj1; uniform mat4 proj2; uniform mat4 proj3;
vec4 camera_sample(sampler2D img,mat4 proj,mat4 view,vec3 p){
    vec4 clip=proj*view*vec4(p,1.0);
    vec2 uv=vec2(clip.x,-clip.y)/max(clip.w,0.001)*0.5+0.5;
    if(clip.w<0.0 || uv.x<0.01 || uv.x>0.99 || uv.y<0.01 || uv.y>0.99) return vec4(0.0);
    return vec4(texture(img,uv).rgb,1.0);
}
void fragment(){
    vec3 p=vec3((UV.x-0.5)*8.0,0.015,mix(-5.325,2.675,UV.y));
    vec4 a=camera_sample(image0,proj0,view0,p),b=camera_sample(image1,proj1,view1,p);
    vec4 c=camera_sample(image2,proj2,view2,p),d=camera_sample(image3,proj3,view3,p);
    float front=max(0.0,-p.z-1.325),back=max(0.0,p.z+1.325);
    vec4 w=vec4(front,back,max(0.0,-p.x),max(0.0,p.x));
    w=pow(w,vec4(8.0))*vec4(a.a,b.a,c.a,d.a);
    float total=dot(w,vec4(1.0));
    vec3 color=total>0.00001?(a.rgb*w.x+b.rgb*w.y+c.rgb*w.z+d.rgb*w.w)/total:vec3(0.12,0.16,0.18);
    if(abs(p.x)<0.90 && p.z>-3.55 && p.z<0.90){
        color=vec3(0.83,0.88,0.86);
        if(abs(p.x)<0.68 && p.z>-2.5 && p.z<0.05) color=vec3(0.22,0.29,0.31);
        if(abs(p.x)<0.65 && p.z>-2.05 && p.z<-0.40) color=vec3(0.74,0.81,0.79);
        if(abs(p.x)>0.55 && p.z<-3.25) color=vec3(0.95,0.90,0.62);
        if(abs(p.x)>0.55 && p.z>0.70) color=vec3(0.8,0.19,0.13);
    }
    COLOR=vec4(color,1.0);
}"}};
        for(int i=0;i<4;i++) surroundMaterial.SetShaderParameter($"image{i}",surroundSources[i].GetTexture());
        guideMesh=new MeshInstance3D {Layers=4,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};vehicle.AddChild(guideMesh);
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
        // Bumper and underside-of-mirror mounts, calibrated for the nearby apron.
        Vector3[] pos={new(0,.85f,-3.58f),new(.04f,1.10f,.925f),new(-(float)CarSpec.MirrorX,.99f,(float)CarSpec.MirrorZ+.08f),new((float)CarSpec.MirrorX,.99f,(float)CarSpec.MirrorZ+.08f)};
        float[] yaw={0,Mathf.Pi,Mathf.Pi/2,-Mathf.Pi/2};
        for(int i=0;i<4;i++)
        {
            var local=new Transform3D(Basis.FromEuler(new(-.96f,yaw[i],0)),pos[i]);
            surroundCameras[i].GlobalTransform=vehicle.GlobalTransform*local;
            surroundMaterial.SetShaderParameter($"view{i}",local.AffineInverse());
            surroundMaterial.SetShaderParameter($"proj{i}",surroundCameras[i].GetCameraProjection());
            surroundSources[i].RenderTargetUpdateMode=CameraMode==1&&!Paused?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;
        }
        mapViewport.RenderTargetUpdateMode=mapRect.IsVisibleInTree()?SubViewport.UpdateMode.Always:SubViewport.UpdateMode.Disabled;
        UpdateGuides();
        var reference=Scene.Obstacles.First(o=>!o.Curb);
        var corner=new Pose(reference.Shape.X,reference.Shape.Z,reference.Shape.Yaw).Point((Scene.Mirrored?1:-1)*(reference.Shape.Width/2-.09),reference.Shape.Length/2+.025);
        landmark.Position=new((float)corner.X,.68f,(float)corner.Z);landmark.Visible=Tutor&&!Inspection;
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
