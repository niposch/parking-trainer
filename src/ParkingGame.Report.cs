using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    PanelContainer reportPanel=null!;
    Label reportTitle=null!,reportBody=null!,reportFooter=null!;
    ReportDiagram reportDiagram=null!;
    TouchMenuScroll reportScroll=null!;
    internal Assessment? LastReport;
    internal bool LastTestPassed;
    void SetupReport()
    {
        reportPanel=Panel(hud,new(12,12),new(400,0),cream);reportPanel.MouseFilter=Control.MouseFilterEnum.Stop;
        // Scrolls on short screens so the replay is never cut off.
        reportScroll=new TouchMenuScroll {HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled,MouseFilter=Control.MouseFilterEnum.Pass};reportPanel.AddChild(reportScroll);
        var box=new VBoxContainer();box.AddThemeConstantOverride("separation",6);reportScroll.AddChild(box);
        reportTitle=Text(box,"",20);reportTitle.AutowrapMode=TextServer.AutowrapMode.WordSmart;reportTitle.CustomMinimumSize=new(364,0);
        reportBody=Text(box,"",14);reportBody.AutowrapMode=TextServer.AutowrapMode.WordSmart;reportBody.CustomMinimumSize=new(364,0);
        reportDiagram=new ReportDiagram {CustomMinimumSize=new(364,250)};box.AddChild(reportDiagram);
        reportFooter=Text(box,"",12,new("52675f"));reportFooter.AutowrapMode=TextServer.AutowrapMode.WordSmart;reportFooter.CustomMinimumSize=new(364,0);
        reportPanel.Hide();
    }
    void BuildReport()
    {
        var report=Scene.Assess(Car.Pose);LastReport=report;
        bool test=Mode==TrainingMode.Test;
        LastTestPassed=report.Success && Car.Contacts==0;
        reportTitle.Text=test?Tr(LastTestPassed?"TEST PASSED":"TEST NOT PASSED"):Scene.FreeGoal?Tr(report.Grade switch {Grade.Good=>"FINISHED IN THE LANE",Grade.Acceptable=>"NEARLY THERE · ADJUST YOUR LANE POSITION",_=>"NOT IN THE TARGET LANE YET"}):
            Tr(report.Grade switch {Grade.Good=>"GOOD PARK",Grade.Acceptable=>"ACCEPTABLE · A SMALL CORRECTION WOULD HELP",_=>"RE-PARK RECOMMENDED"});
        var lines=new List<string>();
        if(test && !LastTestPassed) lines.Add(Car.Contacts>0?Tr("A contact fails the test, even if the final position is good."):Tr("The final position is outside the accepted range."));
        if(!Scene.FreeGoal) lines.Add(PositionText(report).Split('\n')[0]);
        else lines.Add(string.Format(Tr("Lane position: {0:0.00} m from the lane centre · heading {1:0.0}° off"),Math.Abs(report.Lateral),Math.Abs(report.Angle*180/Math.PI)));
        if(report.LeftGap is {} left && report.RightGap is {} right)
            lines.Add(string.Format(Tr(Exercise==Exercise.Garage?"Wall gaps: left {0:0.00} m · right {1:0.00} m":"Line gaps: left {0:0.00} m · right {1:0.00} m"),left,right)+(left<0||right<0?"  "+Tr("(over the line)"):""));
        if(report.KerbGap is {} kerb)
            lines.Add(string.Format(Tr(Exercise switch {Exercise.Parallel or Exercise.ReverseCorner=>"Kerb gap {0:0.00} m",Exercise.Garage=>"Gap to the back wall {0:0.00} m",Exercise.TurnInRoad=>"Gap to the kerb {0:0.00} m",_=>"Gap to the kerb at the end of the bay {0:0.00} m"}),kerb));
        if(!Scene.FreeGoal)
        {
            // A door needs roughly 0.6 m to open far enough to get out.
            double Side(int side)=>Math.Min(ParkingProximity.MeasureSide(Car.Pose,Scene.Obstacles,true,side),ParkingProximity.MeasureSide(Car.Pose,Scene.Obstacles,false,side));
            double driver=Side(-1),passenger=Side(1);
            string Door(double d)=>d>=1.99?Tr("clear"):string.Format(Tr(d>=.6?"{0:0.00} m, room to open":"{0:0.00} m, tight"),d);
            lines.Add(string.Format(Tr("Driver door: {0} · passenger door: {1}"),Door(driver),Door(passenger)));
        }
        lines.Add(string.Format(Tr("Contacts {0} · direction changes {1} · top speed {2:0.0} km/h · time {3:0}:{4:00}"),Car.Contacts,DirectionChanges,MaxSpeed*3.6,Math.Floor(Elapsed/60),Elapsed%60));
        if(Car.KerbTouches>0) lines.Add(string.Format(Tr("Gentle kerb touches: {0} (fine at creeping speed)"),Car.KerbTouches));
        double wheelTurns=StationarySteer*12/(2*Math.PI);
        if(wheelTurns>.5) lines.Add(string.Format(Tr("Steering wheel turned {0:0.0} turns at a standstill. Try steering while creeping."),wheelTurns));
        if(Scene.Features.Count>0) lines.Add(Tr("Hazards: ")+string.Join(" · ",Scene.Features.Select(f=>Tr(f))));
        reportBody.Text=string.Join("\n",lines);
        reportFooter.Text=TouchEnabled?Tr("Return to cockpit to correct · Menu for retry or a new variation"):Tr(Pad!=null?"D-pad left / right: scrub the replay · A / B: back to the cockpit to correct":"Left / Right arrows: scrub the replay · Enter: back to the cockpit to correct · F5: retry · F6: new variation");
        reportDiagram.Scene=Scene;reportDiagram.Trail=new(Trail);reportDiagram.Final=Car.Pose;reportDiagram.Grade=report.Grade;
        reportDiagram.ShowGoal=true;reportDiagram.Restart();
    }
    void LayoutReport(Vector2 pixels,float unit)
    {
        float scale=unit*Mathf.Min(1,(pixels.X-24)/400);
        // Phone landscape shows only part of the panel at once: replay first, under the grade.
        var box=reportDiagram.GetParent();
        box.MoveChild(reportDiagram,CompactTouch?1:2);
        reportDiagram.CustomMinimumSize=new(364,CompactTouch?190:250);
        // Stay above the touch Return/zoom buttons (and below the toolbar in portrait).
        float top=TouchEnabled&&!CompactUi?88:12,bottom=TouchEnabled?20+56+8:12;
        float content=((Control)reportScroll.GetChild(0)).GetCombinedMinimumSize().Y+26;
        float height=Mathf.Min(content,(pixels.Y-top-bottom)/(scale/unit));
        reportScroll.CustomMinimumSize=new(364,Mathf.Max(60,height-26));
        reportPanel.Scale=Vector2.One*scale;
        reportPanel.Size=new(400,height);
        reportPanel.Position=new(12*unit,top*unit);
        if(IsInstanceValid(touchControls)) touchControls.Blocker=reportPanel.Visible?reportPanel.GetGlobalRect():BriefingVisible?briefing.GetGlobalRect():default;
    }
}
