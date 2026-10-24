using Godot;
namespace ParkingTrainer;

public static class WorldFactory
{
    public static StandardMaterial3D Material(Color color,bool unshaded=false) => new() {
        AlbedoColor=color,Roughness=.94f,ShadingMode=unshaded?BaseMaterial3D.ShadingModeEnum.Unshaded:BaseMaterial3D.ShadingModeEnum.PerPixel };
    public static MeshInstance3D Box(Node3D parent,Vector3 position,Vector3 size,Color color)
    {
        var mesh=new MeshInstance3D { Mesh=new BoxMesh { Size=size },MaterialOverride=Material(color),Position=position };
        parent.AddChild(mesh); return mesh;
    }
    public static void Line(Node3D parent,Vector3 a,Vector3 b,float width,Color color)
    {
        var mesh=Box(parent,(a+b)*.5f,new(width,.014f,a.DistanceTo(b)),color);
        if(a.DistanceTo(b)>.001f) mesh.LookAt(b,Vector3.Up);
    }
    public static Node3D Car(Node3D parent,string model,double width,double length,double height,bool player=false)
    {
        var root=new Node3D(); parent.AddChild(root);
        var source=GD.Load<PackedScene>($"res://assets/kenney/{model}.glb").Instantiate<Node3D>();
        root.AddChild(source);
        // Normalize the reused mesh, while placing wheel axles separately from body scale.
        Aabb bounds=new(); bool first=true;
        foreach(var mesh in Meshes(source))
        {
            var box=source.GlobalTransform.AffineInverse()*mesh.GlobalTransform*mesh.GetAabb();
            bounds=first?box:bounds.Merge(box); first=false;
        }
        var dimensions=bounds.Size;
        source.Scale=new((float)width/dimensions.X,(float)height/dimensions.Y,(float)length/dimensions.Z);
        source.Position=new(0,-bounds.Position.Y*source.Scale.Y,0);
        source.Rotation=new(0,Mathf.Pi,0); // asset front +Z -> vehicle front -Z
        if(player)
        {
            source.Scale=new((float)width/1.30f,source.Scale.Y,source.Scale.Z);
            source.Position+=new Vector3(0,0,(float)CarSpec.CentreZ);
            foreach(var mesh in Meshes(source))
            {
                mesh.Layers=32; // wheels/glass are exterior-only; AdaptBody exposes the lined body to the driver
                if(mesh.Name.ToString().Contains("wheel"))
                {
                    bool front=mesh.Name.ToString().Contains("front");
                    bool left=mesh.Name.ToString().Contains("left");
                    mesh.GetParent().RemoveChild(mesh);root.AddChild(mesh);
                    mesh.Rotation=new(0,Mathf.Pi,0);
                    mesh.Scale=new(source.Scale.X*.55f,source.Scale.Y,source.Scale.Y);
                    var centre=new Vector3(left?-.76f:.76f,.346f,front?-(float)CarSpec.Wheelbase:0);
                    mesh.Position=centre-mesh.Basis*mesh.GetAabb().GetCenter();
                    mesh.SetMeta("wheel_centre",centre);
                    mesh.SetMeta("steering_wheel",front);
                    mesh.SetMeta("base_rotation",mesh.Rotation);
                }
                else if(mesh.Name=="body") PlayerCabin.AdaptBody(mesh,root);
            }
            PlayerCabin.Build(root);
            // A neutral wheel communicates accumulated steering position in first person.
            var wheel=new MeshInstance3D { Name="DriverWheel",Mesh=new TorusMesh { InnerRadius=.16f,OuterRadius=.19f,Rings=20,RingSegments=10 },
                MaterialOverride=Material(new("20282c")),Position=new(-.38f,.80f,-1.95f),Rotation=new(Mathf.Pi/2-.25f,0,0),Layers=2 };
            root.AddChild(wheel);
            Box(wheel,new(0,0,.04f),new(.24f,.035f,.035f),new("455053")).Layers=2;
            Box(wheel,new(0,0,.08f),new(.035f,.035f,.16f),new("455053")).Layers=2;
        }
        return root;
    }
    public static IEnumerable<MeshInstance3D> Meshes(Node node)
    {
        if(node is MeshInstance3D mesh) yield return mesh;
        foreach(Node child in node.GetChildren()) foreach(var m in Meshes(child)) yield return m;
    }
    public static void BuildLot(Node3D root,Scenario scene,bool showGoal=true)
    {
        Box(root,new(0,-.12f,0),new(70,.2f,70),new("697276"));
        if(scene.Exercise is not (Exercise.ReverseCorner or Exercise.TurnInRoad))
        {
            // Quiet border and distance markers make the small practice area legible.
            Box(root,new(-19,-.08f,0),new(6,.18f,65),new("97a48d"));
            if(scene.Exercise==Exercise.Parallel) for(int z=-24;z<=24;z+=6) Box(root,new(-4,.006f,z),new(.09f,.015f,2.2f),new("e0e0d1"));
        }
        foreach(var obstacle in scene.Obstacles) Prop(root,obstacle);
        Color white=new("eeeee3");
        foreach(var line in scene.Lines) Line(root,new((float)line.X1,.012f,(float)line.Z1),new((float)line.X2,.012f,(float)line.Z2),.07f,white);
        if(!showGoal) return;
        Color green=new("c7f29f");
        (double X,double Z)[] outline;
        if(scene.FreeGoal)
        {
            // A lane target: any stopping point inside the zone counts.
            var g=scene.Goal;double half=CarSpec.Width/2+scene.LateralTolerance;
            outline=new[]{g.Point(-half,CarSpec.CentreZ+3.5),g.Point(half,CarSpec.CentreZ+3.5),g.Point(half,CarSpec.CentreZ-5.5),g.Point(-half,CarSpec.CentreZ-5.5)};
        }
        else outline=DrivingModel.Body(scene.Goal).Corners();
        for(int i=0;i<4;i++)
        {
            var a=outline[i]; var b=outline[(i+1)%4];
            Line(root,new((float)a.X,.025f,(float)a.Z),new((float)b.X,.025f,(float)b.Z),.045f,green);
        }
        var centre=scene.Goal.Point(0,CarSpec.CentreZ);
        if(scene.FreeGoal)
        {
            // Direction of travel through the lane zone, drawn as lines (no font glyph needed).
            Vector3 P(double x,double z) {var p=scene.Goal.Point(x,z);return new((float)p.X,.03f,(float)p.Z);}
            double tip=CarSpec.CentreZ-1.2,tail=CarSpec.CentreZ+1.2;
            Line(root,P(0,tail),P(0,tip),.12f,green);
            Line(root,P(0,tip),P(-.5,tip+.6),.12f,green);Line(root,P(0,tip),P(.5,tip+.6),.12f,green);
            return;
        }
        var text=new Label3D { Text="P",FontSize=160,PixelSize=.007f,Modulate=green,Position=new((float)centre.X,.03f,(float)centre.Z),Rotation=new(-Mathf.Pi/2,0,0),NoDepthTest=false };
        root.AddChild(text);
    }
    static void Prop(Node3D root,Obstacle obstacle)
    {
        var p=obstacle.Shape;float w=(float)p.Width,l=(float)p.Length,h=(float)obstacle.Height;
        var node=new Node3D {Position=new((float)p.X,0,(float)p.Z),Rotation=new(0,(float)p.Yaw,0)};
        switch(obstacle.Model)
        {
            case "curb": Box(node,new(0,h/2,0),new(w,h,l),new("b9b8aa")); break;
            case "wall": Box(node,new(0,h/2,0),new(w,h,l),new("d8d2c4")); break;
            case "pillar":
                Box(node,new(0,h/2,0),new(w,h,l),new("a9aaa4"));
                Box(node,new(0,.45f,0),new(w+.02f,.3f,l+.02f),new("f2c230"));
                break;
            case "post":
                Box(node,new(0,h/2,0),new(w,h,l),new("3d464a"));
                Box(node,new(0,h-.3f,0),new(.06f,.45f,.45f),new("2f6fb3"));
                break;
            case "lowpost": Box(node,new(0,h/2,0),new(w,h,l),new("f2c230")); break;
            case "bin":
                Box(node,new(0,h/2-.03f,0),new(w,h-.06f,l),new("3e6b45"));
                Box(node,new(0,h-.03f,0),new(w+.04f,.06f,l+.04f),new("2c4d32"));
                break;
            case "trolley":
                Box(node,new(0,.62f,0),new(w,.42f,l*.8f),new("c0c6c8"));
                Box(node,new(0,.95f,l*.45f),new(w,.05f,.05f),new("c0392b"));
                foreach(float x in new[]{-w/2+.05f,w/2-.05f}) Box(node,new(x,.2f,0),new(.03f,.4f,l*.8f),new("8d9497"));
                break;
            case "rack": Box(node,new(0,.8f,0),new(w,.5f,l),new("2b2f33")); break;
            case "towbar": Box(node,new(0,.3f,0),new(w,.2f,l),new("2b2f33")); break;
            case "shelf": Box(node,new(0,h/2,0),new(w,h,l),new("8a6d4b")); break;
            case "bike":
                Box(node,new(0,.55f,0),new(.05f,.5f,l*.9f),new("2b2f33"));
                foreach(float z in new[]{-l/2+.33f,l/2-.33f}) Box(node,new(0,.33f,z),new(.04f,.66f,.66f),new("454b50"));
                break;
            case "tree":
                Box(node,new(0,1.1f,0),new(.25f,2.2f,.25f),new("6b4f36"));
                node.AddChild(new MeshInstance3D {Mesh=new SphereMesh {Radius=1.2f,Height=2.2f},MaterialOverride=Material(new("5d8a4a")),Position=new(0,2.8f,0)});
                break;
            default:
                root.AddChild(node);
                Car(node,obstacle.Model,p.Width,p.Length,obstacle.Height);
                return;
        }
        root.AddChild(node);
    }
}
