#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
#endregion

namespace PalletConveyorCatalog
{
    [Auto]
    public class PalletGravityConveyor
    {

        #region Properties
        /// <summary>
        /// Number of pallet position in total on the gravity conveyor
        /// </summary>
        [Auto, Category("_Configuration"), Description("Number of pallet position in total on the gravity conveyor")] SimplePropertyValue<int> NumberOfPositions;
        /// <summary>
        /// Length of each conveyor position on the gravity conveyor
        /// </summary>
        [Auto, Category("_Configuration"), Description("Length of each conveyor position on the gravity conveyor")] SimplePropertyValue<DistanceProperty> LengthPerPosition;
        /// <summary>
        /// Width of the gravity conveyor
        /// </summary>
        [Auto, Category("_Configuration"), Description("Width of the gravity conveyor")] SimplePropertyValue<DistanceProperty> ConveyorWidth;
        /// <summary>
        /// Speed of the gravity conveyors
        /// </summary>
        [Auto, Category("_Configuration"), Description("Speed of the gravity conveyors")] SimplePropertyValue<SpeedProfile> GravitySpeed;
        /// <summary>
        /// Set decline angle which the gravity convyeor should have. Property DeclineDistance will be updated when this property changed.
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set decline angle which the gravity convyeor should have. Property DeclineDistance will be updated when this property changed.")] SimplePropertyValue<AngleProperty> DeclineAngle;
        /// <summary>
        /// Set height drop from start to end of gravity conveyor. Property DeclineAngle will be updated when this property changed.
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set height drop from start to end of gravity conveyor. Property DeclineAngle will be updated when this property changed.")] SimplePropertyValue<DistanceProperty> DeclineDistance;
        /// <summary>
        /// Information of current number of loads on the gravity conveyor.
        /// </summary>
        [Auto, Category("_Information"), Description("Information of current number of loads on the gravity conveyor."), ReadOnly(true)] CustomPropertyValue<int> CurrentNrLoads;

        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;
        #endregion

        #region Declarations
        Visual Visual { get; set; }
        /// <summary>
        /// Variable to keep track of current loads on the conveyor
        /// </summary>
        List<Visual> currentLoads;
        /// <summary>
        /// Event to trigger when a load exit the gravity conveyor to release all other loads to move forward
        /// </summary>
        ScriptReference TxTransferDone;
        #endregion

        #region Constructor
        public PalletGravityConveyor(Visual sender)
        {
            sender.SetNativeObject(this);
            Visual = sender;
        }
        #endregion

        #region PropertyEvents
        [Auto]
        void OnNumberOfPositionsUpdated(Visual sender, int newVal, int oldVal)
        {
            if (newVal < 1)
            {
                document.Warning("There have to be at least 1 position", sender);
                NumberOfPositions.Value = oldVal;
            }
            else
                ConfigureConveyor(sender);
        }

        [Auto]
        void OnLengthPerPositionUpdated(Visual sender, DistanceProperty newVal, DistanceProperty oldVal)
        {
            if (newVal < 0.5)
            {
                document.Warning("Length have to be greater than 0.5 m", sender);
                LengthPerPosition.Value = oldVal;
            }
            else
                ConfigureConveyor(sender);
        }

        [Auto]
        void OnConveyorWidthUpdated(Visual sender, DistanceProperty newVal, DistanceProperty oldVal)
        {
            if (newVal < 0.5)
            {
                document.Warning("Width have to be greater than 0.5 m", sender);
                ConveyorWidth.Value = oldVal;
            }
            else
                ConfigureConveyor(sender);
        }

        [Auto]
        void OnGravitySpeedUpdated(Visual sender, SpeedProfile newVal, SpeedProfile oldVal)
        {
            if (newVal.MaxSpeed < 0.01)
            {
                document.Warning("Speed have to be greater than 0.01", sender);
                GravitySpeed.Value = oldVal;
            }
            else
                ConfigureConveyor(sender);

            double angle = (180 / Math.PI) * Math.Asin(DeclineDistance.Value / (LengthPerPosition.Value * NumberOfPositions.Value));
            double declineDistance = (LengthPerPosition.Value * NumberOfPositions.Value) * Math.Sin((Math.PI / 180) * DeclineAngle.Value);
        }

        [Auto]
        void OnDeclineAngleUpdated(Visual sender, AngleProperty newVal, AngleProperty oldVal)
        {
            if (newVal > 45 || newVal < -45)
            {
                document.Warning("Decline angle to big, value reset to old value", sender);
                DeclineAngle.Value = oldVal;
            }
            else if (newVal < 0)
            {
                document.Warning("Angle should be set to a positive value", sender);
                DeclineAngle.Value = Math.Abs(newVal);
            }

            //Update DeclineDistance property
            DeclineDistance.Value = (LengthPerPosition.Value * NumberOfPositions.Value) * Math.Sin((Math.PI / 180) * DeclineAngle.Value);

            //Configure conveyor angle
            ConfigureDecline(sender);
        }

        [Auto]
        void OnDeclineDistanceUpdated(Visual sender, DistanceProperty newVal, DistanceProperty oldVal)
        {
            //Calculate angle of the conveyors with given DeclineDistance
            double angle = (180 / Math.PI) * Math.Asin(Math.Abs(DeclineDistance.Value) / (LengthPerPosition.Value * NumberOfPositions.Value));

            //Check if angle is okay
            if (angle > 45 || angle < -45)
            {
                document.Warning("Decline distance to big, value reset to old value", sender);
                DeclineDistance.Value = oldVal;
            }
            else
            {
                if (newVal < 0)
                {
                    document.Warning("Decline distance should be set to a positive value", sender);
                    DeclineDistance.Value = Math.Abs(newVal);
                }

                //Set decline angle property to trigger configuration of the conveyor
                DeclineAngle.Value = angle;
            }
        }

        [Auto]
        void OnConnected(Visual sender, Connector connector, Connector info)
        {
            //Make sure that decline angle is kept when conveyor is connected to other conveyor elements
            ConfigureDecline(sender);
        }

        #endregion

        #region EventHandlers
        [Auto]
        void OnInitialize(Visual sender)
        {
            CurrentNrLoads.Value = 0;
            currentLoads = new List<Visual>();
            TxTransferDone = new ScriptReference();
        }

        [Auto]
        void OnDispatchIn(Visual sender)
        {
            if (sender.TransferState.ReadyForIncoming && currentLoads.Count() < NumberOfPositions.Value)
            {
                //Select the first incoming transfer and run it:
                Transfer transfer = sender.TransferState.FirstIncomingWaitingToStart;
                if (transfer == null)
                    return;

                currentLoads.Add(transfer.Load);

                // Forbid other loads to enter conveyor:
                sender.TransferState.ReadyForIncoming = false;

                // Run transfer:
                transfer.Run();
            }

            // C# should handle DispatchIn:
            Demo3D.Script.ScriptThread.CurrentEventHandled = true;
        }

        [Auto]
        IEnumerable OnRxTransfer(Visual sender, Transfer transfer)
        {
            ConveyorVisual firstConveyor = sender.FindChild("ConveyorPart1") as ConveyorVisual;
            PhotoEye firstSensor = firstConveyor.FindChild("Sensor") as PhotoEye;

            firstConveyor.MotorOn();

            // Wait till load hits Sensor:
            while (firstSensor.BlockingLoad.Visual != transfer.Load)
                yield return Wait.ForEvent(firstSensor.OnBlocked);

            // Turn off motor
            firstConveyor.MotorOff();



        }


        [Auto]
        IEnumerable OnProcess(Visual sender, Transfer transfer)
        {
            ConveyorVisual firstConveyor = sender.FindChild("ConveyorPart1") as ConveyorVisual;

            while (NumberOfPositions.Value - currentLoads.IndexOf(transfer.Load) == 1)
            {
                yield return Wait.ForEvent(TxTransferDone);
            }

            document.Run(() => WaitToSetReadyForIncoming(sender));

            firstConveyor.MotorOn();

            for (int i = 2; i <= NumberOfPositions.Value; i++)
            {
                ConveyorVisual currentConv = sender.FindChild("ConveyorPart" + i) as ConveyorVisual;
                PhotoEye currentSensor = currentConv.FindChild("Sensor") as PhotoEye;

                currentConv.MotorOn();

                // Wait till load hits Sensor:
                while (currentSensor.BlockingLoad.Visual != transfer.Load)
                    yield return Wait.ForEvent(currentSensor.OnBlocked);

                // Turn off motor
                currentConv.MotorOff();


                if (i < NumberOfPositions.Value)
                {
                    while (NumberOfPositions.Value - currentLoads.IndexOf(transfer.Load) == i)
                    {
                        yield return Wait.ForEvent(TxTransferDone);
                    }

                    currentConv.MotorOn();
                }

            }
        }

        /// <summary>
        /// Wiat until sensor of first conveyor element is cleared before unblocking conveyor for new pallets
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private IEnumerable WaitToSetReadyForIncoming(Visual sender)
        {
            //Get first conveyor element and sensor of it
            ConveyorVisual firstConveyor = sender.FindChild("ConveyorPart1") as ConveyorVisual;
            PhotoEye firstSensor = firstConveyor.FindChild("Sensor") as PhotoEye;

            while (firstSensor.IsBlocked)
                yield return Wait.ForEvent(firstSensor.OnCleared);

            //Set conveyor ready for incoming
            sender.TransferState.ReadyForIncoming = true;
            Transfer.UserDispatchIn(sender);

        }

        [Auto]
        void OnDispatchOut(Visual sender)
        {
            // Create transfer:
            Transfer.Create(sender, "End", currentLoads[0]);

            // C# should handle DispatchOut:
            Demo3D.Script.ScriptThread.CurrentEventHandled = true;
        }

        [Auto]
        void OnTxTransfer(Visual sender, Transfer transfer)
        {

            // Turn on motor:
            currentLoads.Remove(transfer.Load);
            ConveyorVisual endConveyor = sender.FindChild("ConveyorPart" + NumberOfPositions.Value) as ConveyorVisual;
            endConveyor.MotorOn();

            //Trigger event for all pallets to move when first pallet in line have started to be transferred out
            TxTransferDone.RunNowParams();
        }

        #endregion

        #region Configuration
        void ConfigureConveyor(Visual sender)
        {
            foreach (Visual child in sender.Children)
            {
                if (child.Name != "ConveyorPart1") child.Delete();
            }

            ConveyorVisual conveyor1 = sender.FindChild("ConveyorPart1") as ConveyorVisual;
            conveyor1.Width = ConveyorWidth.Value;
            conveyor1.Length = LengthPerPosition.Value;
            conveyor1.Motor.Speed = GravitySpeed.Value.MaxSpeed;
            conveyor1.Motor.Acceleration = GravitySpeed.Value.Acceleration;
            conveyor1.Motor.Deceleration = GravitySpeed.Value.Deceleration;

            LocateSensor(conveyor1);

            conveyor1.PropertiesUpdated();

            ConveyorVisual lastConveyor = conveyor1;


            for (int i = 2; i <= NumberOfPositions; i++)
            {
                ConveyorVisual conveyor = lastConveyor;

                conveyor = conveyor1.Clone() as ConveyorVisual;
                conveyor.Reparent(sender);
                conveyor.Name = "ConveyorPart" + i;

                conveyor.SelectParentWhenPicked = true;

                Connector lastEndConnector = lastConveyor.FindConnector("End");
                Connector thisStartConnector = conveyor.FindConnector("Start");

                conveyor.WorldLocation = lastEndConnector.WorldCenter;

                thisStartConnector.ConnectTo(lastEndConnector, true);

                conveyor.PropertiesUpdated();

                lastConveyor = conveyor;
            }

            Connector endConnector = sender.FindConnector("End");

            endConnector.Start = vector(LengthPerPosition.Value * NumberOfPositions.Value, 0, 0);
            endConnector.End = vector(LengthPerPosition.Value * NumberOfPositions.Value, 0, 0);
            endConnector.Normal = vector(1, 0, 0);

            //Update DeclineDistance property
            DeclineDistance.Value = (LengthPerPosition.Value * NumberOfPositions.Value) * Math.Sin((Math.PI / 180) * DeclineAngle.Value);

        }

        private void ConfigureDecline(Visual sender)
        {
            sender.RotationZDegrees = -DeclineAngle.Value;
        }


        #endregion

        #region Aux
        void LocateSensor(ConveyorVisual conv)
        {
            PhotoEye Sensor = conv.FindChild("Sensor") as PhotoEye;

            // Locate the sensor depending on the speed and acceleration of the conveyor
            ExprDouble vMax = conv.Motor.Speed;
            double dec = conv.Motor.Deceleration;
            double stopTime = vMax.Value / dec;
            double stopingDistance = 0.5 * dec * Math.Pow(stopTime, 2);
            double sensorLoc = conv.Length - stopingDistance;


            // Sanity check and warning:
            if (sensorLoc < 0.4)
                document.Warning("Please check if the length of " + conv + " is suffiecient to stop a pallet", conv);
            if (sensorLoc < 0)
                throw new InvalidOperationException("Please check the length, sensor offset, vMax and Deceleration settings of " + conv);

            // Set position:
            Sensor.Position = sensorLoc - 0.01;
        }
        #endregion
    }
}