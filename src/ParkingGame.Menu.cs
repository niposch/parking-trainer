using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal string MenuPage="home";
    internal Exercise SelectedExercise;
    internal bool MirrorBay,SelectedMirrorBay;
    readonly Button[] baySideButtons=new Button[2];
    readonly ParkingDiagram[] parkingDiagrams=new ParkingDiagram[7];
    VBoxContainer baySideChoices=null!,aisleChoices=null!;
    internal Drill Drill=Drill.Park,SelectedDrill=Drill.Park;
    internal Clutter Clutter=Clutter.None,SelectedClutter=Clutter.None;
    internal bool OppositeRow,SelectedOppositeRow;
    internal TrainingMode SelectedMode=TrainingMode.Guided;
    Button[] drillButtons=null!,aisleButtons=null!,clutterButtons=null!,modeButtons=null!;
    Button hintsButton=null!,cuesButton=null!;
    internal Difficulty Difficulty=Difficulty.Standard,SelectedDifficulty=Difficulty.Standard;
    internal int DisplayMode;
    internal OptionButton displaySelector=null!;
    Vector2I windowedSize=new(1280,800),windowedPosition=new(100,80);
    readonly Dictionary<string,VBoxContainer> menuPages=new();
    readonly Button[] exerciseCards=new Button[7],difficultyButtons=new Button[3];
    Button menuBack=null!,scenarioStart=null!,controlsFirst=null!;
    Label scenarioDetails=null!;
    TouchMenuScroll menuScroll=null!;
    ColorRect menuBackdrop=null!;
    GridContainer scenarioCards=null!;
    readonly List<BoxContainer> responsiveRows=new();
    BoxContainer Row(Node parent,int spacing=12)
    {
        var row=new BoxContainer();row.AddThemeConstantOverride("separation",spacing);parent.AddChild(row);responsiveRows.Add(row);return row;
    }

    void SetupMenu()
    {
        menuBackdrop=new ColorRect {Color=new(0.025f,.05f,.055f,.65f),MouseFilter=Control.MouseFilterEnum.Stop};
        hud.AddChild(menuBackdrop);menuBackdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        menu=Panel(hud,new(290,100),new(860,700),cream);menu.MouseFilter=Control.MouseFilterEnum.Stop;
        var root=new VBoxContainer();root.AddThemeConstantOverride("separation",12);menu.AddChild(root);
        var header=new HBoxContainer();header.AddThemeConstantOverride("separation",18);root.AddChild(header);
        menuBack=Button(header,"Back",BackOrResume);
        menuBack.CustomMinimumSize=new(90,44);
        menuTitle=Text(header,"Parking Practice",28);menuTitle.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        menuTitle.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        menuSummary=Text(root,"",16);menuSummary.AutowrapMode=TextServer.AutowrapMode.WordSmart;menuSummary.CustomMinimumSize=new(0,50);
        root.AddChild(new HSeparator());
        menuScroll=new TouchMenuScroll {FollowFocus=true,SizeFlagsVertical=Control.SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};root.AddChild(menuScroll);
        var host=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};menuScroll.AddChild(host);
        VBoxContainer Page(string name)
        {
            var box=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};box.AddThemeConstantOverride("separation",12);
            host.AddChild(box);menuPages[name]=box;return box;
        }
        var home=Page("home");
        resumeButton=Button(home,"Continue driving",()=>Pause(false));resumeButton.CustomMinimumSize=new(0,58);
        resumeButton.AddThemeStyleboxOverride("normal",Style(accent,null,7));
        Button(home,"Choose scenario",OpenScenarioSelection);
        Button(home,"Surprise me: random scenario",StartRandomMix).TooltipText=Tr("A random parking type, side, size, task and obstacles.");
        Button(home,"How to drive",()=>ShowMenuPage("howto"));
        Button(home,"Options",()=>ShowMenuPage("options"));
        var retry=Row(home);
        Button(retry,"Retry this layout",()=>NewScenario(false)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(retry,"New variation",()=>NewScenario(true)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(home,"Watch approach + parking demonstration",StartDemo);
        if(IsWeb) Text(home,"Continue driving enables audio. With touch controls off, click to capture the mouse. Esc pauses and releases it.",14).AutowrapMode=TextServer.AutowrapMode.WordSmart;
        else Button(home,"Exit game",()=>{SaveSettings();GetTree().Quit();});

        var scenarios=Page("scenarios");
        Text(scenarios,"PARKING TYPE",13,new("52675f"));
        var cards=new GridContainer {Columns=4};cards.AddThemeConstantOverride("h_separation",10);cards.AddThemeConstantOverride("v_separation",10);scenarios.AddChild(cards);scenarioCards=cards;
        foreach(Exercise exercise in Enum.GetValues<Exercise>())
        {
            var card=Button(cards,"",()=>{SelectedExercise=exercise;RefreshScenarioSelection();});
            card.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;card.CustomMinimumSize=new(0,150);exerciseCards[(int)exercise]=card;
            var contents=new VBoxContainer {Position=new(10,8),MouseFilter=Control.MouseFilterEnum.Ignore};card.AddChild(contents);
            contents.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);contents.OffsetLeft=10;contents.OffsetRight=-10;contents.OffsetTop=6;contents.OffsetBottom=-6;
            var diagram=new ParkingDiagram {Exercise=exercise,CustomMinimumSize=new(0,92),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
            contents.AddChild(diagram);parkingDiagrams[(int)exercise]=diagram;
            var title=Text(contents,ExerciseTitle(exercise),15);title.HorizontalAlignment=HorizontalAlignment.Center;title.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        }
        Button[] Choices(Node parent,string title,string[] labels,Action<int> pick,out VBoxContainer box)
        {
            box=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};parent.AddChild(box);
            Text(box,title,13,new("52675f"));
            var row=new HBoxContainer();row.AddThemeConstantOverride("separation",10);box.AddChild(row);
            var buttons=new Button[labels.Length];
            for(int i=0;i<labels.Length;i++)
            {
                int index=i;buttons[i]=Button(row,labels[i],()=>{pick(index);RefreshScenarioSelection();});
                buttons[i].SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
            }
            return buttons;
        }
        drillButtons=Choices(scenarios,"TASK",new[]{"Park","Fix an off-centre park","Drive out"},i=>SelectedDrill=(Drill)i,out _);
        var choices=Row(scenarios,20);
        var difficulty=Choices(choices,"SPACE DIFFICULTY",new[]{"Spacious","Standard","Tight"},i=>SelectedDifficulty=(Difficulty)i,out var difficultyBox);
        difficultyBox.SizeFlagsStretchRatio=2;
        for(int i=0;i<3;i++) difficultyButtons[i]=difficulty[i];
        var sides=Choices(choices,"SIDE",new[]{"Left","Right"},i=>SelectedMirrorBay=i==1,out baySideChoices);
        for(int i=0;i<2;i++) {baySideButtons[i]=sides[i];sides[i].TooltipText=Tr("Side of the road or aisle during your approach.");}
        var extras=Row(scenarios,20);
        aisleButtons=Choices(extras,"AISLE",new[]{"Open lot","Cars opposite"},i=>SelectedOppositeRow=i==1,out aisleChoices);
        clutterButtons=Choices(extras,"OBSTACLES",new[]{"None","Some","Busy"},i=>SelectedClutter=(Clutter)i,out _);
        modeButtons=Choices(scenarios,"COACHING",new[]{"Guided","Hints only","Test"},i=>SelectedMode=(TrainingMode)i,out _);
        modeButtons[0].TooltipText=Tr("Tutor, landmark, live coaching and correction plans.");
        modeButtons[1].TooltipText=Tr("Tutor text; coaching only once you stop. F shows a correction plan.");
        modeButtons[2].TooltipText=Tr("No tutor, coaching, target outline or demonstration. Graded at the end.");
        scenarioDetails=Text(scenarios,"",15);scenarioDetails.AutowrapMode=TextServer.AutowrapMode.WordSmart;scenarioDetails.CustomMinimumSize=new(0,62);
        var start=Row(scenarios,12);
        scenarioStart=Button(start,"Start practice",BeginSelectedScenario);scenarioStart.CustomMinimumSize=new(0,52);scenarioStart.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        scenarioStart.AddThemeStyleboxOverride("normal",Style(accent,null,7));
        Button(start,"Surprise me",StartRandomMix).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;

        var options=Page("options");
        Text(options,"DISPLAY",13,new("52675f"));
        displaySelector=new OptionButton {MouseFilter=Control.MouseFilterEnum.Pass,CustomMinimumSize=new(0,44),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        foreach(string label in IsWeb?new[]{"In browser tab","Fullscreen"}:new[]{"Windowed","Borderless fullscreen","Fullscreen"}) displaySelector.AddItem(Tr(label));
        displaySelector.AddThemeFontSizeOverride("font_size",17);displaySelector.AddThemeColorOverride("font_color",ink);displaySelector.AddThemeColorOverride("font_hover_color",ink);displaySelector.AddThemeColorOverride("font_pressed_color",ink);displaySelector.AddThemeColorOverride("font_focus_color",ink);
        displaySelector.AddThemeStyleboxOverride("normal",Style(new("e0e3d8"),null,7));displaySelector.AddThemeStyleboxOverride("hover",Style(accent,null,7));displaySelector.AddThemeStyleboxOverride("pressed",Style(accent,null,7));
        displaySelector.Select(DisplayMode);displaySelector.ItemSelected+=index=>ApplyWindowMode((int)index);options.AddChild(displaySelector);
        Text(options,"ON-SCREEN CONTROLS",13,new("52675f"));
        touchSelector=new OptionButton {MouseFilter=Control.MouseFilterEnum.Pass,CustomMinimumSize=new(0,44),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        foreach(string label in new[]{"Auto (touch devices)","On","Off"}) touchSelector.AddItem(Tr(label));
        touchSelector.Select(TouchMode);touchSelector.ItemSelected+=index=>SetTouchMode((int)index);options.AddChild(touchSelector);
        var views=Row(options,24);
        Slider(views,"Field of view",55,100,1,Fov,v=>Fov=(float)v);
        Slider(views,"Eye height (m)",1.04,1.30,.01,EyeHeight,v=>EyeHeight=(float)v);
        Text(options,"DRIVING & ASSISTANCE",13,new("52675f"));
        steeringButton=Button(options,"",()=>{RateSteering=!RateSteering;UpdateMenu();SaveSettings();});
        Slider(options,"Stick dead zone",.02,.25,.01,Deadzone,v=>Deadzone=(float)v);
        var aids=Row(options);
        tutorButton=Button(aids,"",()=>{Tutor=!Tutor;UpdateMenu();SaveSettings();});
        guideButton=Button(aids,"",()=>{DynamicGuides=!DynamicGuides;UpdateMenu();SaveSettings();});
        muteButton=Button(aids,"",()=>{Muted=!Muted;UpdateMenu();SaveSettings();});
        foreach(var toggle in new[]{tutorButton,guideButton,muteButton}) toggle.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        hintsButton=Button(options,"",()=>{ControlHints=!ControlHints;UpdateMenu();QueueHudLayout();SaveSettings();});
        cuesButton=Button(options,"",()=>{WorldCues=!WorldCues;UpdateMenu();SaveSettings();});
        Slider(options,"Beeper volume (%)",0,100,5,BeeperVolume*100,v=>BeeperVolume=(float)v/100);
        var tools=Row(options);
        Button(tools,"Controller setup & controls",()=>ShowMenuPage("controls")).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(tools,"Teaching map",()=>ShowMenuPage("map")).TooltipText=Tr("A paused teaching aid, separate from the car's surround camera.");

        tools.GetChild<Godot.Button>(1).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;

        var controls=Page("controls");
        deviceLabel=Text(controls,"",14);deviceLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        pedalStatus=Text(controls,"",16);
        pedalStatus.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var pedals=Row(controls,10);
        controlsFirst=Button(pedals,"Bind L2 brake",()=>BeginPedalBinding(BrakeAction));
        Button(pedals,"Bind R2 gas",()=>BeginPedalBinding(GasAction));Button(pedals,"Bind steering",()=>BeginPedalBinding(SteerAction));Button(pedals,"Reset bindings",ResetPedalBindings);
        var help=Text(controls,"RT gas · LT brake · left stick steer (click: centre) · right stick look (click: forward)\nRB D · LB R · A P · Y parking brake · X rear / 360° camera\nB camera screen · View over the shoulder · D-pad up instructions · D-pad down correction plan · Start menu\nReport: A / B cockpit · D-pad left/right replay · LB / RB zoom\n\nW/S pedals · A/D steer · mouse look\n1 P · 2 R · 3 N · 4 D · Space parking brake\nZ centre wheel · C shoulder · V console · Home forward\nTab display · H/T instructions · F correction plan · Esc pause/back · F5 retry · F6 new",16);
        help.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var howTo=Page("howto");
        var howToText=Text(howTo,HowToText,16);howToText.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var strip=new ControlStrip {SizeFlagsHorizontal=Control.SizeFlags.ShrinkCenter};howTo.AddChild(strip);
        strip.Set(0,1,0,0,Gear.D,false,null);
        howToFirst=Button(howTo,"Start driving",()=>{seenHowTo=true;SaveSettings();if(MenuPage=="howto") Pause(false);});
        howToFirst.AddThemeStyleboxOverride("normal",Style(accent,null,7));
        var map=Page("map");mapRect=Image(map,mapViewport);mapRect.CustomMinimumSize=new(0,430);mapRect.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
        Text(root,"D-pad / arrows: navigate   ·   A / Enter: select   ·   B / Esc: back",13,new("52675f")).AutowrapMode=TextServer.AutowrapMode.WordSmart;
        ShowMenuPage("home");
    }
    Button howToFirst=null!;
    static string ExerciseTitle(Exercise exercise)=>exercise switch {Exercise.Parallel=>"Reverse parallel",Exercise.ReverseBay=>"Reverse 90° bay",Exercise.ForwardBay=>"Forward 90° bay",
        Exercise.AngledBay=>"Forward 60° angled bay",Exercise.Garage=>"Reverse into a garage",Exercise.ReverseCorner=>"Reverse around a corner",_=>"Turn in the road"};
    string DifficultyTitle(Difficulty difficulty)=>Tr(difficulty.ToString());
    void OpenScenarioSelection()
    {
        SelectedExercise=Exercise;SelectedDifficulty=Difficulty;SelectedMirrorBay=MirrorBay;SelectedDrill=Drill;SelectedClutter=Clutter;SelectedOppositeRow=OppositeRow;SelectedMode=Mode;
        RefreshScenarioSelection();ShowMenuPage("scenarios");
    }
    void RefreshScenarioSelection()
    {
        void Mark(Button button,bool selected,bool enabled=true)
        {
            button.Disabled=!enabled;
            button.AddThemeStyleboxOverride("normal",Style(selected?accent:new Color(enabled?"e0e3d8":"c9ccc2"),selected?new Color("456b34"):null,7));
            button.AddThemeColorOverride("font_disabled_color",new Color("8a958f"));
        }
        if(!Scenario.Supports(SelectedExercise,SelectedDrill)) SelectedDrill=Drill.Park;
        for(int i=0;i<exerciseCards.Length;i++) Mark(exerciseCards[i],i==(int)SelectedExercise);
        for(int i=0;i<3;i++)
        {
            Mark(difficultyButtons[i],i==(int)SelectedDifficulty);
            Mark(drillButtons[i],i==(int)SelectedDrill,Scenario.Supports(SelectedExercise,(Drill)i));
            Mark(clutterButtons[i],i==(int)SelectedClutter);
            Mark(modeButtons[i],i==(int)SelectedMode);
        }
        for(int i=0;i<2;i++) {Mark(baySideButtons[i],(i==1)==SelectedMirrorBay);Mark(aisleButtons[i],(i==1)==SelectedOppositeRow);}
        baySideChoices.Visible=SelectedExercise!=Exercise.TurnInRoad;
        aisleChoices.Visible=Scenario.HasRow(SelectedExercise);
        foreach(var diagram in parkingDiagrams) {diagram.Mirrored=SelectedMirrorBay;diagram.QueueRedraw();}
        var range=Scenario.SpaceRange(SelectedExercise,SelectedDifficulty);
        string side=Tr(SelectedMirrorBay?"right":"left"),kerbSide=Tr(SelectedMirrorBay?"left":"right");
        string space=SelectedExercise switch
        {
            Exercise.Parallel=>string.Format(Tr("Gap length: {0:0.00}–{1:0.00} m. Your car is 4.45 m long. The kerb is on your {2}."),range.Min,range.Max,kerbSide),
            Exercise.Garage=>string.Format(Tr("Garage width: {0:0.00}–{1:0.00} m. Your car is 2.22 m wide across the mirrors. The garage is on your {2}."),range.Min,range.Max,side),
            Exercise.ReverseCorner=>string.Format(Tr("Side-road width: {0:0.00}–{1:0.00} m. The side road is on your {2}."),range.Min,range.Max,kerbSide),
            Exercise.TurnInRoad=>string.Format(Tr("Road width kerb to kerb: {0:0.00}–{1:0.00} m. Turn round using forward and reverse gears."),range.Min,range.Max),
            _=>string.Format(Tr("Bay width: {0:0.00}–{1:0.00} m. Your car is 1.80 m wide. The bay is on your {2}."),range.Min,range.Max,side)
        };
        string task=Tr(SelectedDrill switch
        {
            Drill.Correct=>"You start parked off-centre and crooked. Fix it with as few moves as you can.",
            Drill.DriveOut=>"You start parked. Drive out without touching anything and stop in the green lane zone.",
            _=>"Drive up, choose your stopping point and line up before the manoeuvre."
        });
        if(Scenario.HasRow(SelectedExercise) && SelectedOppositeRow) task+=" "+string.Format(Tr("Aisle: {0:0.0}–{1:0.0} m wide; you may need several moves."),Scenario.AisleRange(SelectedDifficulty).Min,Scenario.AisleRange(SelectedDifficulty).Max);
        scenarioDetails.Text=space+"\n"+task;
    }
    void BeginSelectedScenario()
    {
        Exercise=SelectedExercise;Difficulty=SelectedDifficulty;MirrorBay=SelectedMirrorBay;Drill=SelectedDrill;Clutter=SelectedClutter;OppositeRow=SelectedOppositeRow;Mode=SelectedMode;
        NewScenario(true);SaveSettings();Pause(false);
    }
    void StartRandomMix()
    {
        var rng=Random.Shared;
        SelectedExercise=(Exercise)rng.Next(7);SelectedDifficulty=(Difficulty)rng.Next(3);SelectedMirrorBay=rng.Next(2)==1;
        var drills=Enum.GetValues<Drill>().Where(d=>Scenario.Supports(SelectedExercise,d)).ToArray();
        SelectedDrill=rng.NextDouble()<.6?Drill.Park:drills[rng.Next(drills.Length)];
        SelectedClutter=(Clutter)rng.Next(1,3);SelectedOppositeRow=rng.Next(2)==1;SelectedMode=Mode;
        BeginSelectedScenario();
        Message(string.Format(Tr("Random scenario: {0} · {1}"),ExerciseName(),Tr(DrillTitle(Drill))));
    }
    void ShowMenuPage(string page)
    {
        menuScroll.CancelTouch();
        MenuPage=page;QueueHudLayout();
        if(page=="map") FrameTeachingMap();
        foreach(var pair in menuPages) pair.Value.Visible=pair.Key==page;
        menuBack.Visible=page!="home";menuScroll.ScrollVertical=0;
        UpdateMenu();
        Control focus=page switch {"scenarios"=>exerciseCards[(int)SelectedExercise],"options"=>displaySelector,"controls"=>controlsFirst,"map"=>menuBack,"howto"=>howToFirst,_=>resumeButton};
        focus.GrabFocus();
    }
    void BackOrResume()
    {
        if(MenuPage=="home") Pause(false);
        else {if(MenuPage=="howto") {seenHowTo=true;SaveSettings();}ShowMenuPage(MenuPage is "controls" or "map"?"options":"home");}
    }
    internal void ApplyWindowMode(int value,bool save=true)
    {
        if(IsWeb)
        {
            DisplayMode=Math.Clamp(value,0,1);
            DisplayServer.WindowSetMode(DisplayMode==1?DisplayServer.WindowMode.Fullscreen:DisplayServer.WindowMode.Windowed);
            if(IsInstanceValid(displaySelector)) displaySelector.Select(DisplayMode);
            if(save) SaveSettings();
            return;
        }
        value=Math.Clamp(value,0,2);
        if(DisplayServer.GetName()!="headless")
        {
            if(DisplayServer.WindowGetMode()==DisplayServer.WindowMode.Windowed && DisplayMode==0 && value!=0)
            {windowedSize=DisplayServer.WindowGetSize();windowedPosition=DisplayServer.WindowGetPosition();}
            DisplayServer.WindowSetMode(value switch {1=>DisplayServer.WindowMode.Fullscreen,2=>DisplayServer.WindowMode.ExclusiveFullscreen,_=>DisplayServer.WindowMode.Windowed});
            if(value==0)
            {
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless,false);
                DisplayServer.WindowSetSize(windowedSize);DisplayServer.WindowSetPosition(windowedPosition);
            }
        }
        DisplayMode=value;if(IsInstanceValid(displaySelector)) displaySelector.Select(value);
        if(save) SaveSettings();
    }
}
