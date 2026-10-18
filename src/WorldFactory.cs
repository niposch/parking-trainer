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
                mesh.Layers=2;
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
                else if(mesh.Name=="body") OpenWindows(mesh);
            }
            // Minimal cabin infill only; the exterior/pillars/wheels remain the CC0 asset.
            Color dark=new("333b3e");
            foreach(float x in new[]{-.43f,.43f})
            foreach(float z in new[]{-.5f,-1.15f})
            {
                Box(root,new(x,.48f,z),new(.46f,.15f,.48f),dark).Layers=2;
                Box(root,new(x,.77f,z+.20f),new(.46f,.55f,.11f),dark).Layers=2;
                Box(root,new(x,1.09f,z+.20f),new(.26f,.20f,.12f),dark).Layers=2;
            }
            Box(root,new(0,.70f,-2.14f),new(1.65f,.12f,.36f),dark).Layers=2;
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
    static void OpenWindows(MeshInstance3D node)
    {
        // Remove only the pale-blue upper window triangles from the atlas-based body.
        // This small adaptation preserves the model's existing window frames/pillars.
        if(node.Mesh is not ArrayMesh original) return;
        var edited=new ArrayMesh(); int removed=0;
        for(int surface=0;surface<original.GetSurfaceCount();surface++)
        {
            var arrays=original.SurfaceGetArrays(surface);
            var vertices=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var uv=arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
            var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            var material=original.SurfaceGetMaterial(surface) as StandardMaterial3D;
            var texture=material?.AlbedoTexture?.GetImage();
            var keep=new List<int>();
            for(int i=0;i<indices.Length;i+=3)
            {
                bool glass=false;
                if(texture!=null)
                {
                    var centre=(uv[indices[i]]+uv[indices[i+1]]+uv[indices[i+2]])/3;
                    var c=texture.GetPixel(Math.Clamp((int)(centre.X*texture.GetWidth()),0,texture.GetWidth()-1),Math.Clamp((int)(centre.Y*texture.GetHeight()),0,texture.GetHeight()-1));
                    float low=Math.Min(vertices[indices[i]].Y,Math.Min(vertices[indices[i+1]].Y,vertices[indices[i+2]].Y));
                    glass=low>.44f && c.B>c.R*1.04f && c.G>.64f && c.R>.48f;
                }
                if(glass) removed++; else { keep.Add(indices[i]);keep.Add(indices[i+1]);keep.Add(indices[i+2]); }
            }
            // Thin the toy asset's oversized A-pillars by moving its existing aperture
            // borders together. No new exterior is modelled; cabin dimensions stay fixed.
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];float ax=Math.Abs(p.X);
                if(Math.Abs(ax-.3644f)<.001f || Math.Abs(ax-.4488f)<.001f) p.X+=Math.Sign(p.X)*.065f;
                if(ax>.46f && ax<.56f && (Math.Abs(p.Z-.1263f)<.001f || Math.Abs(p.Z-.4479f)<.001f || Math.Abs(p.Z-.3479f)<.001f)) p.Z+=.09f;
                vertices[i]=p;
            }
            arrays[(int)Mesh.ArrayType.Vertex]=vertices;
            arrays[(int)Mesh.ArrayType.Index]=keep.ToArray();
            edited.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles,arrays);
            edited.SurfaceSetMaterial(surface,material);
        }
        node.Mesh=edited;
        GD.Print($"Player windows: removed {removed} glass triangles; reused original body and pillars.");
    }
    public static void BuildLot(Node3D root,Scenario scene)
    {
        Box(root,new(0,-.12f,0),new(65,.2f,65),new("697276"));
        // Quiet border and distance markers make the small practice area legible.
        Box(root,new(-19,-.08f,0),new(6,.18f,65),new("97a48d"));
        for(int z=-24;z<=24;z+=6) Box(root,new(-4,.006f,z),new(.09f,.015f,2.2f),new("e0e0d1"));
        foreach(var obstacle in scene.Obstacles)
        {
            var p=obstacle.Shape;
            if(obstacle.Curb)
            {
                var b=Box(root,new((float)p.X,(float)obstacle.Height/2,(float)p.Z),new((float)p.Width,(float)obstacle.Height,(float)p.Length),new("b9b8aa"));
                b.Rotation=new(0,(float)p.Yaw,0);
            }
            else
            {
                var car=Car(root,obstacle.Model,p.Width,p.Length,obstacle.Height);
                car.Position=new((float)p.X,0,(float)p.Z); car.Rotation=new(0,(float)p.Yaw,0);
            }
        }
        Color white=new("eeeee3");
        if(scene.Exercise!=Exercise.Parallel)
        {
            float w=(float)scene.BayWidth;
            for(int i=-3;i<=3;i++) Line(root,new((i+.5f)*w,.012f,-5.85f),new((i+.5f)*w,.012f,-.15f),.07f,white);
            Line(root,new(-3.5f*w,.012f,-5.85f),new(3.5f*w,.012f,-5.85f),.07f,white);
        }
        var goal=DrivingModel.Body(scene.Goal); var corners=goal.Corners();
        for(int i=0;i<4;i++)
        {
            var a=corners[i]; var b=corners[(i+1)%4];
            Line(root,new((float)a.X,.025f,(float)a.Z),new((float)b.X,.025f,(float)b.Z),.045f,new("c7f29f"));
        }
        var text=new Label3D { Text="P",FontSize=160,PixelSize=.007f,Modulate=new("c7f29f"),Position=new((float)goal.X,.03f,(float)goal.Z),Rotation=new(-Mathf.Pi/2,0,0),NoDepthTest=false };
        root.AddChild(text);
    }
}
