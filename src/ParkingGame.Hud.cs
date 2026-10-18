using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    Control hud=null!;
    internal PanelContainer menu=null!;
    internal Button resumeButton=null!;
    internal TextureRect mapRect=null!;
    TextureRect rearRect=null!,surroundRect=null!;
    Label gearLabel=null!,speedLabel=null!,steerLabel=null!,sensorLabel=null!,noticeLabel=null!,tutorHeading=null!,tutorText=null!,cameraLabel=null!,metricsLabel=null!;
    Label menuTitle=null!,menuSummary=null!,deviceLabel=null!;
    PanelContainer tutorPanel=null!;
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
        var button=new Button {Text=Tr(text),CustomMinimumSize=new(0,42),MouseDefaultCursorShape=Control.CursorShape.PointingHand};
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
        hud.Resized+=LayoutHud;
        tutorPanel=Panel(hud,new(22,22),new(370,210),cream);var tv=new VBoxContainer();tutorPanel.AddChild(tv);
        tutorHeading=Text(tv,"01 / SET UP",16);tutorText=Text(tv,"",16);tutorText.AutowrapMode=TextServer.AutowrapMode.WordSmart;tutorText.CustomMinimumSize=new(330,130);
        metricsLabel=Text(tv,"",13,new("52675f"));
        metricsLabel.Hide();
        noticeLabel=Text(hud,"",18,cream);noticeLabel.Position=new(470,800);noticeLabel.Size=new(500,65);noticeLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        noticeLabel.AddThemeColorOverride("font_shadow_color",ink);noticeLabel.AddThemeConstantOverride("shadow_offset_x",1);noticeLabel.AddThemeConstantOverride("shadow_offset_y",2);
        menu=Panel(hud,new(410,138),new(620,665),cream);menu.MouseFilter=Control.MouseFilterEnum.Stop;
        var scroll=new ScrollContainer {CustomMinimumSize=new(0,620),FollowFocus=true,HorizontalScrollMode=ScrollContainer.ScrollMode.Disabled};menu.AddChild(scroll);
        var mv=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};mv.AddThemeConstantOverride("separation",7);scroll.AddChild(mv);
        menuTitle=Text(mv,"A LITTLE SPACE. A LOT OF PRACTICE.",22);menuSummary=Text(mv,"",16);menuSummary.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        var scenarioRow=new HBoxContainer();mv.AddChild(scenarioRow);
        var prev=Button(scenarioRow,"←",()=>CycleExercise(-1));prev.CustomMinimumSize=new(52,42);
        var next=Button(scenarioRow,"Change parking exercise →",()=>CycleExercise(1));next.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        resumeButton=Button(mv,"Drive / Resume",()=>Pause(false));
        resumeButton.AddThemeStyleboxOverride("normal",Style(accent,null,7));
        var rr=new HBoxContainer();mv.AddChild(rr);
        Button(rr,"Retry same setup",()=>NewScenario(false)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(rr,"New variation",()=>NewScenario(true)).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        var aidRow=new HBoxContainer();mv.AddChild(aidRow);
        tutorButton=Button(aidRow,"",()=>{Tutor=!Tutor;UpdateMenu();SaveSettings();});tutorButton.SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        Button(aidRow,"Watch demonstration",StartDemo).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        guideButton=Button(mv,"",()=>{DynamicGuides=!DynamicGuides;UpdateMenu();});
        steeringButton=Button(mv,"",()=>{RateSteering=!RateSteering;UpdateMenu();SaveSettings();});
        var sliders=new HBoxContainer();mv.AddChild(sliders);
        Slider(sliders,"Field of view",55,100,1,Fov,v=>Fov=(float)v);
        Slider(sliders,"Eye height (m)",1.04,1.30,.01,EyeHeight,v=>EyeHeight=(float)v);
        Slider(mv,"Stick dead zone",.02,.25,.01,Deadzone,v=>Deadzone=(float)v);
        muteButton=Button(mv,"",()=>{Muted=!Muted;UpdateMenu();SaveSettings();});
        Button(mv,"Teaching map · pause to inspect",()=>{mapRect.Visible=!mapRect.Visible;}).TooltipText=Tr("An overhead teaching aid, not the car's surround camera.");
        deviceLabel=Text(mv,"",13);deviceLabel.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        pedalStatus=Text(mv,"",15);
        var pedalRow=new HBoxContainer();mv.AddChild(pedalRow);
        Button(pedalRow,"Bind L2 brake",()=>BeginPedalBinding(BrakeAction));
        Button(pedalRow,"Bind R2 gas",()=>BeginPedalBinding(GasAction));
        Button(pedalRow,"Reset pedals",ResetPedalBindings);
        var help=Text(mv,"RT gas · LT brake · left stick turn · right stick look\nLB R · RB D · A gear · Y display · X tutor · B console\nD-pad: menus only · right-stick click: parking brake\nKeyboard: W/S pedals · A/D steer · mouse look\n1 P · 2 R · 3 N · 4 D · Space parking brake\nZ centre wheel · C shoulder · V console · Home forward\nTab display · T tutor · Esc pause · F5 retry · F6 new",13);help.AutowrapMode=TextServer.AutowrapMode.WordSmart;
        Text(mv,"Prototype 01 · generic car · uncalibrated sightlines",12,new("66756b"));
        mapRect=Image(hud,mapViewport);mapRect.Position=new(1045,270);mapRect.Size=new(365,350);mapRect.Visible=false;
        SetupCockpitDisplays();
        resumeButton.GrabFocus();LayoutHud();
    }
    void Slider(Node parent,string name,double min,double max,double step,double value,Action<double> changed)
    {
        var v=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};parent.AddChild(v);Text(v,name,13);
        var slider=new HSlider {MinValue=min,MaxValue=max,Step=step,Value=value,CustomMinimumSize=new(230,20)};v.AddChild(slider);
        slider.ValueChanged+=x=>{changed(x);SaveSettings();};
    }
    void LayoutHud()
    {
        if(!IsInstanceValid(hud)) return;
        var size=GetViewport().GetVisibleRect().Size;
        hud.Scale=new(size.X/1440,size.Y/900);
    }
    void CycleExercise(int direction)
    {
        Exercise=(Exercise)(((int)Exercise+direction+3)%3);NewScenario(false);
    }
    string ExerciseName()=>Exercise switch {Exercise.Parallel=>Tr("Reverse parallel"),Exercise.ReverseBay=>Tr("Reverse 90° bay"),_=>Tr("Forward 90° bay")};
    void UpdateMenu()
    {
        if(!IsInstanceValid(menu)) return;
        menu.Visible=Paused;
        menuSummary.Text=string.Format(Tr("{0} · variation {1}\nStarts in P with parking brake ON. Hold the brake, select R/D, then release the parking brake."),ExerciseName(),Seed);
        tutorButton.Text=Tutor?Tr("Tutor: on"):Tr("Tutor: off");
        guideButton.Text=DynamicGuides?Tr("Rear guides: follow steering"):Tr("Rear guides: fixed");
        steeringButton.Text=RateSteering?Tr("Steering: turn & hold (stick release holds angle)"):Tr("Steering: stick angle (returns to centre)");
        muteButton.Text=Muted?Tr("Parking beepers: muted"):Tr("Parking beepers: on");
        UpdateDeviceStatus();
        if(Paused) resumeButton.GrabFocus();
    }
    void UpdateHud()
    {
        gearLabel.Text=Car.Gear.ToString();speedLabel.Text=string.Format(Tr("{0:0.0}"),Math.Abs(Car.Speed)*3.6);
        steerLabel.Text=Car.ParkingBrake?"(P)":"";
        string distance(double d)=>d>=1.99?"—":$"{d:0.00} m";
        sensorLabel.Text=string.Format(Tr("FRONT {0}     REAR {1}"),distance(FrontRange),distance(RearRange));
        sensorLabel.Modulate=Math.Min(FrontRange,RearRange)<.3?new Color("ff997a"):Colors.White;
        rearRect.Visible=CameraMode==0;surroundRect.Visible=CameraMode==1;
        sensorGraphic.SetRanges(FrontSectors,RearSectors);
        cameraLabel.Text=CameraMode==0?Tr("REAR + SENSORS"):Tr("SURROUND + SENSORS");
        noticeLabel.Text=Notice!=""?Tr(Notice):Demo?Tr("DEMONSTRATION · observe the mirrors"):successTime>.8?Tr("Position looks good. Select P and apply the parking brake."):"";
        if(Inspection) noticeLabel.Text=Tr("PARKING INSPECTION · mouse / right stick to orbit\nMouse wheel to zoom · B / Enter to return to cockpit");
        if(!Inspection && successTime>.8 && Car.Gear==Gear.P && Car.ParkingBrake) noticeLabel.Text=Tr("PARKED. Take your time, or try a new variation.");
        tutorPanel.Visible=Tutor&&!Paused&&!Inspection;
        int index=Scene.ClosestPath(Car.Pose);double progress=(double)index/Math.Max(1,Scene.Path.Count-1);
        if(progress<.08)
        {
            tutorHeading.Text=Tr("01 / SET UP & OBSERVE");
            tutorText.Text=Exercise==Exercise.Parallel?Tr("Find the rear corner of the car beside you. Check the gap and your right mirror. Brake held; select R, then release the parking brake."):Tr("Check both bay lines and the cars on either side. Observe around the car before moving. Hold the brake while choosing D or R.");
        }
        else if(progress<.52)
        {
            tutorHeading.Text=Tr("02 / SLOW MOVEMENT, ACTIVE LOOKING");
            tutorText.Text=Exercise==Exercise.Parallel?Tr("Reverse toward the gap. The rear moves toward your steering; the nose swings out. Alternate the right mirror, rear view, and front clearance."):Tr("Turn slowly toward your bay. Compare the near bay line in the door mirror with the car's side. Keep watching the opposite front corner.");
        }
        else if(progress<.90)
        {
            tutorHeading.Text=Tr("03 / ALIGN & CORRECT");
            tutorText.Text=Exercise==Exercise.Parallel?Tr("As the rear enters, countersteer to bring the nose in. Watch the curb and the rear car. Stop and pull forward to correct if needed."):Tr("As the body becomes parallel with the bay lines, unwind the steering. Compare both mirrors. A small correction is fine.");
        }
        else
        {
            tutorHeading.Text=Tr("04 / STRAIGHTEN & SECURE");
            tutorText.Text=Tr("Centre the wheels and creep to a good stopping position. Use mirrors and the camera together. Hold the brake, select P, then apply the parking brake.");
        }
        metricsLabel.Text=string.Format(Tr("Goal offset {0:0.00} m · angle {1:0}° · contacts {2}"),Scene.PositionError(Car.Pose),DrivingModel.AngleError(Car.Pose.Yaw,Scene.Goal.Yaw)*180/Math.PI,Car.Contacts);
        menu.Visible=Paused;
        if(Paused) UpdateDeviceStatus();
    }
}
