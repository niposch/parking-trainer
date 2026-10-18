using Godot;
namespace ParkingTrainer;

// Visibility adaptations and simple interior for the reused CC0 vehicle asset.
internal static class PlayerCabin
{
    public static Vector3 EyeTarget(float yaw,float height)
    {
        float angle=Math.Abs(Mathf.Wrap(yaw,-Mathf.Pi,Mathf.Pi));
        float t=Mathf.Clamp((angle-Mathf.DegToRad(100))/Mathf.DegToRad(70),0,1);
        t=t*t*(3-2*t);
        // Turn the torso and move inboard/rearward to look between the front seats.
        return new(-.36f+.32f*t,height+.015f*t,-1.25f+.17f*t);
    }
    // Adapt the existing CC0 body in place. No second exterior shell: shared
    // vertices keep the bonnet, doors, roof and hatch joined after reshaping.
    public static void AdaptBody(MeshInstance3D node,Node3D root)
    {
        if(node.Mesh is not ArrayMesh original) return;
        var transform=root.GlobalTransform.AffineInverse()*node.GlobalTransform;
        var inverse=transform.AffineInverse();
        var outer=new SurfaceTool();var inner=new SurfaceTool();var glass=new SurfaceTool();
        outer.Begin(Mesh.PrimitiveType.Triangles);inner.Begin(Mesh.PrimitiveType.Triangles);glass.Begin(Mesh.PrimitiveType.Triangles);
        var edges=new Dictionary<(Vector3,Vector3),(Vector3 A,Vector3 B,int Count)>();
        // An affine inset preserves collinearity at the source mesh's T-junctions
        // and at clipped window edges. A normalized radial offset opens pixel gaps.
        Vector3 Lining(Vector3 p)=>p.Lerp(new Vector3(0,.60f,-1.1f),.022f);
        Vector3 Shape(Vector3 p)
        {
            var pivot=new Vector3(0,.15f,-.025f);p+=pivot;
            float upper=Mathf.Clamp((p.Y-.70f)/.10f,0,1);
            p.X*=1+upper*.1636f;
            // Extend the existing rear glass into a liftback, leaving the bumper
            // and wheel arches in place. The deformation is continuous at the belt.
            float rear=p.Z<-.57f?Mathf.Clamp((-p.Z-.57f)/.18f,0,1):0;
            float taper=p.Z<-.75f?Mathf.Clamp((1.25f+p.Z)/.50f,0,1):1;
            p.Z-=upper*rear*taper*.42f;
            var result=transform*(p-pivot);
            // Match the asset's arch centres to the simulated axles while keeping
            // the original front/rear bumper at the physical body limits.
            result.Z=p.Z>=.66f?Mathf.Lerp(-(float)CarSpec.Wheelbase,-(float)CarSpec.Front,(p.Z-.66f)/.59f)
                :p.Z>=-.66f?Mathf.Lerp(0,-(float)CarSpec.Wheelbase,(p.Z+.66f)/1.32f)
                :Mathf.Lerp(0,(float)CarSpec.Rear,(-p.Z-.66f)/.64f);
            return result;
        }
        void Emit(SurfaceTool tool,List<(Vector3 P,Vector2 U)> polygon,bool reverse=false,bool lining=false)
        {
            for(int j=1;j+1<polygon.Count;j++) foreach(int k in reverse?new[]{0,j+1,j}:new[]{0,j,j+1})
            {var v=polygon[k];tool.SetUV(v.U);tool.AddVertex(inverse*(lining?Lining(v.P):v.P));}
        }
        void Solid(List<(Vector3 P,Vector2 U)> polygon)
        {
            if(polygon.Count<3) return;
            Emit(outer,polygon);Emit(inner,polygon,true,true);
            for(int i=0;i<polygon.Count;i++)
            {
                var a=polygon[i].P;var b=polygon[(i+1)%polygon.Count].P;
                Vector3 Key(Vector3 p)=>new(Mathf.Snapped(p.X,.0001f),Mathf.Snapped(p.Y,.0001f),Mathf.Snapped(p.Z,.0001f));
                var ka=Key(a);var kb=Key(b);
                bool swap=ka.X>kb.X || ka.X==kb.X&&(ka.Y>kb.Y || ka.Y==kb.Y&&ka.Z>kb.Z);
                var key=swap?(kb,ka):(ka,kb);
                if(edges.TryGetValue(key,out var e)) edges[key]=(e.A,e.B,e.Count+1);else edges[key]=(a,b,1);
            }
        }
        // Convex rear-door aperture, cut directly out of the original side panel.
        Vector2[] hole={new(-.89f,.96f),new(-.89f,1.34f),new(-.45f,1.34f),new(.41f,.96f)};
        float Cross(Vector2 a,Vector2 b)=>a.X*b.Y-a.Y*b.X;
        List<(Vector3 P,Vector2 U)> Clip(List<(Vector3 P,Vector2 U)> polygon,Vector2 a,Vector2 b,bool inside)
        {
            var result=new List<(Vector3 P,Vector2 U)>();
            float D(Vector3 p)=>-Cross(b-a,new Vector2(p.Z,p.Y)-a)*(inside?1:-1);
            for(int j=0;j<polygon.Count;j++)
            {
                var v=polygon[j];var w=polygon[(j+1)%polygon.Count];float d=D(v.P),e=D(w.P);
                if(d>=-.00001f) result.Add(v);
                if((d>=0)!=(e>=0)) {float t=d/(d-e);result.Add((v.P.Lerp(w.P,t),v.U.Lerp(w.U,t)));}
            }
            return result;
        }
        StandardMaterial3D? paint=null;
        for(int surface=0;surface<original.GetSurfaceCount();surface++)
        {
            var arrays=original.SurfaceGetArrays(surface);var points=arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var uv=arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();var indices=arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
            paint=original.SurfaceGetMaterial(surface) as StandardMaterial3D;var texture=paint?.AlbedoTexture?.GetImage();
            for(int i=0;i<indices.Length;i+=3)
            {
                // Original toy mirror blocks are replaced by the functioning
                // vehicle-mounted mirrors, avoiding a second pair on the doors.
                if(new[]{indices[i],indices[i+1],indices[i+2]}.All(k=>Math.Abs(points[k].X)>.549f && points[k].Y>.549f && points[k].Y<.651f && points[k].Z>.3478f && points[k].Z<.4480f)) continue;
                var poly=new List<(Vector3 P,Vector2 U)>();foreach(int k in new[]{indices[i],indices[i+1],indices[i+2]}) poly.Add((Shape(points[k]),uv[k]));
                var centre=(uv[indices[i]]+uv[indices[i+1]]+uv[indices[i+2]])/3;
                var c=texture?.GetPixel(Math.Clamp((int)(centre.X*texture.GetWidth()),0,texture.GetWidth()-1),Math.Clamp((int)(centre.Y*texture.GetHeight()),0,texture.GetHeight()-1))??Colors.White;
                bool window=points[indices[i]].Y>.64f && points[indices[i+1]].Y>.64f && points[indices[i+2]].Y>.64f && c.B>c.R*1.04f && c.G>.64f && c.R>.48f;
                if(window) {Emit(glass,poly);continue;}
                var n=(poly[1].P-poly[0].P).Cross(poly[2].P-poly[0].P).Normalized();
                bool side=Math.Abs(n.X)>.85f && poly.All(v=>Math.Abs(v.P.X)>.60f);
                if(side)
                {
                    var remainder=poly;
                    for(int k=0;k<hole.Length && remainder.Count>=3;k++)
                    {Solid(Clip(remainder,hole[k],hole[(k+1)%hole.Length],false));remainder=Clip(remainder,hole[k],hole[(k+1)%hole.Length],true);}
                    if(remainder.Count>=3) Emit(glass,remainder);
                }
                else Solid(poly);
            }
        }
        // Return faces join the inner liner to every open window edge.
        foreach(var e in edges.Values.Where(e=>e.Count==1))
            foreach(var p in new[]{e.A,e.B,Lining(e.B),e.A,Lining(e.B),Lining(e.A)}) inner.AddVertex(inverse*p);
        // The source atlas material is double-sided. Its orange back faces must
        // not show through the dark lining along rasterized triangle boundaries.
        var exterior=(StandardMaterial3D)paint!.Duplicate();exterior.CullMode=BaseMaterial3D.CullModeEnum.Back;
        var interior=WorldFactory.Material(new("30393e"));
        var mesh=new ArrayMesh();outer.GenerateNormals();outer.Commit(mesh);mesh.SurfaceSetMaterial(0,exterior);
        inner.GenerateNormals();inner.Commit(mesh);mesh.SurfaceSetMaterial(1,interior);
        node.Mesh=mesh;node.Layers=2;
        var windows=new ArrayMesh();glass.GenerateNormals();glass.Commit(windows);
        var tint=WorldFactory.Material(new Color(.24f,.37f,.43f,.23f));tint.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        tint.CullMode=BaseMaterial3D.CullModeEnum.Disabled;
        node.AddChild(new MeshInstance3D {Name="WindowGlass",Mesh=windows,MaterialOverride=tint,Layers=32,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off});
    }
    public static void Build(Node3D parent)
    {
        var cabin=new Node3D {Name="VisibilityCabin"};parent.AddChild(cabin);
        Color seat=new("354047"),head=new("303a40");
        foreach(float side in new[]{-1f,1f})
        {
            // Door cards sit inside the body and close the view into wheel wells.
            WorldFactory.Box(cabin,new(side*.69f,.60f,-1.20f),new(.05f,.48f,1.80f),new("30393e")).Layers=2;
            WorldFactory.Box(cabin,new(side*.69f,.60f,.18f),new(.05f,.48f,.96f),new("30393e")).Layers=2;
        }
        WorldFactory.Box(cabin,new(0,.34f,-.69f),new(1.34f,.08f,2.98f),new("252f35")).Layers=2;
        WorldFactory.Box(cabin,new(0,.61f,-2.15f),new(1.48f,.28f,.10f),new("333d42")).Layers=2;
        WorldFactory.Box(cabin,new(0,.63f,.68f),new(1.40f,.48f,.10f),new("303b42")).Layers=2;
        foreach(float side in new[]{-1f,1f}) WorldFactory.Box(cabin,new(side*.69f,.47f,.02f),new(.25f,.38f,.72f),new("303b42")).Layers=2;
        foreach(float x in new[]{-.40f,.40f})
        {
            WorldFactory.Box(cabin,new(x,.52f,-1.16f),new(.49f,.16f,.53f),seat).Layers=2;
            var back=WorldFactory.Box(cabin,new(x,.77f,-.97f),new(.49f,.46f,.12f),seat);back.Rotation=new(.15f,0,0);back.Layers=2;
            WorldFactory.Box(cabin,new(x,1.17f,-.94f),new(.27f,.21f,.13f),head).Layers=2;
            foreach(float dx in new[]{-.075f,.075f}) WorldFactory.Box(cabin,new(x+dx,1.045f,-.94f),new(.015f,.10f,.015f),new("87918e")).Layers=2;
        }
        // One correctly positioned rear bench with three separate head restraints.
        WorldFactory.Box(cabin,new(0,.52f,-.35f),new(1.40f,.17f,.52f),seat).Layers=2;
        var rearBack=WorldFactory.Box(cabin,new(0,.80f,-.08f),new(1.40f,.47f,.13f),seat);rearBack.Rotation=new(.12f,0,0);rearBack.Layers=2;
        foreach(var h in new[]{(X:-.46f,Y:1.18f,W:.27f,H:.20f),(X:0f,Y:1.075f,W:.22f,H:.13f),(X:.46f,Y:1.09f,W:.27f,H:.15f)})
        {
            WorldFactory.Box(cabin,new(h.X,h.Y,-.035f),new(h.W,h.H,.12f),head).Layers=2;
            foreach(float dx in new[]{-.07f,.07f}) WorldFactory.Box(cabin,new(h.X+dx,h.Y-h.H/2-.035f,-.035f),new(.014f,.12f,.014f),new("87918e")).Layers=2;
        }
        WorldFactory.Box(cabin,new(0,.86f,.40f),new(1.42f,.05f,.72f),new("2b353a")).Layers=2;
        WorldFactory.Box(cabin,new(0,.70f,-2.24f),new(1.48f,.12f,.18f),new("333d42")).Layers=2;
    }
}

