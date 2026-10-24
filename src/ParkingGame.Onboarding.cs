using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    ControlStrip controlStrip=null!;
    Label promptLabel=null!,coachLabel=null!;
    PanelContainer coachPanel=null!;
    internal bool ControlHints=true;
    bool seenHowTo,padGlyphs;
    // Labels for the controller in use, or null for keyboard prompts.
    internal PadLabels? Pad=>padGlyphs?PadLabels.For(NativeDriving?modernController.Name:selectedController>=0?Input.GetJoyName(selectedController):""):null;
    float steerInputShown;
    const string HowToText="The car always starts secured: in P with the parking brake on.\n\n1. Hold the brake: S · controller LT.\n2. Select a gear while braking: 4 = D forward, 2 = R reverse, 1 = P · controller RB = D, LB = R, A = P.\n3. Release the parking brake: Space · controller Y.\n4. Ease off the brake and the car creeps by itself. W / RT adds gas, which you rarely need.\n5. Steer with A / D · left stick. The wheel stays where you leave it; hold Z or click the left stick to centre it.\n6. Look around with the mouse · right stick (click it to look forward). V / B: camera screen · C / View: over the shoulder · Tab / X: rear or 360° camera.\n7. To finish: stop, hold the brake, select P, then apply the parking brake. You get a parking report.\n\nHelp: H / D-pad up shows the parking instructions; F / D-pad down shows a correction plan. Esc / Start pauses. PlayStation and Nintendo controllers get their own button names in the prompts.";
    void SetupOnboarding()
    {
        coachPanel=Panel(hud,new(12,240),new(370,0),new Color(.02f,.045f,.05f,.72f));
        var style=Style(new Color(.02f,.045f,.05f,.72f),new Color("5fd6f0"),10);style.ContentMarginTop=style.ContentMarginBottom=9;style.ContentMarginLeft=style.ContentMarginRight=13;
        coachPanel.AddThemeStyleboxOverride("panel",style);
        coachLabel=Text(coachPanel,"",15,cream);coachLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;coachLabel.CustomMinimumSize=new(344,0);
        coachPanel.Hide();
        promptLabel=Text(hud,"",17,new("f6ffe9"));promptLabel.HorizontalAlignment=HorizontalAlignment.Center;promptLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        promptLabel.AddThemeColorOverride("font_shadow_color",ink);promptLabel.AddThemeConstantOverride("shadow_offset_x",1);promptLabel.AddThemeConstantOverride("shadow_offset_y",2);
        controlStrip=new ControlStrip();hud.AddChild(controlStrip);
    }
    string NextStep()
    {
        var labels=Pad;bool pad=labels!=null;
        string brake=labels?.Brake??"S",park=pad?string.Format(Tr("press {0}"),labels!.ParkingBrake):Tr("press Space");
        string Select(Gear gear)=>pad?(gear==Gear.R?labels!.Reverse:labels!.Drive):(gear==Gear.R?"2":"4");
        bool braking=Brake>.25,stopped=Math.Abs(Car.Speed)<.02;
        var first=Scene.Path.Count>1 && Scene.Path[1].Distance<0?Gear.R:Gear.D;
        if(successTime>.8 && Car.Gear!=Gear.P) return string.Format(Tr("Hold {0}, select P ({1}), then {2} to finish."),brake,pad?labels!.Park:"1",park);
        if(Car.Gear==Gear.P && Car.ParkingBrake)
        {
            if(Car.Travel>.05) return "";
            return braking?string.Format(Tr("Keep holding {0} and press {1} for {2}."),brake,Select(first),first)
                :string.Format(Tr("The car is secured in P. Hold {0} to brake first."),brake);
        }
        if(Car.Gear==Gear.P) return string.Format(Tr("Now {0} to apply the parking brake."),park);
        if(Car.Gear==Gear.N) return string.Format(Tr("Hold {0} and press {1} for D or {2} for R."),brake,Select(Gear.D),Select(Gear.R));
        if(Car.ParkingBrake) return string.Format(Tr("Now {0} to release the parking brake."),park);
        if(stopped && Car.Travel<.05) return string.Format(Tr("Ease off {0} and the car creeps. {1} adds gas, {2} turns the wheel."),brake,pad?labels!.Gas:"W",pad?Tr("the left stick"):"A / D");
        if(Car.Travel<3) return pad?string.Format(Tr("Right stick: look around · {0}: glance at the camera screen"),labels!.Console):Tr("Mouse: look around · V: glance at the camera screen · C: over the shoulder");
        return "";
    }
    void UpdateOnboarding()
    {
        bool driving=!Paused && !Inspection && !Demo && !TouchEnabled;
        controlStrip.Visible=driving && ControlHints;
        if(controlStrip.Visible) controlStrip.Set((float)Gas,(float)Brake,steerInputShown,Car.Steer,Car.Gear,Car.ParkingBrake,Pad);
        promptLabel.Text=driving && ControlHints && Mode!=TrainingMode.Test?NextStep():"";
        promptLabel.Visible=promptLabel.Text!="";
        coachLabel.Text=CoachText;
        coachPanel.Visible=CoachText!="" && !Paused && !Inspection;
        reportPanel.Visible=Inspection && !Paused && LastReport!=null;
    }
    void LayoutOnboarding(Vector2 size,Vector2 pixels,float unit)
    {
        float stripScale=unit*Mathf.Min(1,(pixels.X-24)/ControlStrip.DesignWidth);
        controlStrip.Scale=Vector2.One*stripScale;controlStrip.Size=new(ControlStrip.DesignWidth,ControlStrip.DesignHeight);
        controlStrip.Position=new((size.X-ControlStrip.DesignWidth*stripScale)/2,size.Y-(ControlStrip.DesignHeight+12)*stripScale);
        promptLabel.Scale=Vector2.One*unit;promptLabel.Size=new(Mathf.Min(620,pixels.X-24),48);
        promptLabel.Position=new((size.X-promptLabel.Size.X*unit)/2,controlStrip.Position.Y-54*unit);
        float coachScale=unit*Mathf.Min(1,(pixels.X-24)/370);
        coachPanel.Scale=Vector2.One*coachScale;coachPanel.Size=new(370,coachPanel.GetCombinedMinimumSize().Y);
        float top=tutorPanel.Visible?tutorPanel.Position.Y+tutorPanel.Size.Y*tutorPanel.Scale.Y+8*unit:(TouchEnabled?88:12)*unit;
        coachPanel.Position=new(tutorPanel.Visible||TouchEnabled?12*unit:64*unit,top);
        LayoutReport(pixels,unit);
    }
}
