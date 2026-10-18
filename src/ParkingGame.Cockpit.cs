using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    Node3D[] mirrorScreens=new Node3D[3];
    MeshInstance3D?[] mirrorArms=new MeshInstance3D?[3];
    ParkingSensors sensorGraphic=null!;
    readonly Vector3[] mirrorCentres={new(-(float)CarSpec.MirrorX,1.055f,(float)CarSpec.MirrorZ),new(0,1.30f,-1.90f),new((float)CarSpec.MirrorX,1.055f,(float)CarSpec.MirrorZ)};
    readonly Vector2[] mirrorSizes={new(.22f,.135f),new(.25f,.085f),new(.22f,.135f)};
    SubViewport FlatDisplay(int width,int height,out Control canvas)
    {
        var view=new SubViewport {Size=new(width,height),Disable3D=true,RenderTargetUpdateMode=SubViewport.UpdateMode.Always};AddChild(view);
        canvas=new ColorRect {Color=new("10191d"),Size=new(width,height),MouseFilter=Control.MouseFilterEnum.Ignore};view.AddChild(canvas);
        return view;
    }
    Node3D Screen(Vector3 position,Vector2 size,Texture2D texture,bool flip=false)
    {
        var mount=new Node3D {Position=position};vehicle.AddChild(mount);
        float border=flip?.014f:.032f,depth=flip?.020f:.035f;
        var frame=WorldFactory.Box(mount,Vector3.Zero,new(size.X+border,size.Y+border,depth),new("202a2d"));
        frame.Layers=flip?8u:2u;if(flip) frame.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
        var material=new StandardMaterial3D {AlbedoTexture=texture,AlbedoColor=Colors.White,
            ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,CullMode=BaseMaterial3D.CullModeEnum.Disabled,TextureFilter=BaseMaterial3D.TextureFilterEnum.Linear};
        if(flip) {material.Uv1Scale=new(-1,1,1);material.Uv1Offset=new(1,0,0);}
        var glass=new MeshInstance3D {Mesh=new QuadMesh {Size=size},MaterialOverride=material,Position=new(0,0,depth/2+.0025f),Layers=16,
            CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};mount.AddChild(glass);
        return mount;
    }
    void SetupCockpitDisplays()
    {
        var cluster=FlatDisplay(640,240,out var instrument);
        speedLabel=Text(instrument,"0.0",104,new("edf2e8"));speedLabel.Position=new(58,8);speedLabel.Size=new(370,142);
        speedLabel.HorizontalAlignment=HorizontalAlignment.Center;
        var unit=Text(instrument,"km/h",28,new("9badb1"));unit.Position=new(340,90);
        gearLabel=Text(instrument,"P",92,new("d0f39f"));gearLabel.Position=new(485,25);
        steerLabel=Text(instrument,"",28,new("f3be78"));steerLabel.Position=new(24,24);
        WorldFactory.Box(vehicle,new(-.38f,.825f,-2.36f),new(.45f,.23f,.16f),new("202a2d")).Layers=2;
        var dash=Screen(new(-.38f,.93f,-2.26f),new(.40f,.16f),cluster.GetTexture());dash.Rotation=new(-.16f,0,0);
        var display=FlatDisplay(720,480,out var console);
        cameraLabel=Text(console,"REAR",24,new("d0f39f"));cameraLabel.Position=new(22,10);
        rearRect=Image(console,rearViewport,true);rearRect.Position=new(0,45);rearRect.Size=new(450,370);rearRect.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
        surroundRect=new TextureRect {Texture=surroundSources[0].GetTexture(),Material=surroundMaterial,
            ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,
            Position=new(0,45),Size=new(450,370),Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};console.AddChild(surroundRect);
        sensorGraphic=new ParkingSensors {Position=new(450,45),Size=new(260,370)};console.AddChild(sensorGraphic);
        sensorLabel=Text(console,"",23,new("e6ece4"));sensorLabel.Position=new(22,435);
        WorldFactory.Box(vehicle,new(.08f,.58f,-2.05f),new(.40f,.35f,.15f),new("202a2d")).Layers=2;
        var media=Screen(new(.08f,.78f,-1.93f),new(.34f,.227f),display.GetTexture());media.Rotation=new(-.13f,-.20f,0);
        // Physical mirror glass is visible only to the driver's camera. Reflection
        // cameras omit the glass layer to prevent recursive render feedback.
        for(int i=0;i<3;i++) mirrorScreens[i]=Screen(mirrorCentres[i],mirrorSizes[i],mirrorViewports[i].GetTexture(),true);
        WorldFactory.Box(vehicle,new(0,1.40f,-1.90f),new(.018f,.14f,.028f),new("202a2d")).Layers=8;
        foreach(int i in new[]{0,2})
        {
            var arm=WorldFactory.Box(vehicle,Vector3.Zero,new(.018f,.018f,.15f),new("202a2d"));
            arm.Layers=8;arm.CastShadow=GeometryInstance3D.ShadowCastingSetting.Off;
            mirrorArms[i]=arm;
        }
    }
    void UpdateMirrorMount(int i)
    {
        if(mirrorArms[i] is not { } arm) return;
        var anchor=new Vector3(i==0?-.89f:.89f,.915f,(float)CarSpec.MirrorZ-.10f);
        // Enter the back of the lower housing; never pass in front of its glass.
        var attachment=mirrorScreens[i].Transform*new Vector3(0,-.05f,-.008f);
        arm.Position=(anchor+attachment)/2;
        var mesh=(BoxMesh)arm.Mesh;float length=anchor.DistanceTo(attachment);
        if(Math.Abs(mesh.Size.Z-length)>.00001f) mesh.Size=new(.018f,.018f,length);
        arm.Basis=Basis.LookingAt(attachment-anchor,Vector3.Up);
    }
}
