using Godot;
namespace ParkingTrainer;

// A short card when a new kind of scenario starts: what the goal is, which way to
// drive first, and how to finish. Retries of the same kind skip it.
public partial class ParkingGame
{
    PanelContainer briefing=null!;
    Label briefingTitle=null!,briefingBody=null!;
    ParkingDiagram briefingDiagram=null!;
    Button briefingButton=null!;
    bool briefingPending;
    internal bool BriefingEnabled=true;
    string lastBriefed="";
    internal bool BriefingVisible=>IsInstanceValid(briefing) && briefing.Visible;
    void SetupBriefing()
    {
        briefing=Panel(hud,Vector2.Zero,new(480,0),cream);briefing.MouseFilter=Control.MouseFilterEnum.Stop;
        var style=Style(cream,new Color("456b34"),12);style.BorderWidthBottom=style.BorderWidthTop=style.BorderWidthLeft=style.BorderWidthRight=2;
        briefing.AddThemeStyleboxOverride("panel",style);
        var box=new VBoxContainer();box.AddThemeConstantOverride("separation",8);briefing.AddChild(box);
        briefingTitle=Text(box,"",22);briefingTitle.AutowrapMode=TextServer.AutowrapMode.WordSmart;briefingTitle.CustomMinimumSize=new(444,0);
        briefingDiagram=new ParkingDiagram {CustomMinimumSize=new(0,120),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};box.AddChild(briefingDiagram);
        briefingBody=Text(box,"",16);briefingBody.AutowrapMode=TextServer.AutowrapMode.WordSmart;briefingBody.CustomMinimumSize=new(444,0);
        briefingButton=Button(box,"",DismissBriefing);briefingButton.AddThemeStyleboxOverride("normal",Style(accent,null,7));
        briefing.Hide();
    }
    void QueueBriefing()
    {
        string kind=$"{Exercise}/{Scene.Drill}/{MirrorBay}/{Mode}/{OppositeRow}";
        briefingPending=BriefingEnabled && kind!=lastBriefed;lastBriefed=kind;
        briefing?.Hide();
    }
    void ShowBriefing()
    {
        briefingPending=false;
        string side=Tr(MirrorBay?"right":"left"),kerbSide=Tr(MirrorBay?"left":"right");
        briefingTitle.Text=string.Format(Tr("{0} · {1}"),Tr(ExerciseTitle(Exercise)),Tr(DrillTitle(Scene.Drill)));
        briefingDiagram.Exercise=Exercise;briefingDiagram.Mirrored=MirrorBay;briefingDiagram.Visible=Scene.Drill==Drill.Park;briefingDiagram.QueueRedraw();
        string goal=Scene.Drill switch
        {
            Drill.Correct=>Tr("You start parked, but off-centre and crooked. Fix the position with as few moves as you can."),
            Drill.DriveOut=>Tr("You start parked. Drive out without touching anything and stop in the green lane zone."),
            _=>Exercise switch
            {
                Exercise.Parallel=>string.Format(Tr("Reverse into the gap between the parked cars, parallel to the kerb on your {0}."),kerbSide),
                Exercise.ReverseBay=>string.Format(Tr("Reverse into the marked bay on your {0}."),side),
                Exercise.ForwardBay=>string.Format(Tr("Drive forward into the marked bay on your {0}."),side),
                Exercise.AngledBay=>string.Format(Tr("Drive forward into the angled bay on your {0}."),side),
                Exercise.Garage=>string.Format(Tr("Reverse into the garage on your {0}."),side),
                Exercise.ReverseCorner=>string.Format(Tr("Reverse around the corner into the side road on your {0}, close to its kerb."),kerbSide),
                _=>Tr("Turn the car round to face the other way, using forward and reverse, and finish in the opposite lane.")
            }
        };
        bool firstReverse=Scene.Path.Count>1 && Scene.Path[1].Distance<0;
        int maneuver=Scene.Drill==Drill.Park?Scene.ApproachEnd+1:1;
        bool parkReverse=maneuver<Scene.Path.Count && Scene.Path[maneuver].Distance<0;
        string how=Scene.Drill==Drill.Park
            ?(Exercise==Exercise.TurnInRoad?Tr("Drive forward (D) along the road and stop at a safe spot. Then turn: forward, reverse, forward."):
              string.Format(Tr("First drive forward (D) {0} and choose where to stop. Then {1}."),Tr(Exercise==Exercise.Parallel||Exercise==Exercise.ReverseCorner?"past the space":"along the aisle"),
                Tr(parkReverse?"select R and reverse in":"turn in, still in D")))
            :string.Format(Tr("Start by {0}."),Tr(firstReverse?"reversing (R)":"driving forward (D)"));
        string finish=Tr(Scene.FreeGoal?"Finish: stop inside the green lane zone, select P and apply the parking brake.":"Finish: stop inside the green outline, select P and apply the parking brake. You get a report.");
        var lines=new List<string> {goal,how,finish};
        if(Mode==TrainingMode.Test) lines.Add(Tr("Test mode: no target outline, tutor or coaching while you drive."));
        else lines.Add(Tr("Cyan rings on the road mark where to stop or change gear."));
        if(Scene.Features.Count>0) lines.Add(Tr("Watch for: ")+string.Join(" · ",Scene.Features.Select(f=>Tr(f))));
        briefingBody.Text=string.Join("\n\n",lines);
        var pad=Pad;
        briefingButton.Text=TouchEnabled?Tr("Got it"):string.Format(Tr("Got it ({0})"),pad!=null?pad.Park:"Enter");
        briefing.Show();QueueHudLayout();
    }
    void DismissBriefing() {briefing.Hide();UpdateMouseCapture();}
    void LayoutBriefing(Vector2 size,Vector2 pixels,float unit)
    {
        float scale=unit*Mathf.Min(1,Mathf.Min((pixels.X-24)/480,(pixels.Y-24)/Mathf.Max(1,briefing.GetCombinedMinimumSize().Y)));
        briefing.Scale=Vector2.One*scale;briefing.Size=new(480,briefing.GetCombinedMinimumSize().Y);
        briefing.Position=(size-briefing.Size*scale)/2;
        if(IsInstanceValid(touchControls)) touchControls.Blocker=briefing.Visible?briefing.GetGlobalRect():default;
    }
}
