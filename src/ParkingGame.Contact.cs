using Godot;
namespace ParkingTrainer;

// A contact must be impossible to miss: red screen edges, a jolt, a thud, rumble
// or phone vibration, a marker at the touching point and a banner naming it.
public partial class ParkingGame
{
    ColorRect contactVignette=null!;
    Label contactBanner=null!;
    AudioStreamPlayer contactSound=null!;
    MeshInstance3D contactMarker=null!;
    double contactBannerTime,contactShake;
    internal string ContactText="";
    // Whether the car is still resting against the obstacle of a real contact.
    bool contactTouching;
    void SetupContactFeedback()
    {
        contactVignette=new ColorRect {MouseFilter=Control.MouseFilterEnum.Ignore,Material=new ShaderMaterial {Shader=new Shader {Code=
            "shader_type canvas_item; uniform float strength=0.0; void fragment(){ vec2 d=abs(UV-0.5)*2.0; float e=smoothstep(0.45,1.0,max(d.x,d.y)); COLOR=vec4(0.95,0.12,0.06,e*strength); }"}}};
        hud.AddChild(contactVignette);hud.MoveChild(contactVignette,0);contactVignette.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);contactVignette.Hide();
        contactBanner=Text(hud,"",22,Colors.White);contactBanner.HorizontalAlignment=HorizontalAlignment.Center;contactBanner.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var style=Style(new Color(.75f,.12f,.08f,.92f),new Color("ffb3a6"),10);contactBanner.AddThemeStyleboxOverride("normal",style);contactBanner.Hide();
        contactSound=new AudioStreamPlayer {Stream=ThudSound(),VolumeDb=-4};AddChild(contactSound);
        var red=WorldFactory.Material(new Color(1,.25f,.15f,.85f),true);red.Transparency=BaseMaterial3D.TransparencyEnum.Alpha;
        contactMarker=new MeshInstance3D {Mesh=new SphereMesh {Radius=.16f,Height=.32f},MaterialOverride=red,Layers=1,CastShadow=GeometryInstance3D.ShadowCastingSetting.Off};
        AddChild(contactMarker);contactMarker.Hide();
    }
    // A short low thump with a scrape of noise, generated once.
    static AudioStreamWav ThudSound()
    {
        const int rate=22050;int count=(int)(rate*.35);var data=new byte[count*2];var rng=new Random(7);
        for(int i=0;i<count;i++)
        {
            double t=(double)i/rate;
            double v=Math.Sin(2*Math.PI*(62+40*Math.Exp(-t*20))*t)*Math.Exp(-t*11)*.75+(rng.NextDouble()*2-1)*Math.Exp(-t*28)*.35;
            short s=(short)(Math.Clamp(v,-1,1)*14000);data[2*i]=(byte)(s&0xff);data[2*i+1]=(byte)((s>>8)&0xff);
        }
        return new AudioStreamWav {Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Stereo=false,Data=data};
    }
    static string ObstacleName(string model)=>model switch
    {
        "curb"=>"the kerb","wall"=>"the wall","pillar"=>"the pillar","post"=>"the signpost","lowpost"=>"the low post","bin"=>"the bin","trolley"=>"the trolley",
        "towbar"=>"the tow bar","rack"=>"the bike rack","shelf"=>"the shelf","bike"=>"the bicycle","tree"=>"the tree",_=>"the parked car"
    };
    // Which part of the car touched what, from the nearest obstacle point.
    string DescribeContact((double X,double Z) point)
    {
        double c=Math.Cos(Car.Pose.Yaw),s=Math.Sin(Car.Pose.Yaw),dx=point.X-Car.Pose.X,dz=point.Z-Car.Pose.Z;
        double x=c*dx-s*dz,z=s*dx+c*dz;
        bool mirror=Math.Abs(Math.Abs(x)-CarSpec.MirrorX)<.25 && Math.Abs(z-CarSpec.MirrorZ)<.3 && Math.Abs(x)>CarSpec.Width/2;
        string end=z<-CarSpec.Front+.5?"front":z>CarSpec.Rear-.5?"rear":"";
        string side=x<-.55?"left":x>.55?"right":"";
        string part=mirror?$"{side} mirror":end!=""&&side!=""?$"{end}-{side} corner":end!=""?$"{end} bumper":$"{side} side";
        var obstacle=Scene.Obstacles.OrderBy(o=>Nearest(Car.Pose,new[]{o},5).Distance).First();
        return string.Format(Tr("CONTACT · {0} hit {1}"),Tr(part),Tr(ObstacleName(obstacle.Model)));
    }
    void OnContact()
    {
        ContactFlash=1;contactShake=1;contactBannerTime=3;contactTouching=true;
        var nearest=Nearest(Car.Pose,Scene.Obstacles,.3);
        ContactText=double.IsFinite(nearest.Distance)?DescribeContact(nearest.Point):Tr("CONTACT");
        if(double.IsFinite(nearest.Distance)) {contactMarker.Position=new((float)nearest.Point.X,.4f,(float)nearest.Point.Z);contactMarker.Show();}
        contactBanner.Text=ContactText+"\n"+Tr("Stop. Move away from it, then correct.");
        if(!Muted && BeeperVolume>0) {contactSound.VolumeDb=Mathf.LinearToDb(Mathf.Max(.05f,BeeperVolume))-4;contactSound.Play();}
        if(selectedController>=0) Input.StartJoyVibration(selectedController,.7f,.9f,.25f);
        if(TouchEnabled) Input.VibrateHandheld(180);
    }
    void UpdateContactFeedback(double delta)
    {
        ContactFlash=Math.Max(0,ContactFlash-delta*1.4);contactShake=Math.Max(0,contactShake-delta*3);
        // Keep the banner while the car still rests against the obstacle.
        if(NearestClearance>=.03) contactTouching=false;
        bool touching=contactTouching;
        contactBannerTime=touching?Math.Max(contactBannerTime,.5):Math.Max(0,contactBannerTime-delta);
        contactVignette.Visible=ContactFlash>0 || touching;
        ((ShaderMaterial)contactVignette.Material).SetShaderParameter("strength",(float)Math.Max(ContactFlash*.85,touching?.3+.15*Math.Sin(Elapsed*6):0));
        contactBanner.Visible=contactBannerTime>0 && !Paused && !Inspection;
        contactMarker.Visible=contactMarker.Visible && (touching || contactBannerTime>0) && !Inspection;
        float jolt=(float)(contactShake*contactShake*.035);
        eye.HOffset=jolt*Mathf.Sin((float)Elapsed*71);eye.VOffset=jolt*Mathf.Sin((float)Elapsed*53);
    }
    void LayoutContact(Vector2 size,Vector2 pixels,float unit)
    {
        contactBanner.Scale=Vector2.One*unit;contactBanner.Size=new(Mathf.Min(560,pixels.X-24),0);
        contactBanner.Size=new(contactBanner.Size.X,contactBanner.GetCombinedMinimumSize().Y);
        contactBanner.Position=new((size.X-contactBanner.Size.X*unit)/2,size.Y*.22f);
    }
}
