#region Using Statements
using System;
using System.Collections;
using Demo3D.Visuals;
using Demo3D.Common;
using Demo3D.KeyFrame;
using Demo3D.Utilities;
using Microsoft.DirectX;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Demo3D.Script;
using Demo3D.Script.CustomAttribute;
using Demo3D.EventQueue;

using Units = Demo3D.Utilities.Units;
using BuiltInCasts = Demo3D.Scripting.BuiltinFunctions;

#endregion

namespace Demo3D.NativeScripts {
	public sealed class LoadConsumer_Script : Demo3D.Script.NativeScript {
		[Optimize(true)]public void LC_OnReset(Demo3D.Visuals.BoxVisual sender)
		{
			__SetCustomProperty(sender, "LoadsConsumed", 0);
		}
		
		[Optimize(true)]public void LC_OnProcess2(Demo3D.Visuals.Visual sender, Demo3D.Visuals.Visual messageSender)
		{
			Demo3D.Visuals.TransferState transferState = messageSender.Props.TransferState;
			if ((transferState.Processed.Count > 0)) {
				Demo3D.Visuals.VisualReference loadRef = transferState.Processed[0];
				Demo3D.Visuals.Visual load = loadRef.Visual;
				doc.DestroyVisual(load);
				transferState.Processed.Clear();
				transferState.ReadyForIncoming = true;
				__SetCustomProperty(sender, "LoadsConsumed", Demo3D.Script.BinaryNode.Add(__GetCustomProperty(sender, "LoadsConsumed", null), 1));
			}
		}
		
		[Optimize(true)]public void LC_OnInitialize(Demo3D.Visuals.BoxVisual sender)
		{
			Demo3D.Visuals.Visual subscribedTo = ((Demo3D.Visuals.Visual)__GetCustomProperty(sender, "SubscribedTo", typeof(Demo3D.Visuals.Visual)));
			if ((subscribedTo != null)) {
				subscribedTo.RemoveMessageListener(sender);
				__SetCustomProperty(sender, "SubscribedTo", null);
			}
			Demo3D.Visuals.Visual parent = sender.Props.Parent;
			if (((parent != doc.Scene) && (parent != null))) {
				if (parent.Props.TransferStateEnabled) {
					parent.AddMessageListener(sender);
					__SetCustomProperty(sender, "SubscribedTo", parent);
					sender.Props.PhysicsEnabled = false;
				}
				else {
					sender.Props.BodyType = PhysicsBodyType.Sensor;
					sender.Props.PhysicsEnabled = true;
				}
				sender.PropertiesUpdated();
			}
		}
		
		#region Generated Code
		public LoadConsumer_Script(Demo3D.Visuals.ScriptContainer script, Demo3D.Visuals.Document doc) : base(script, doc) {
		}
		#endregion
	}
}
