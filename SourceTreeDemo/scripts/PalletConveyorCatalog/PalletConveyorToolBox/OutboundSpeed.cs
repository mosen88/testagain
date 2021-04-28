#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Demo3D.Visuals;
using Demo3D.Native;
#endregion

namespace PalletConveyorCatalog.PalletConveyorToolBox
{
    internal class OutboundSpeed
    {
        /// <summary>
        /// Checks if connected conveyor for outbound has lower speed and if so then current speed is adjusted
        /// </summary>
        /// <param name="sender"></param>
        internal static bool CheckOutboundSpeed(ConveyorVisual sender, Visual receiver)
        {            
            if (receiver != null)
            {
                //Check if connected visual is a conveyor and not a gravity conveyor
                if (receiver is ConveyorVisual && receiver.Type != "GravityConveyor")
                {
                    ConveyorVisual connectedConveyor = null;
                    //If connected conveyor is a transfer and chains to be used on transfer then check speed of chains
                    //else always check speed of the connected parent
                    if (receiver.Type.Contains("PalletTransfer") && sender is ChainConveyor)
                    {
                        //Get transfer chain
                        connectedConveyor = receiver.FindChild("ChainTransfer") as ConveyorVisual;
                    }
                    else
                    {
                        connectedConveyor = receiver as ConveyorVisual;
                    }

                    //Check if speed of connected conveyor is lower than this conveyor speed
                    if (sender.Motor.Speed.Value > connectedConveyor.Motor.Speed.Value)
                    {
                        sender.MotorSpeed = connectedConveyor.MotorSpeed;
                        return true;
                    }
                        
                }
            }

            return false;
        }
    }
}
