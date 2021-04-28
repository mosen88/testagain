#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Demo3D.Native;
using Demo3D.Visuals;
using Microsoft.DirectX;
using Demo3D.Utilities;
using PalletConveyorCatalog.PalletConveyorToolBox;
#endregion

namespace PalletConveyorCatalog
{
    [Auto]
    public class PalletTurntable
    {
        #region Properties
        /// <summary>
        /// Set to true if advanced properties should be shown
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set if advanced settings should be shown or not")] SimplePropertyValue<bool> ShowAdvancedProperties;

        /// <summary>
        /// Select if conveyor speed should be set to a standard value or select other if speed to be set manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of the conveyor. To set manually select other")] SimplePropertyValue<CustomEnumeration> ConveyorPerformance;
        /// <summary>
        /// Property to make it possible to manually set conveyor speed
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of conveyor if standard values not used")] SimplePropertyValue<SpeedProfile> ConveyorSpeed;

        /// <summary>
        /// Select time for rotating turntable 90 degrees. If value not as standrad value select other
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Select time for rotating turntable 90 degrees. If value not as standrad value select other")] SimplePropertyValue<CustomEnumeration> Turn90DegPerformance;
        /// <summary>
        /// Set time for rotating turntable 90 degrees manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set time for rotating turntable 90 degrees manually")] SimplePropertyValue<TimeProperty> Turn90DegDuration;
        /// <summary>
        /// If the turn table should return to Home position after task
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set to true if table should return to home position after every task.")] SimplePropertyValue<bool> AutoHome;

        /// <summary>
        /// Set width of conveyor
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set width of conveyor")] SimplePropertyValue<CustomEnumeration> ConveyorWidth;
        /// <summary>
        /// Set at which height the turntable should be located
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set at which height the turntable should be located")] SimplePropertyValue<DistanceProperty> TurnTableHeight;
        /// <summary>
        /// Set diameter of turntable (total length of turntable)
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set diameter of turntable (total length of turntable)")] SimplePropertyValue<CustomEnumeration> TurnTableDiameter;

        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;
        #endregion

        #region Declarations
        Visual Visual { get; set; }
        [Auto] PhotoEye Sensor;
        #endregion

        #region Constants
        /// <summary>
        /// For FlowDirection. Pallets is about to leave table
        /// </summary>
        public const string Outgoing = "Outgoing";

        /// <summary>
        /// For FlowDirection. Pallets is about to enter table
        /// </summary>
        public const string Incoming = "Incoming";

        #endregion

        #region Constructor
        public PalletTurntable(Visual sender)
        {
            // This will force the binding of all [Auto] members now instead of after the constructor completes
            sender.SetNativeObject(this);
            Visual = sender;

            // Set properties with custom enumeration
            if (ConveyorPerformance.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "0.2 m/s", "0.35 m/s", "Other" };
                ConveyorPerformance.Value = new CustomEnumeration(allowed, "0.35 m/s");
            }

            if (Turn90DegPerformance.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "4 s", "Other" };
                Turn90DegPerformance.Value = new CustomEnumeration(allowed, "4 s");
            }

            if (ConveyorWidth.Value.Value.Count() == 0)
            {
                if (sender.Type == "PalletRollerTurntable")
                {
                    ArrayList allowed = new ArrayList() { "900 mm", "1100 mm", "1300 mm" };
                    ConveyorWidth.Value = new CustomEnumeration(allowed, "900 mm");
                }
                else
                {
                    ArrayList allowed = new ArrayList() { "850 mm", "975 mm" };
                    ConveyorWidth.Value = new CustomEnumeration(allowed, "975 mm");
                }

            }

            if (TurnTableDiameter.Value.Value.Count() == 0)
            {
                if (sender.Type == "PalletRollerTurntable")
                {
                    ArrayList allowed = new ArrayList() { "1800 mm", "1925 mm", "2075 mm" };
                    TurnTableDiameter.Value = new CustomEnumeration(allowed, "1800 mm");
                }
                else
                {
                    ArrayList allowed = new ArrayList() { "1700 mm", "1760 mm", "1825 mm" };
                    TurnTableDiameter.Value = new CustomEnumeration(allowed, "1760 mm");
                }

            }

        }
        #endregion

        #region PropertyEvents
        [Auto]
        void OnShowAdvancedPropertiesUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            ShowOrHideAdvancedProperties(sender);
        }

        [Auto]
        void OnConveyorPerformanceUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //If not standard performance selected then show speed property for user to manually set speed
            if (newValue.Value == "Other")
            {
                ConveyorSpeed.Hidden = false;
                ConveyorSpeed.ReadOnly = false;
            }
            else
            {
                ConveyorSpeed.Hidden = true;
                ConveyorSpeed.ReadOnly = true;
                SetStandardConveyorPerformance(sender);
            }
        }

        [Auto]
        void OnConveyorSpeedUpdated(ConveyorVisual sender, SpeedProfile newValue, SpeedProfile oldValue)
        {
            //Set motor speed of conveyors
            SetConveyorSpeed(sender);
        }

        [Auto]
        void OnTurn90DegPerformanceUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //If not standard performance selected then show speed property for user to manually set speed
            if (newValue.Value == "Other")
            {
                Turn90DegDuration.Hidden = false;
                Turn90DegDuration.ReadOnly = false;
            }
            else
            {
                Turn90DegDuration.Hidden = true;
                Turn90DegDuration.ReadOnly = true;
                SetStandardRotationDuration(sender);
            }
        }

        [Auto]
        void OnTurn90DegDurationUpdated(ConveyorVisual sender, TimeProperty newValue, TimeProperty oldValue)
        {
            //Set motor speed of conveyors
            SetRotationDuration(sender);
        }

        [Auto]
        void OnTurnTableHeightUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            sender.ConveyorHeight = newValue;
        }

        [Auto]
        void OnConveyorWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check width of conveyor
            if (newValue.Value == "900 mm")
                sender.Width = 0.9;
            else if (newValue.Value == "1100 mm")
                sender.Width = 1.1;
            else if (newValue.Value == "1300 mm")
                sender.Width = 1.3;
            else if (newValue.Value == "975 mm")
                sender.Width = 0.975;
            else
                sender.Width = 0.85;

            //To make sensors update its width according to new width of conveyor it is temporarily moved to position -0.01 and the reconfigured  
            Sensor.Position = -0.01;
            LocateSensor(sender);
        }

        [Auto]
        void OnTurnTableDiameterUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check diameter of the turntable and set conveyor length (for rollers total length = diameter as the visual prepared for it)
            //For chain conveyor length is set according to PTC product specification
            double diameter;
            double conveyorLength;
            if (newValue.Value == "1800 mm")
            {
                diameter = 1.8;
                conveyorLength = 1.8;
            }
            else if (newValue.Value == "1925 mm")
            {
                diameter = 1.925;
                conveyorLength = 1.925;
            }
            else if (newValue.Value == "2075 mm")
            {
                diameter = 2.075;
                conveyorLength = 2.075;
            }
            else if (newValue.Value == "1700 mm")
            {
                diameter = 1.7;
                conveyorLength = 1.5;
            }
            else if (newValue.Value == "1760 mm")
            {
                diameter = 1.76;
                conveyorLength = 1.35;
            }
            else
            {
                diameter = 1.825;
                conveyorLength = 1.5;
            }

            //Configure dimensions of the turntable
            SetTurnTableDimensions(sender, diameter, conveyorLength);

            //Update location of sensor
            LocateSensor(sender);
        }

        [Auto]
        void OnMoved(ConveyorVisual sender, MatrixUpdateType type)
        {
            TurnTableHeight.Value = sender.ConveyorHeight;
        }
        #endregion

        #region SetReadyForIncoming
        bool _readyForIncoming;
        void SetReadyForIncoming(bool value)
        {
            _readyForIncoming = value;
            Visual.TransferState.ReadyForIncoming = value;
        }
        #endregion

        #region EventHandler
        [Auto]
        void OnReset(ConveyorVisual sender)
        {
            LocateSensor(sender);
        }

        [Auto]
        void OnInitialize(ConveyorVisual sender)
        {
            SetReadyForIncoming(true);
        }

        [Auto]
        IEnumerable OnRxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            if (transfer != null)
                yield return TurnTurnTableTo(transfer.Rx, transfer.RxConnector, Incoming);
        }

        [Auto]
        IEnumerable OnRxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            sender.IsMotorOn = true;
            while (Sensor.BlockingLoad != transfer.Load)
                yield return Wait.ForEvent(Sensor.OnBlocked);

            //Get time to wait before stopping motor
            yield return Wait.ForSeconds(WaitTimeToStop(sender, transfer.Load));

            sender.MotorOff();

            //Wait for pallet to completely stop
            yield return Wait.ForSeconds(StopTime(sender, 0));

        }

        [Auto]
        IEnumerable OnTxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            if (transfer != null)
                yield return TurnTurnTableTo(transfer.Tx, transfer.TxConnector, Outgoing);
        }

        [Auto]
        IEnumerable OnTxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            //Check if motor velocity should be decreased due to next conveyor has lower speed
            //If speed decreased then wait for pallet to reach new speed before turning motor on again
            if (OutboundSpeed.CheckOutboundSpeed(sender, transfer.Rx.Visual) && sender.Motor.CurrentSpeed > 0)
            {
                yield return Wait.ForSeconds(StopTime(sender, sender.MotorSpeed));
            }

            sender.MotorOn();
        }

        [Auto]
        void OnTxAfterTransfer(ConveyorVisual sender, Transfer transfer)
        {
            sender.MotorOff();

            //Reset motor speed if it has been changed for outbound
            SetConveyorSpeed(sender);
        }

        [Auto]
        IEnumerable OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            if (transfer == null)
                yield break;

            // Go to home position after task:
            if (AutoHome.Value)
            {
                //Make sure no other pallet transfers into this conveyor before it has moved back to home pos
                SetReadyForIncoming(false);
                document.Start(TurnHome);
            }
            else
            {
                SetReadyForIncoming(true);
                Transfer.UserDispatchIn(sender);
            }

        }

        IEnumerable TurnHome()
        {
            Connector start = Visual.FindConnector("Start");
            if (start == null)
                throw new InvalidOperationException("Could not find Connector Start of " + Visual);
            yield return TurnTurnTableTo(Visual, start, Incoming);
            SetReadyForIncoming(true);
            Transfer.UserDispatchIn(Visual);
        }
        #endregion

        #region StopTime
        /// <summary>
        /// Calculates time to wait before stopping conveyor to stop the pallet in center of conveyor
        /// </summary>
        /// <param name="conveyor"></param>
        /// <param name="load"></param>
        /// <returns></returns>
        TimeProperty WaitTimeToStop(ConveyorVisual conveyor, Visual load)
        {
            //Get time for pallet to be stopped from when motor is turned off
            double stopTime = StopTime(conveyor, 0);
            double stopingDistance = 0.5 * conveyor.Motor.Deceleration * Math.Pow(stopTime, 2);

            //Get length of pallet in conveyors direction
            double palletLength;
            if (conveyor.Type == "PalletRollerTurntable")
                palletLength = LoadDimensions.GetWidth(load, true);
            else
                palletLength = LoadDimensions.GetDepth(load, true);

            //Pallet should move half length minus stoping distance before stopping conveyor
            double waitTime = (palletLength / 2 - stopingDistance) / conveyor.Motor.Speed.Value;

            return waitTime;
        }

        /// <summary>
        /// Gets time from motor stopped until conveyor has reached given target speed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetSpeed"></param>
        /// <returns></returns>
        TimeProperty StopTime(ConveyorVisual conveyor, double targetSpeed)
        {
            // Get speed and deceleration of conveyor
            ExprDouble vMax = ConveyorSpeed.Value.MaxSpeed;
            double dec = conveyor.Motor.Deceleration;
            double stopTime = (vMax.Value - targetSpeed) / dec;

            return stopTime;
        }
        #endregion

        #region Turning
        IEnumerable TurnTurnTableTo(Visual sender, Connector con, string flowDirection)
        {
            // Set angle:
            double angle = 0;
            if (flowDirection == Incoming)
                angle = GetAngleOfConnector(con);
            else if (flowDirection == Outgoing)
                angle = 180 + GetAngleOfConnector(con);
            else
                throw new InvalidOperationException("FlowDirection is not incoming or outgoing");

            //If roller turntable cast to TurnTableConveyor
            //Else cast to ChainTurntacleConveyor
            if (sender.Type == "PalletRollerTurntable")
            {
                TurntableConveyor turnTable = CastVisualToTurnTableConveyor(sender);

                if (turnTable.Motor.Direction == MotorDirection.Reverse)
                    angle += 180;

                // Set target:
                Vector3 target = vector(0, angle, 0);
                turnTable.TargetPosition = angle;
                double diff = Math.Abs(turnTable.Position - turnTable.TargetPosition);
                turnTable.RotateToTarget();

                // Wait for move it the table has to turn:
                if (diff > 0.01)
                    yield return Wait.ForMove(turnTable);
            }
            else
            {
                ChainTurntableConveyor turnTable = CastVisualToChainTurnTableConveyor(sender);

                if (turnTable.Motor.Direction == MotorDirection.Reverse)
                    angle += 180;

                // Set target:
                Vector3 target = vector(0, angle, 0);
                turnTable.TargetPosition = angle;
                double diff = Math.Abs(turnTable.Position - turnTable.TargetPosition);
                turnTable.RotateToTarget();

                // Wait for move it the table has to turn:
                if (diff > 0.01)
                    yield return Wait.ForMove(turnTable);
            }

        }

        double GetAngleOfConnector(Connector connector)
        {
            string conName = connector.Name;

            switch (conName)
            {
                case "Start":
                    return 0;
                case "C15":
                    return 15;
                case "C30":
                    return 30;
                case "C45":
                    return 45;
                case "C60":
                    return 60;
                case "C75":
                    return 75;
                case "C90":
                    return 90;
                case "C105":
                    return 105;
                case "C120":
                    return 120;
                case "C135":
                    return 135;
                case "C150":
                    return 150;
                case "C165":
                    return 165;
                case "End":
                    return 180;
                case "C195":
                    return 195;
                case "C210":
                    return 210;
                case "C225":
                    return 225;
                case "C240":
                    return 240;
                case "C255":
                    return 255;
                case "C270":
                    return 270;
                case "C285":
                    return 285;
                case "C300":
                    return 300;
                case "C315":
                    return 315;
                case "C330":
                    return 330;
                case "C345":
                    return 345;
            }
            return 0;
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Shows or hids advanced properties
        /// </summary>
        /// <param name="sender"></param>
        private void ShowOrHideAdvancedProperties(Visual sender)
        {
            sender.HideAdvancedProperties = !ShowAdvancedProperties.Value;
        }

        /// <summary>
        /// Set conveyor speed to standard values
        /// </summary>
        /// <param name="sender"></param>
        private void SetStandardConveyorPerformance(ConveyorVisual sender)
        {
            if (ConveyorPerformance.Value.Value == "0.2 m/s")
            {
                ConveyorSpeed.Value.MaxSpeed = 0.2;
                ConveyorSpeed.Value.Acceleration = 0.4;
                ConveyorSpeed.Value.Deceleration = 0.4;
            }
            else if (ConveyorPerformance.Value.Value == "0.35 m/s")
            {
                ConveyorSpeed.Value.MaxSpeed = 0.35;
                ConveyorSpeed.Value.Acceleration = 0.4;
                ConveyorSpeed.Value.Deceleration = 0.4;
            }
        }

        /// <summary>
        /// Sets the actual speed property for the conveyor motors
        /// </summary>
        /// <param name="sender"></param>
        private void SetConveyorSpeed(ConveyorVisual sender)
        {
            sender.Motor.Speed = ConveyorSpeed.Value.MaxSpeed;
            sender.Motor.Acceleration = ConveyorSpeed.Value.Acceleration;
            sender.Motor.Deceleration = ConveyorSpeed.Value.Deceleration;
        }

        /// <summary>
        /// Set conveyor speed to standard values
        /// </summary>
        /// <param name="sender"></param>
        private void SetStandardRotationDuration(ConveyorVisual sender)
        {
            if (Turn90DegPerformance.Value.Value == "4 s")
            {
                Turn90DegDuration.Value = 4.0;
            }
        }

        /// <summary>
        /// Sets the actual speed property for the conveyor motors
        /// </summary>
        /// <param name="sender"></param>
        private void SetRotationDuration(ConveyorVisual sender)
        {
            if (sender.Type == "PalletRollerTurntable")
            {
                ((TurntableConveyor)sender).Rotate90Duration = Turn90DegDuration.Value;
            }
            else
            {
                ((ChainTurntableConveyor)sender).Rotate90Duration = Turn90DegDuration.Value;
            }

        }

        /// <summary>
        /// Set length of conveyor, diameter of surrounding ring and configure connectors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="diameter"></param>
        /// <param name="conveyorLength"></param>
        private void SetTurnTableDimensions(ConveyorVisual sender, double diameter, double conveyorLength)
        {
            //Set length of conveyor
            sender.Length = conveyorLength;

            //Get turn table ring
            CylinderVisual turnTableRing = sender.FindChild("TurnTableRing") as CylinderVisual;

            //Set dimensions and location of the ring
            turnTableRing.InnerDiameter = diameter;
            turnTableRing.Diameter = diameter + 0.02;

            turnTableRing.Location = vector(conveyorLength / 2, -0.12, 0);

            //Configure connectors one by one
            foreach (Connector connector in sender.AllConnectors)
            {
                //Get angle of connector
                double angle = GetAngleOfConnector(connector);

                //Calculate x and z location for connector
                double xLoc = diameter / 2 - Math.Cos((Math.PI / 180) * angle) * diameter / 2 - (diameter - conveyorLength) / 2;
                double zLoc = Math.Sin((Math.PI / 180) * angle) * diameter / 2;

                connector.AutoConfigure = false;

                connector.Start = vector(xLoc, 0, zLoc);
                connector.End = vector(xLoc, 0, zLoc);
            }

        }
        #endregion

        #region Aux
        TurntableConveyor CastVisualToTurnTableConveyor(Visual visual)
        {
            TurntableConveyor conv = (TurntableConveyor)visual;
            if (conv == null)
                throw new InvalidCastException("Could not case " + visual + " as TurntableConveyor");
            else
                return conv;
        }

        ChainTurntableConveyor CastVisualToChainTurnTableConveyor(Visual visual)
        {
            ChainTurntableConveyor conv = (ChainTurntableConveyor)visual;
            if (conv == null)
                throw new InvalidCastException("Could not case " + visual + " as ChainTurntableConveyor");
            else
                return conv;
        }

        void LocateSensor(ConveyorVisual sender)
        {
            // Locate the sensor in the center of the conveyor
            //Set position:
            Sensor.Position = sender.Length / 2;

            Sensor.PropertiesUpdated();
        }
        #endregion
    }
}