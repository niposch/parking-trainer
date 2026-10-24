using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    Control hud=null!;
    internal PanelContainer menu=null!;
    internal Button resumeButton=null!;
    internal TextureRect mapRect=null!;
    TextureRect rearRect=null!,surroundRect=null!;
    Label gearLabel=null!,speedLabel=null!,steerLabel=null!,sensorLabel=null!,noticeLabel=null!,tutorText=null!,cameraLabel=null!,metricsLabel=null!;
    Label menuTitle=null!,menuSummary=null!,deviceLabel=null!;
    PanelContainer tutorPanel=null!;
    Button tutorHeading=null!,hintIcon=null!;
    bool HintCollapsed=false;
    // Short screens (phone landscape): compact headers and panels that avoid the touch controls.
    internal bool CompactUi;
    bool CompactTouch=>CompactUi && TouchEnabled;
    Label hintKey=null!;
    void ToggleHint() {HintCollapsed=Tutor?!HintCollapsed:false;Tutor=true;ApplyHintLayout();SaveSettings();}
    void ApplyHintLayout()
    {
        tutorText.Visible=!HintCollapsed;
        bool tutor=Tutor&&Mode!=TrainingMode.Test;
        tutorPanel.Visible=tutor&&!HintCollapsed&&!Paused&&!Inspection&&!BriefingVisible&&!(CompactTouch&&CoachText!="");
        hintIcon.Visible=tutor&&HintCollapsed&&!Paused&&!Inspection&&!TouchEnabled;
        hintIcon.Size=new(44,44);
    }
    Button tutorButton=null!,guideButton=null!,steeringButton=null!,muteButton=null!;
    Color cream=new("f0f0e5"),ink=new("14262b"),accent=new("cff59f");
    StyleBoxFlat Style(Color color,Color? border=null,int radius=12)
    {
        return new StyleBoxFlat {BgColor=color,BorderColor=border??color,BorderWidthBottom=1,BorderWidthTop=1,BorderWidthLeft=1,BorderWidthRight=1,
            CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius,CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,
            ContentMarginLeft=18,ContentMarginRight=18,ContentMarginTop=13,ContentMarginBottom=13};
    }
    Label Text(Node parent,string text,int size,Color? color=null)
    {
        var label=new Label {Text=Tr(text),MouseFilter=Control.MouseFilterEnum.Ignore};label.AddThemeFontSizeOverride("font_size",size);
        label.AddThemeColorOverride("font_color",color??ink);parent.AddChild(label);return label;
    }
    PanelContainer Panel(Node parent,Vector2 position,Vector2 size,Color color)
    {
        var panel=new PanelContainer {Position=position,Size=size,MouseFilter=Control.MouseFilterEnum.Ignore};
        panel.AddThemeStyleboxOverride("panel",Style(color));parent.AddChild(panel);return panel;
    }
    Button Button(Node parent,string text,Action pressed)
    {
        var button=new Button {MouseFilter=Control.MouseFilterEnum.Pass,Text=Tr(text),ClipText=true,TextOverrunBehavior=TextServer.OverrunBehavior.TrimEllipsis,CustomMinimumSize=new(0,44),MouseDefaultCursorShape=Control.CursorShape.PointingHand};
        button.AddThemeFontSizeOverride("font_size",17);button.AddThemeColorOverride("font_color",ink);
        button.AddThemeColorOverride("font_hover_color",ink);button.AddThemeColorOverride("font_pressed_color",ink);button.AddThemeColorOverride("font_focus_color",ink);
        button.AddThemeStyleboxOverride("normal",Style(new("e0e3d8"),null,7));
        button.AddThemeStyleboxOverride("hover",Style(accent,null,7));button.AddThemeStyleboxOverride("pressed",Style(new("b0db87"),null,7));
        button.AddThemeStyleboxOverride("focus",new StyleBoxFlat {BgColor=Colors.Transparent,BorderColor=new("456b34"),BorderWidthBottom=3,BorderWidthTop=3,BorderWidthLeft=3,BorderWidthRight=3,CornerRadiusBottomLeft=7,CornerRadiusBottomRight=7,CornerRadiusTopLeft=7,CornerRadiusTopRight=7});
        button.Pressed+=()=>pressed();parent.AddChild(button);return button;
    }
    TextureRect Image(Control parent,SubViewport viewport,bool flip=false)
    {
        var rect=new TextureRect {Texture=viewport.GetTexture(),ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale,MouseFilter=Control.MouseFilterEnum.Ignore};
        if(flip) rect.Material=FlipImage();parent.AddChild(rect);return rect;
    }
    void SetupHud()
    {
        var layer=new CanvasLayer();AddChild(layer);
        hud=new Control {LayoutMode=0};hud.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);layer.AddChild(hud);
        // Fixed design space follows the actual viewport without changing camera FOV.
        GetViewport().SizeChanged+=QueueHudLayout;
        tutorPanel=Panel(hud,new(22,22),new(370,210),cream);var tv=new VBoxContainer();tutorPanel.AddChild(tv);
        tutorHeading=Button(tv,"01 / SET UP",ToggleHint);tutorHeading.Alignment=HorizontalAlignment.Left;tutorHeading.AddThemeFontSizeOverride("font_size",14);tutorText=Text(tv,"",16);tutorText.AutowrapMode=TextServer.AutowrapMode.WordSmart;tutorText.CustomMinimumSize=new(330,130);
        metricsLabel=Text(tv,"",13,new("52675f"));
        metricsLabel.Hide();
        hintIcon=Button(hud,"?",ToggleHint);hintIcon.TooltipText=Tr("Show parking instructions (H / T / X)");
        hintIcon.CustomMinimumSize=new(44,44);hintIcon.Size=new(44,44);
        var iconStyle=Style(cream,null,22);iconStyle.ContentMarginLeft=iconStyle.ContentMarginRight=iconStyle.ContentMarginTop=iconStyle.ContentMarginBottom=0;
        hintIcon.AddThemeStyleboxOverride("normal",iconStyle);
        hintIcon.AddThemeFontSizeOverride("font_size",22);
        // Desktop shows the toggle key as a small badge inside the 44 px icon.
        hintKey=Text(hintIcon,"H",11,new("456b34"));hintKey.Position=new(30,26);hintKey.Size=new(12,16);
        ApplyHintLayout();
        noticeLabel=Text(hud,"",18,cream);noticeLabel.Position=new(470,800);noticeLabel.Size=new(500,65);noticeLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        noticeLabel.AddThemeColorOverride("font_shadow_color",ink);noticeLabel.AddThemeConstantOverride("shadow_offset_x",1);noticeLabel.AddThemeConstantOverride("shadow_offset_y",2);
        SetupShortcutPanel();
        SetupMenu();
        menu.MinimumSizeChanged+=QueueHudLayout;
        SetupReport();
        SetupOnboarding();
        SetupContactFeedback();
        SetupWorldCues();
        SetupBriefing();
        SetupCockpitDisplays();
        SetupTouchControls();
        resumeButton.GrabFocus();LayoutHud();
    }
    void Slider(Node parent,string name,double min,double max,double step,double value,Action<double> changed)
    {
        var v=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};parent.AddChild(v);
        var header=new HBoxContainer();v.AddChild(header);Text(header,name,13).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        var readout=Text(header,value.ToString(step<1?"0.00":"0"),13);
        var slider=new HSlider {MouseFilter=Control.MouseFilterEnum.Pass,MinValue=min,MaxValue=max,Step=step,Value=value,CustomMinimumSize=new(230,20)};v.AddChild(slider);
        slider.ValueChanged+=x=>{readout.Text=x.ToString(step<1?"0.00":"0");changed(x);SaveSettings();};
    }
    bool hudLayoutPending=true;
    Vector2 lastHudSize;
    float lastHudUnit;
    void QueueHudLayout()=>hudLayoutPending=true;
    void RefreshHudLayout()
    {
        // SizeChanged can precede the engine's new screen transform/DPI and the
        // container minimum-size pass. Recheck the settled inputs each frame.
        if(!IsInstanceValid(hud)) return;
        var size=GetViewport().GetVisibleRect().Size;
        float unit=InterfaceLayout.Unit(GetViewport());
        if(hudLayoutPending || !size.IsEqualApprox(lastHudSize) || Math.Abs(unit-lastHudUnit)>.001f) LayoutHud();
    }
    void LayoutHud()
    {
        if(!IsInstanceValid(hud)) return;
        var size=GetViewport().GetVisibleRect().Size;
        float unit=InterfaceLayout.Unit(GetViewport());
        hudLayoutPending=false;lastHudSize=size;lastHudUnit=unit;
        var pixels=size/unit;
        hud.Scale=Vector2.One;
        var area=MenuArea(pixels);
        var menuSize=new Vector2(Mathf.Min(860,area.Size.X),Mathf.Min(700,area.Size.Y));
        bool narrow=menuSize.X<700;
        scenarioCards.Columns=narrow?2:4;
        foreach(var row in responsiveRows) row.Vertical=narrow;
        FitMenu(size,unit);
        UpdateShortcutPanel();
        CompactUi=pixels.Y<520;
        menuTitle.AddThemeFontSizeOverride("font_size",CompactUi?20:narrow?22:28);
        menuSummary.Visible=MenuPage!="home" && !CompactUi;
        foreach(var card in exerciseCards) card.CustomMinimumSize=new(0,CompactUi?122:150);
        foreach(var diagram in parkingDiagrams) diagram.CustomMinimumSize=new(0,CompactUi?62:92);
        homeDiagram.CustomMinimumSize=new(0,CompactUi?64:96);
        LayoutTutor(size,pixels,unit);
        hintIcon.Scale=Vector2.One*unit;hintIcon.Position=new(12*unit,12*unit);
        noticeLabel.Scale=Vector2.One*unit;
        if(CompactTouch)
        {
            // Phone landscape: notices go in the free gap between steering pad and pedals.
            var steer=InterfaceLayout.TouchRegions(size,unit,false)["steer"];var brake=InterfaceLayout.TouchRegions(size,unit,false)["brake"];
            float left=steer.End.X/unit+8,right=brake.Position.X/unit-8;
            noticeLabel.Size=new(Mathf.Max(160,right-left),70);
            noticeLabel.Position=new(left*unit,size.Y-90*unit);
        }
        else
        {
            noticeLabel.Size=new(Mathf.Min(520,pixels.X-24),70);
            noticeLabel.Position=new((size.X-noticeLabel.Size.X*unit)/2,size.Y-(TouchEnabled?260:ControlHints?220:90)*unit);
        }
        LayoutOnboarding(size,pixels,unit);LayoutContact(size,pixels,unit);LayoutBriefing(size,pixels,unit);
        touchControls?.ResizeToViewport();
    }
    string ExerciseName()=>Exercise==Exercise.TurnInRoad?Tr(ExerciseTitle(Exercise)):string.Format(Tr("{0} ({1})"),Tr(ExerciseTitle(Exercise)),Tr(MirrorBay?"right":"left"));
    void UpdateMenu()
    {
        if(!IsInstanceValid(menu)) return;
        menu.Visible=Paused;menuBackdrop.Visible=Paused;
        menuTitle.Text=Tr(MenuPage switch {"scenarios"=>"Choose your scenario","options"=>"Options","controls"=>"Controller setup & controls","map"=>"Teaching map","howto"=>"How to drive",_=>"Parking Practice"});
        if(Scene!=null)
        {
            homeDiagram.Exercise=Exercise;homeDiagram.Mirrored=MirrorBay;homeDiagram.QueueRedraw();
            homeTitle.Text=ExerciseName();
            homeDetails.Text=string.Format(Tr("{0} · {1} · {2:0.00} m {3} · {4} coaching · layout {5}"),Tr(DrillTitle(Scene.Drill)),DifficultyTitle(Difficulty),Scene.Space,Tr(SpaceName(Exercise)),Tr(ModeTitle(Mode)),Seed);
            homeHazards.Text=Scene.Features.Count>0?Tr("Hazards: ")+string.Join(" · ",Scene.Features.Select(f=>Tr(f))):"";
            homeHazards.Visible=homeHazards.Text!="";
        }
        menuFooter.Visible=!TouchEnabled;
        // The home card already says what the scenario is; short screens need the room.
        menuSummary.Visible=MenuPage!="home" && !CompactUi;
        menuSummary.Text=MenuPage switch
        {
            "scenarios"=>Tr("Choose a parking type and how much room you have. Starting a new scenario replaces the current attempt."),
            "options"=>Tr("Display, driving controls and parking assistance."),
            "controls"=>Tr("Check the analog pedals, change bindings and review the controls."),
            "map"=>Tr("An overhead teaching aid while paused. The car's surround-camera view is separate."),
            "howto"=>Tr("Controls for keyboard and controller. On touch screens, use the on-screen pedals, wheel and gear buttons."),
            _=>Scene==null?Tr("Approach, line up and park at your own pace."):string.Format(Tr("{0} · {1} · {2} · {3} · layout {4}\n{5:0.00} m {6} · Starts secured in P with the parking brake on."),ExerciseName(),Tr(DrillTitle(Scene.Drill)),DifficultyTitle(Difficulty),Tr(ModeTitle(Mode)),Seed,Scene.Space,Tr(SpaceName(Exercise)))
                +(Scene.Features.Count>0?"\n"+Tr("Hazards: ")+string.Join(" · ",Scene.Features.Select(f=>Tr(f))):"")
        };
        mapRect.Visible=Paused && MenuPage=="map";
        tutorButton.Text=Tutor?Tr("Tutor: on"):Tr("Tutor: off");
        guideButton.Text=DynamicGuides?Tr("Guides: steering"):Tr("Guides: fixed");
        steeringButton.Text=RateSteering?Tr("Steering: turn & hold"):Tr("Steering: stick angle");
        muteButton.Text=Muted?Tr("Beepers: muted"):Tr("Beepers: on");
        hintsButton.Text=ControlHints?Tr("Key bar & next-step hints: on"):Tr("Key bar & next-step hints: off");
        cuesButton.Text=WorldCues?Tr("3D cues (checkpoints, wheel tracks, clearance): on"):Tr("3D cues (checkpoints, wheel tracks, clearance): off");
        UpdateDeviceStatus();
    }
    void UpdateHud()
    {
        RefreshTouchControls();
        UpdateShortcutPanel();
        if(Paused)
        {
            // Containers may lower their minimum size a frame after reflow.
            // Reapply the fit once that deferred layout has completed.
            var size=GetViewport().GetVisibleRect().Size;
            float unit=InterfaceLayout.Unit(GetViewport());
            FitMenu(size,unit);
        }
        gearLabel.Text=Car.Gear.ToString();speedLabel.Text=string.Format(Tr("{0:0.0}"),Math.Abs(Car.Speed)*3.6);
        steerLabel.Text=Car.ParkingBrake?"(P)":"";
        string distance(double d)=>d>=1.99?"—":$"{d:0.00} m";
        sensorLabel.Text=string.Format(Tr("FRONT {0}     REAR {1}"),distance(FrontRange),distance(RearRange));
        sensorLabel.Modulate=Math.Min(FrontRange,RearRange)<.3?new Color("ff997a"):Colors.White;
        rearRect.Visible=CameraMode==0;surroundRect.Visible=CameraMode==1;
        sensorGraphic.SetRanges(FrontSectors,RearSectors,SideSectors);
        cameraLabel.Text=CameraMode==0?Tr("REAR + SENSORS"):Tr("SURROUND + SENSORS");
        noticeLabel.Text=Notice!=""?Tr(Notice):Demo?Tr("DEMONSTRATION · observe the mirrors"):successTime>.8&&Mode!=TrainingMode.Test?Tr(Scene.Assess(Car.Pose).Grade==Grade.Good?"Position looks good. Select P and apply the parking brake.":"Acceptable position. A small correction would make it good."):"";
        if(Inspection) noticeLabel.Text=CompactTouch?"":Tr(TouchEnabled?"PARKING INSPECTION · drag to orbit":"PARKING INSPECTION · mouse / right stick to orbit · wheel to zoom");

        int index=Scene.ClosestPath(Car.Pose);
        double progress=(double)Math.Max(0,index-Scene.ApproachEnd)/Math.Max(1,Scene.Path.Count-1-Scene.ApproachEnd);
        var step=TutorStep(index,progress);
        tutorHeading.Text=Tr(step.Heading);tutorText.Text=Tr(step.Text);
        string hintKeyName=Pad?.Hint??"H";
        hintKey.Text=Pad!=null?"Up":"H";hintKey.Visible=!TouchEnabled;
        tutorHeading.Text=(TouchEnabled?"[-] ":string.Format(Tr("[{0}: hide] "),hintKeyName))+tutorHeading.Text;
        ApplyHintLayout();
        if(IsInstanceValid(touchControls)) touchControls.HintHeader=tutorPanel.Visible?tutorHeading.GetGlobalRect():default;
        UpdateOnboarding();
        var viewSize=GetViewport().GetVisibleRect().Size;float viewUnit=InterfaceLayout.Unit(GetViewport());
        LayoutOnboarding(viewSize,viewSize/viewUnit,viewUnit);
        if(briefing.Visible && (Car.Travel>.5 || Inspection || Paused)) DismissBriefing();
        LayoutBriefing(viewSize,viewSize/viewUnit,viewUnit);
        menu.Visible=Paused;menuBackdrop.Visible=Paused;
        if(Paused) UpdateDeviceStatus();
    }
    static string SpaceName(Exercise exercise)=>exercise switch {Exercise.Parallel=>"gap length",Exercise.Garage=>"garage width",Exercise.ReverseCorner=>"side-road width",Exercise.TurnInRoad=>"road width",_=>"bay width"};
    static string DrillTitle(Drill drill)=>drill switch {Drill.Correct=>"Fix an off-centre park",Drill.DriveOut=>"Drive out",_=>"Park"};
    static string ModeTitle(TrainingMode mode)=>mode switch {TrainingMode.Hints=>"Hints only",TrainingMode.Test=>"Test",_=>"Guided"};
    const string SecureText="Centre the wheels and creep to a good stopping position. Use mirrors and the camera together. Hold the brake, select P, then apply the parking brake.";
    (string Heading,string Text) TutorStep(int index,double progress)
    {
        int phase=Scene.Drill==Drill.Park && index<Scene.ApproachEnd-8?0:progress<.08?1:progress<.52?2:progress<.9?3:4;
        bool parallel=Exercise==Exercise.Parallel;
        if(Scene.Drill==Drill.Correct) return phase switch
        {
            <=1=>("01 / ASSESS THE POSITION","You are parked, but not well. Compare both mirrors with the lines or kerb, check the camera, and decide which way the car has to move."),
            2=>(parallel?"02 / CREEP BACK, STEERING IN":"02 / PULL OUT STRAIGHT",parallel?"You are too far from the kerb. Reverse slowly, steering toward the kerb, then the other way to straighten. The rear follows your steering.":"Wheel straight, creep out one to two metres. That gives the car room to shift sideways."),
            3=>("03 / SHIFT BACK IN","Turn toward the side you need, then the other way, while creeping back in. Reversing, the rear follows your steering; driving forward, the front leads and the rear cuts in."),
            _=>("04 / STRAIGHTEN & SECURE",SecureText)
        };
        if(Scene.Drill==Drill.DriveOut) return phase switch
        {
            <=1=>("01 / OBSERVE","Check both mirrors and over your shoulder before moving. Traffic in the aisle or lane has priority."),
            2=>("02 / CREEP OUT",parallel?"Back up a little if needed, then steer away from the kerb as you creep forward. Watch your front corner against the car ahead.":Exercise is Exercise.ForwardBay or Exercise.AngledBay?"Reverse out slowly with the wheel straight until your mirrors pass the neighbouring cars.":"Creep forward with the wheel straight until your mirrors have passed the neighbours or the walls."),
            3=>("03 / SWING CLEAR","Turn into the lane once the body is clear of the neighbours. The inside rear corner cuts in when you turn forward; check it in the mirror."),
            _=>("04 / DRIVE OFF & STOP","Straighten in the lane and stop inside the green zone. Select P and apply the parking brake to finish.")
        };
        string side=Tr(Scene.Mirrored?"left":"right");
        return (phase,Exercise) switch
        {
            (0,Exercise.Parallel)=>("01 / APPROACH & SET UP","Select D and drive up to the gap. Leave room beside the parked cars. Choose where to stop beyond the space, observe around you, then hold the brake before selecting R."),
            (0,Exercise.AngledBay)=>("01 / APPROACH & SET UP","Select D and drive along the aisle. Angled bays are entered forward, in the direction of travel. Keep about a metre from the parked cars."),
            (0,Exercise.Garage)=>("01 / APPROACH & SET UP","Select D and drive past the garage. Line up so the opening is straight behind you before reversing: the walls leave no room to swing in."),
            (0,Exercise.ReverseCorner)=>("01 / DRIVE PAST THE CORNER",string.Format(Tr("Select D and drive past the side road on your {0}, close to the kerb. Stop a few metres past the corner."),side)),
            (0,Exercise.TurnInRoad)=>("01 / CHOOSE A SAFE SPOT","Select D and drive along the road. Stop close to the kerb where you can see both ways, then check mirrors and blind spots."),
            (0,_)=>("01 / APPROACH & SET UP","Select D and approach along the aisle. Find the marked bay, choose your lateral clearance and stopping point, and check both sides before turning or selecting R."),
            (1,Exercise.Parallel)=>("01 / SET UP & OBSERVE","Find the rear corner of the car beside you. Check the gap and your mirror on the kerb side. Brake held; select R, then release the parking brake."),
            (1,Exercise.AngledBay)=>("01 / SET UP & OBSERVE","Check the angled lines and both neighbours. Start turning when your mirror is level with the near line of your bay."),
            (1,Exercise.Garage)=>("01 / SET UP & OBSERVE","Look over your shoulder and in both mirrors: the garage opening must be straight behind you. Select R with the brake held."),
            (1,Exercise.ReverseCorner)=>("01 / SET UP & OBSERVE","Look over your shoulder at the corner. Select R and start reversing slowly along the kerb, about half a metre from it."),
            (1,Exercise.TurnInRoad)=>("02 / FORWARD, FULL LOCK","Creep forward and turn the wheel fully toward the far side. Stop before the front reaches the far kerb."),
            (1,_)=>("01 / SET UP & OBSERVE","Check both bay lines and the cars on either side. Observe around the car before moving. Hold the brake while choosing D or R."),
            (2,Exercise.Parallel)=>("02 / SLOW MOVEMENT, ACTIVE LOOKING","Reverse toward the gap. The rear moves toward your steering; the nose swings out. Alternate the kerb-side mirror, rear view, and front clearance."),
            (2,Exercise.AngledBay)=>("02 / TURN IN","Turn in steadily. A 60-degree bay needs less steering than a 90-degree one. Watch the inside front corner against the neighbour."),
            (2,Exercise.Garage)=>("02 / REVERSE STRAIGHT IN","Reverse at creeping speed. Watch both mirrors: the gaps to the walls should stay equal. Do not rely on the camera alone."),
            (2,Exercise.ReverseCorner)=>("02 / FOLLOW THE KERB","When the rear wheel reaches the start of the curve, steer toward the kerb. Keep the kerb at a constant distance in the mirror."),
            (2,Exercise.TurnInRoad)=>("03 / REVERSE, OPPOSITE LOCK","Select R and turn the wheel fully the other way while creeping back. Look over your shoulder and stop before the kerb behind."),
            (2,_)=>("02 / SLOW MOVEMENT, ACTIVE LOOKING","Turn slowly toward your bay. Compare the near bay line in the door mirror with the car's side. Keep watching the opposite front corner."),
            (3,Exercise.Parallel)=>("03 / ALIGN & CORRECT","As the rear enters, countersteer to bring the nose in. Watch the curb and the rear car. Stop and pull forward to correct if needed."),
            (3,Exercise.Garage)=>("03 / ALIGN & CORRECT","Correct drift early with small steering movements. Stop clear of the back wall with room to open the doors."),
            (3,Exercise.ReverseCorner)=>("03 / STRAIGHTEN","Unwind the steering as the car becomes parallel to the side road's kerb. Keep reversing a car length into the side road."),
            (3,Exercise.TurnInRoad)=>("04 / FORWARD INTO YOUR LANE","Select D, turn fully toward the far side again and drive forward. Straighten into the lane on the new side of the road."),
            (3,_)=>("03 / ALIGN & CORRECT","As the body becomes parallel with the bay lines, unwind the steering. Compare both mirrors. A small correction is fine."),
            (_,Exercise.TurnInRoad)=>("05 / SETTLE & SECURE","Straighten in your lane, stop inside the green zone, select P and apply the parking brake."),
            _=>("04 / STRAIGHTEN & SECURE",SecureText)
        };
    }
    // Free space for top-left panels on touch screens: left of the toolbar and,
    // in landscape, above the gear row.
    (float Width,float Top,float Height) TouchPanelArea(Vector2 pixels)
    {
        if(!TouchEnabled) return (370,12,pixels.Y-24);
        if(!CompactUi) return (Mathf.Min(370,pixels.X-24),88,pixels.Y-88-12);
        float toolbarLeft=pixels.X-20-56*3-8*3-72;
        float gearTop=pixels.Y-20-Mathf.Min(160,pixels.Y*.42f)-8-56;
        return (Mathf.Clamp(toolbarLeft-24,240,560),12,gearTop-12-8);
    }
    void LayoutTutor(Vector2 size,Vector2 pixels,float unit)
    {
        var area=TouchPanelArea(pixels);
        bool compact=CompactTouch;
        tutorText.CustomMinimumSize=compact?new(area.Width-40,0):new(330,130);
        tutorPanel.Size=new(compact?area.Width:370,0);
        tutorPanel.Size=new(tutorPanel.Size.X,tutorPanel.GetCombinedMinimumSize().Y);
        float fit=Mathf.Min(1,Mathf.Min((pixels.X-24)/tutorPanel.Size.X,area.Height/Mathf.Max(1,tutorPanel.Size.Y)));
        tutorPanel.Scale=Vector2.One*unit*fit;tutorPanel.Position=new(12*unit,area.Top*unit);
    }
}
