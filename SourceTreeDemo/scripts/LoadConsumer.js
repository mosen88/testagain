
[Optimize(true)]
function LC_OnReset( sender : Demo3D.Visuals.BoxVisual )
{
	sender.LoadsConsumed = 0;
}


[Message("OnProcessComplete")]
[Optimize(false)]
function LC_OnProcess( sender : Visual, messageSender : Visual )
{
	Wait( sender.DelayBeforeDeletion.NextDouble() );

	LC_OnProcess2(sender, messageSender);
}

[Optimize(true)]
function LC_OnProcess2( sender : Visual, messageSender : Visual ) 
{
	var transferState : TransferState = messageSender.TransferState;
	
	if (transferState.Processed.Count > 0) {
		var loadRef : VisualReference = transferState.Processed[0];
		var load : Visual = loadRef.Visual;
		doc.DestroyVisual(load);
		transferState.Processed.Clear();
		transferState.ReadyForIncoming = true;
	
		++sender.LoadsConsumed;
	}
}

[Optimize(true)]
function LC_OnInitialize( sender : Demo3D.Visuals.BoxVisual )
{
	var subscribedTo : Visual = sender.SubscribedTo;
	if (subscribedTo != null) {
		subscribedTo.RemoveMessageListener(sender);
		sender.SubscribedTo = null;
	}
	
	var parent : Visual = sender.Parent;
	
	if (parent != doc.Scene && parent != null) {
		if (parent.TransferStateEnabled) {
			parent.AddMessageListener(sender);
			sender.SubscribedTo   = parent;
			sender.PhysicsEnabled = false;
		}
		else {
			sender.BodyType       = PhysicsBodyType.Sensor;
			sender.PhysicsEnabled = true;
		}
		sender.PropertiesUpdated();
	}
}

[Optimize(false)]
function LC_OnBlocked( sender : Demo3D.Visuals.PhysicsObject, load : Demo3D.Visuals.Visual )
{
	var conv : Demo3D.Visuals.StraightRollerConveyor = sender.Parent;

	if (conv.TransferStateEnabled) { return; }
	
	Wait( sender.DelayBeforeDeletion.NextDouble() );
	doc.DestroyVisual(load);
	++sender.LoadsConsumed;
}
