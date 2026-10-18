using Godot;
namespace ParkingTrainer;

// Desktop/web builds do not consistently enable the engine's mobile drag path.
// Use real touch events and preserve normal taps, wheel and controller handling.
internal partial class TouchMenuScroll : ScrollContainer
{
    int finger=-1,startScroll;
    float startY;
    bool dragging;
    public override void _Ready() {Resized+=CancelTouch;VisibilityChanged+=CancelTouch;}
    public void CancelTouch()
    {
        if(dragging) PropagateNotification((int)Control.NotificationScrollEnd);
        finger=-1;dragging=false;
    }
    public override void _GuiInput(InputEvent input)
    {
        if(input.Device==InputEvent.DeviceIdEmulation) return;
        if(input is InputEventScreenTouch touch)
        {
            if(touch.Pressed && finger<0) {finger=touch.Index;startY=touch.Position.Y;startScroll=ScrollVertical;dragging=false;}
            else if(!touch.Pressed && touch.Index==finger) CancelTouch();
        }
        if(input is InputEventScreenDrag drag && drag.Index==finger)
        {
            float delta=drag.Position.Y-startY;
            if(!dragging && Math.Abs(delta)>8)
            {
                dragging=true;
                // BaseButton cancels its pending click on this notification.
                PropagateNotification((int)Control.NotificationScrollBegin);
            }
            if(dragging) {ScrollVertical=startScroll-(int)Mathf.Round(delta);AcceptEvent();}
        }
    }
}
