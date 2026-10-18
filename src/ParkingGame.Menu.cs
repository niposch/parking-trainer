using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    internal string MenuPage="home";
    internal Exercise SelectedExercise;
    internal bool MirrorBay,SelectedMirrorBay;
    readonly Button[] baySideButtons=new Button[2];
    readonly ParkingDiagram[] parkingDiagrams=new ParkingDiagram[3];
    VBoxContainer baySideChoices=null!;
    internal Difficulty Difficulty=Difficulty.Standard,SelectedDifficulty=Difficulty.Standard;
    internal int DisplayMode;
    internal OptionButton displaySelector=null!;
    Vector2I windowedSize=new(1280,800),windowedPosition=new(100,80);
    readonly Dictionary<string,VBoxContainer> menuPages=new();
    readonly Button[] exerciseCards=new Button[3],difficultyButtons=new Button[3];
    Button menuBack=null!,scenarioStart=null!,controlsFirst=null!;
    Label scenarioDetails=null!;
    ScrollContainer menuScroll=null!;
    ColorRect menuBackdrop=null!;

    void SetupMenu()
    {
        menuBackdrop=new ColorRect {Color=new(0.025f,.05f,.055f,.65f),MouseFilter=Control.MouseFilterEnum.Stop};
        hud.AddChild(menuBackdrop);menuBackdrop.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        menu=Panel(hud,new(290,100),new(860,700),cream);menu.MouseFilter=Control.MouseFilterEnum.Stop;
        var root=new VBoxContainer();root.AddThemeConstantOverride("separation",12);menu.AddChild(root);
        var header=new HBoxContainer();header.AddThemeConstantOverride("separation",18);root.AddChild(header);
        menuBack=Button(header,"Back",BackOrResume);
        menuTitle=Text(header,"Parking Practice",28);menuTitle.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        menuSummary=Text(root,"",16);menuSummary.AutowrapMode=TextServer.AutowrapMode.WordSmart;menuSummary.CustomMinimumSize=new(0,50);
        root.AddChild(new HSeparator());
        menuScroll=new ScrollContainer {CustomMinimumSize=new(0,475),FollowFocus=true,SizeFlagsVertical=Control.SizeFlags.ExpandFill,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};root.AddChild(menuScroll);
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
        Button(home,"Options",()=>ShowMenuPage("options"));
        var retry=new HBoxContainer();retry.AddThemeConstantOverride("separation",12);home.AddChild(retry);
        Button(retry,"Retry this layout",()=>NewScenario(false)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(retry,"New variation",()=>NewScenario(true)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(home,"Watch approach + parking demonstration",StartDemo);
        Button(home,"Exit game",()=>{SaveSettings();GetTree().Quit();});

        var scenarios=Page("scenarios");
        Text(scenarios,"PARKING TYPE",13,new("52675f"));
        var cards=new HBoxContainer();cards.AddThemeConstantOverride("separation",12);scenarios.AddChild(cards);
        foreach(Exercise exercise in Enum.GetValues<Exercise>())
        {
            var card=Button(cards,"",()=>{SelectedExercise=exercise;RefreshScenarioSelection();});
            card.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;card.CustomMinimumSize=new(250,200);exerciseCards[(int)exercise]=card;
            var contents=new VBoxContainer {Position=new(12,10),MouseFilter=Control.MouseFilterEnum.Ignore};card.AddChild(contents);
            contents.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);contents.OffsetLeft=12;contents.OffsetRight=-12;contents.OffsetTop=8;contents.OffsetBottom=-8;
            var diagram=new ParkingDiagram {Exercise=exercise,CustomMinimumSize=new(220,130),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
            contents.AddChild(diagram);parkingDiagrams[(int)exercise]=diagram;
            var title=Text(contents,ExerciseTitle(exercise),17);title.HorizontalAlignment=HorizontalAlignment.Center;
            var note=Text(contents,exercise==Exercise.ForwardBay?"Drive into the bay":"Reverse into the space",13,new("52675f"));note.HorizontalAlignment=HorizontalAlignment.Center;
        }
        var choices=new HBoxContainer();choices.AddThemeConstantOverride("separation",20);scenarios.AddChild(choices);
        var difficultyChoices=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill,SizeFlagsStretchRatio=2};choices.AddChild(difficultyChoices);
        Text(difficultyChoices,"SPACE DIFFICULTY",13,new("52675f"));
        var difficulties=new HBoxContainer();difficulties.AddThemeConstantOverride("separation",12);difficultyChoices.AddChild(difficulties);
        foreach(Difficulty difficulty in Enum.GetValues<Difficulty>())
        {
            var button=Button(difficulties,DifficultyTitle(difficulty),()=>{SelectedDifficulty=difficulty;RefreshScenarioSelection();});
            button.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;difficultyButtons[(int)difficulty]=button;
        }
        baySideChoices=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};choices.AddChild(baySideChoices);
        Text(baySideChoices,"BAY SIDE",13,new("52675f"));
        var sides=new HBoxContainer();sides.AddThemeConstantOverride("separation",12);baySideChoices.AddChild(sides);
        for(int i=0;i<2;i++)
        {
            bool mirrored=i==1;var button=Button(sides,mirrored?"Right":"Left",()=>{SelectedMirrorBay=mirrored;RefreshScenarioSelection();});
            button.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;button.TooltipText=Tr("Side of the aisle during your approach.");baySideButtons[i]=button;
        }
        scenarioDetails=Text(scenarios,"",16);scenarioDetails.AutowrapMode=TextServer.AutowrapMode.WordSmart;scenarioDetails.CustomMinimumSize=new(0,62);
        scenarioStart=Button(scenarios,"Start practice",BeginSelectedScenario);scenarioStart.CustomMinimumSize=new(0,52);
        scenarioStart.AddThemeStyleboxOverride("normal",Style(accent,null,7));

        var options=Page("options");
        Text(options,"DISPLAY",13,new("52675f"));
        displaySelector=new OptionButton {CustomMinimumSize=new(0,44),SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};
        foreach(string label in new[]{"Windowed","Borderless fullscreen","Fullscreen"}) displaySelector.AddItem(Tr(label));
        displaySelector.AddThemeFontSizeOverride("font_size",17);displaySelector.AddThemeColorOverride("font_color",ink);displaySelector.AddThemeColorOverride("font_hover_color",ink);displaySelector.AddThemeColorOverride("font_pressed_color",ink);displaySelector.AddThemeColorOverride("font_focus_color",ink);
        displaySelector.AddThemeStyleboxOverride("normal",Style(new("e0e3d8"),null,7));displaySelector.AddThemeStyleboxOverride("hover",Style(accent,null,7));displaySelector.AddThemeStyleboxOverride("pressed",Style(accent,null,7));
        displaySelector.Select(DisplayMode);displaySelector.ItemSelected+=index=>ApplyWindowMode((int)index);options.AddChild(displaySelector);
        var views=new HBoxContainer();views.AddThemeConstantOverride("separation",24);options.AddChild(views);
        Slider(views,"Field of view",55,100,1,Fov,v=>Fov=(float)v);
        Slider(views,"Eye height (m)",1.04,1.30,.01,EyeHeight,v=>EyeHeight=(float)v);
        Text(options,"DRIVING & ASSISTANCE",13,new("52675f"));
        steeringButton=Button(options,"",()=>{RateSteering=!RateSteering;UpdateMenu();SaveSettings();});
        Slider(options,"Stick dead zone",.02,.25,.01,Deadzone,v=>Deadzone=(float)v);
        var aids=new HBoxContainer();aids.AddThemeConstantOverride("separation",12);options.AddChild(aids);
        tutorButton=Button(aids,"",()=>{Tutor=!Tutor;UpdateMenu();SaveSettings();});
        guideButton=Button(aids,"",()=>{DynamicGuides=!DynamicGuides;UpdateMenu();SaveSettings();});
        muteButton=Button(aids,"",()=>{Muted=!Muted;UpdateMenu();SaveSettings();});
        foreach(var toggle in new[]{tutorButton,guideButton,muteButton}) toggle.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Slider(options,"Beeper volume (%)",0,100,5,BeeperVolume*100,v=>BeeperVolume=(float)v/100);
        var tools=new HBoxContainer();tools.AddThemeConstantOverride("separation",12);options.AddChild(tools);
        Button(tools,"Controller setup & controls",()=>ShowMenuPage("controls")).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(tools,"Teaching map",()=>ShowMenuPage("map")).TooltipText=Tr("A paused teaching aid, separate from the car's surround camera.");

        tools.GetChild<Godot.Button>(1).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;

        var controls=Page("controls");
        deviceLabel=Text(controls,"",14);deviceLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        pedalStatus=Text(controls,"",16);
        var pedals=new HBoxContainer();pedals.AddThemeConstantOverride("separation",10);controls.AddChild(pedals);
        controlsFirst=Button(pedals,"Bind L2 brake",()=>BeginPedalBinding(BrakeAction));
        Button(pedals,"Bind R2 gas",()=>BeginPedalBinding(GasAction));Button(pedals,"Reset pedals",ResetPedalBindings);
        var help=Text(controls,"RT gas · LT brake · left stick turn · right stick look\nLB R · RB D · A gear · Y display · X tutor · B console\nRight-stick click: parking brake · D-pad: menus only\n\nW/S pedals · A/D steer · mouse look\n1 P · 2 R · 3 N · 4 D · Space parking brake\nZ centre wheel · C shoulder · V console · Home forward\nTab display · T tutor · Esc pause/back · F5 retry · F6 new",16);
        help.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var map=Page("map");mapRect=Image(map,mapViewport);mapRect.CustomMinimumSize=new(0,430);mapRect.StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered;
        Text(root,"D-pad / arrows: navigate   ·   A / Enter: select   ·   B / Esc: back",13,new("52675f"));
        ShowMenuPage("home");
    }
    static string ExerciseTitle(Exercise exercise)=>exercise switch {Exercise.Parallel=>"Reverse parallel",Exercise.ReverseBay=>"Reverse 90° bay",_=>"Forward 90° bay"};
    string DifficultyTitle(Difficulty difficulty)=>Tr(difficulty.ToString());
    void OpenScenarioSelection()
    {
        SelectedExercise=Exercise;SelectedDifficulty=Difficulty;SelectedMirrorBay=MirrorBay;RefreshScenarioSelection();ShowMenuPage("scenarios");
    }
    void RefreshScenarioSelection()
    {
        for(int i=0;i<3;i++)
        {
            exerciseCards[i].AddThemeStyleboxOverride("normal",Style(i==(int)SelectedExercise?accent:new Color("e0e3d8"),i==(int)SelectedExercise?new Color("456b34"):null,7));
            difficultyButtons[i].AddThemeStyleboxOverride("normal",Style(i==(int)SelectedDifficulty?accent:new Color("e0e3d8"),null,7));
        }
        baySideChoices.Visible=SelectedExercise!=Exercise.Parallel;
        for(int i=0;i<2;i++) baySideButtons[i].AddThemeStyleboxOverride("normal",Style((i==1)==SelectedMirrorBay?accent:new Color("e0e3d8"),null,7));
        foreach(var diagram in parkingDiagrams) {diagram.Mirrored=SelectedMirrorBay;diagram.QueueRedraw();}
        var range=Scenario.SpaceRange(SelectedExercise,SelectedDifficulty);
        scenarioDetails.Text=string.Format(Tr(SelectedExercise==Exercise.Parallel?"Gap length: {0:0.00}–{1:0.00} m. Your car is 4.45 m long.\nDrive up, choose your stopping point and line up before reversing.":"Bay width: {0:0.00}–{1:0.00} m. Your car is 1.80 m wide.\nApproach along the aisle; the bay is on your {2}."),range.Min,range.Max,Tr(SelectedMirrorBay?"right":"left"));
    }
    void BeginSelectedScenario()
    {
        Exercise=SelectedExercise;Difficulty=SelectedDifficulty;MirrorBay=SelectedMirrorBay;NewScenario(true);SaveSettings();Pause(false);
    }
    void ShowMenuPage(string page)
    {
        MenuPage=page;
        if(page=="map") FrameTeachingMap();
        foreach(var pair in menuPages) pair.Value.Visible=pair.Key==page;
        menuBack.Visible=page!="home";menuScroll.ScrollVertical=0;
        UpdateMenu();
        Control focus=page switch {"scenarios"=>exerciseCards[(int)SelectedExercise],"options"=>displaySelector,"controls"=>controlsFirst,"map"=>menuBack,_=>resumeButton};
        focus.GrabFocus();
    }
    void BackOrResume()
    {
        if(MenuPage=="home") Pause(false);else ShowMenuPage(MenuPage is "controls" or "map"?"options":"home");
    }
    internal void ApplyWindowMode(int value,bool save=true)
    {
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
