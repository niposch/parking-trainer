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
        SetupMenu();
        SetupCockpitDisplays();
        resumeButton.GrabFocus();LayoutHud();
    }
    void Slider(Node parent,string name,double min,double max,double step,double value,Action<double> changed)
    {
        var v=new VBoxContainer {SizeFlagsHorizontal=Control.SizeFlags.ExpandFill};parent.AddChild(v);
        var header=new HBoxContainer();v.AddChild(header);Text(header,name,13).SizeFlagsHorizontal=Control.SizeFlags.ExpandFill;
        var readout=Text(header,value.ToString(step<1?"0.00":"0"),13);
        var slider=new HSlider {MinValue=min,MaxValue=max,Step=step,Value=value,CustomMinimumSize=new(230,20)};v.AddChild(slider);
        slider.ValueChanged+=x=>{readout.Text=x.ToString(step<1?"0.00":"0");changed(x);SaveSettings();};
    }
    void LayoutHud()
    {
        if(!IsInstanceValid(hud)) return;
        var size=GetViewport().GetVisibleRect().Size;
        hud.Scale=new(size.X/1440,size.Y/900);
    }
    string ExerciseName()=>Exercise==Exercise.Parallel?Tr(ExerciseTitle(Exercise)):string.Format(Tr("{0} ({1})"),Tr(ExerciseTitle(Exercise)),Tr(MirrorBay?"right":"left"));
    void UpdateMenu()
    {
        if(!IsInstanceValid(menu)) return;
        menu.Visible=Paused;menuBackdrop.Visible=Paused;
        menuTitle.Text=Tr(MenuPage switch {"scenarios"=>"Choose your scenario","options"=>"Options","controls"=>"Controller setup & controls","map"=>"Teaching map",_=>"Parking Practice"});
        menuSummary.Text=MenuPage switch
        {
            "scenarios"=>Tr("Choose a parking type and how much room you have. Starting a new scenario replaces the current attempt."),
            "options"=>Tr("Display, driving controls and parking assistance."),
            "controls"=>Tr("Check the analog pedals, change bindings and review the controls."),
            "map"=>Tr("An overhead teaching aid while paused. The car's surround-camera view is separate."),
            _=>Scene==null?Tr("Approach, line up and park at your own pace."):string.Format(Tr("{0} · {1} · layout {2}\n{3:0.00} m {4} · Starts secured in P with the parking brake on."),ExerciseName(),DifficultyTitle(Difficulty),Seed,Exercise==Exercise.Parallel?Scene.Gap:Scene.BayWidth,Exercise==Exercise.Parallel?Tr("gap length"):Tr("bay width"))
        };
        mapRect.Visible=Paused && MenuPage=="map";
        tutorButton.Text=Tutor?Tr("Tutor: on"):Tr("Tutor: off");
        guideButton.Text=DynamicGuides?Tr("Guides: steering"):Tr("Guides: fixed");
        steeringButton.Text=RateSteering?Tr("Steering: turn & hold"):Tr("Steering: stick angle");
        muteButton.Text=Muted?Tr("Beepers: muted"):Tr("Beepers: on");
        UpdateDeviceStatus();
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
        int index=Scene.ClosestPath(Car.Pose);
        double progress=(double)Math.Max(0,index-Scene.ApproachEnd)/Math.Max(1,Scene.Path.Count-1-Scene.ApproachEnd);
        if(index<Scene.ApproachEnd-8)
        {
            tutorHeading.Text=Tr("01 / APPROACH & SET UP");
            tutorText.Text=Exercise==Exercise.Parallel?Tr("Select D and drive up to the gap. Leave room beside the parked cars. Choose where to stop beyond the space, observe around you, then hold the brake before selecting R."):Tr("Select D and approach along the aisle. Find the marked bay, choose your lateral clearance and stopping point, and check both sides before turning or selecting R.");
        }
        else if(progress<.08)
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
        menu.Visible=Paused;menuBackdrop.Visible=Paused;
        if(Paused) UpdateDeviceStatus();
    }
}
