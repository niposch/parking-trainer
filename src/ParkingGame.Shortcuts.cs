using Godot;
namespace ParkingTrainer;

public partial class ParkingGame
{
    const string KeyboardHelp="1–4: P / R / N / D (hold brake)\nW / S: Gas / brake\nA / D: Steering\nSpace: Parking brake\nMouse: Look around\nTab: Rear / 360° camera\nV / Home: Console / forward view\nC: Look over shoulder\nZ: Centre steering\nH / T: Parking instructions\nF: Correction plan\nG: Steering / fixed camera guides\nF5 / F6: Retry / new variation\nEsc: Pause / release mouse";
    PanelContainer shortcutPanel=null!;
    void SetupShortcutPanel()
    {
        shortcutPanel=Panel(hud,Vector2.Zero,new(306,0),new Color(.02f,.045f,.05f,.32f));
        var background=Style(new Color(.02f,.045f,.05f,.32f),new Color(.8f,.9f,.9f,.12f),8);
        background.ContentMarginLeft=background.ContentMarginRight=12;
        background.ContentMarginTop=background.ContentMarginBottom=10;
        shortcutPanel.AddThemeStyleboxOverride("panel",background);
        var content=new VBoxContainer();content.AddThemeConstantOverride("separation",6);shortcutPanel.AddChild(content);
        var heading=Text(content,"KEYBOARD SHORTCUTS",12,cream);
        var keys=Text(content,KeyboardHelp,13,cream);keys.CustomMinimumSize=new(282,0);keys.AddThemeConstantOverride("line_spacing",0);
        foreach(var label in new[]{heading,keys})
        {
            label.AddThemeColorOverride("font_shadow_color",new Color(0,0,0,.8f));
            label.AddThemeConstantOverride("shadow_offset_x",1);label.AddThemeConstantOverride("shadow_offset_y",1);
        }
        shortcutPanel.MouseFilter=Control.MouseFilterEnum.Ignore;
    }
    Rect2 MenuArea(Vector2 pixels)
    {
        var area=new Rect2(12,12,pixels.X-24,pixels.Y-24);
        if(TouchEnabled || !IsInstanceValid(shortcutPanel)) return area;
        if(pixels.X>=680) area.Size=new(pixels.X-354,area.Size.Y);
        else area.Size=new(area.Size.X,Mathf.Max(120,pixels.Y-shortcutPanel.GetCombinedMinimumSize().Y-36));
        return area;
    }
    void FitMenu(Vector2 size,float unit)
    {
        var area=MenuArea(size/unit);
        menu.Scale=Vector2.One*unit;
        menu.Size=new(Mathf.Min(860,area.Size.X),Mathf.Min(700,area.Size.Y));
        menu.Position=(area.Position+(area.Size-menu.Size)/2)*unit;
    }
    void UpdateShortcutPanel()
    {
        if(!IsInstanceValid(shortcutPanel)) return;
        shortcutPanel.Visible=!TouchEnabled;
        var size=GetViewport().GetVisibleRect().Size;float unit=InterfaceLayout.Unit(GetViewport());
        var pixels=size/unit;
        shortcutPanel.Scale=Vector2.One*unit;
        shortcutPanel.Size=new(306,shortcutPanel.GetCombinedMinimumSize().Y);
        shortcutPanel.Position=pixels.X>=680?new(size.X-(shortcutPanel.Size.X+12)*unit,12*unit):
            new((size.X-shortcutPanel.Size.X*unit)/2,size.Y-(shortcutPanel.Size.Y+12)*unit);
    }
}
