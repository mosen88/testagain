#region Namespaces
using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using PalletConveyorCatalog.PalletConveyorToolBox;
#endregion

namespace PalletConveyorCatalog
{
    [Auto]
    public class PalletJackConveyor
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
        /// If MinStopDistance selected pallet will be stopped the distance before end of conveyor it takes for the conveyor to accelerate up to max speed. To set distance manually select other.
        /// </summary>
        [Auto, Category("_Configuration|StopDistance"), Description("If MinStopDistance selected pallet will be stopped the distance before end of conveyor it takes for the conveyor to accelerate up to max speed. To set distance manually select Other.")]
        SimplePropertyValue<CustomEnumeration> EndSensorOffset;
        /// <summary>
        /// Set distance from end of conveyor the pallet should be stopped.
        /// </summary>
        [Auto, Category("_Configuration|StopDistance"), Description("Set distance from end of conveyor the pallet should be stopped.")] SimplePropertyValue<DistanceProperty> StopDistanceFromEnd;
        /// <summary>
        /// Set width of conveyor
        /// </summary>
        [Auto, Category("Dimensions"), Description("Set width of conveyor")] SimplePropertyValue<CustomEnumeration> DimensionWidth;


        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;
        #endregion

        #region Declarations
        public Visual Visual { get; set; }

        [Auto] PhotoEye Sensor;
        #endregion

        #region Constants
        const string Inbound = "Inbound";
        const string Outbound = "Outbound";
        #endregion

        #region Constructor
        public PalletJackConveyor(Visual sender)
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

            if (DimensionWidth.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "900 mm", "1100 mm", "1300 mm" };
                DimensionWidth.Value = new CustomEnumeration(allowed, "900 mm");
            }

            if (EndSensorOffset.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "MinStopDistance", "Other" };
                EndSensorOffset.Value = new CustomEnumeration(allowed, "MinStopDistance");
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
            //Update end sensor position if applicable
            if (EndSensorOffset.Value.Value == "MinStopDistance")
                SetStandardSensorOffset(sender);
            ConfigureSensor(sender);
        }

        [Auto]
        void OnDimensionWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            SetDimensions(sender);
        }

        [Auto]
        void OnEndSensorOffsetUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //If min stop distance selected then set standard values and hide property for setting distance manually
            if (newValue.Value == "MinStopDistance")
            {
                StopDistanceFromEnd.Hidden = true;
                StopDistanceFromEnd.ReadOnly = true;
                SetStandardSensorOffset(sender);
            }
            else
            {
                //Show property for user to manually set stop distance
                StopDistanceFromEnd.Hidden = false;
                StopDistanceFromEnd.ReadOnly = false;
            }
        }

        [Auto]
        void OnStopDistanceFromEndUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            //Configure sensors based on the new stop distance
            ConfigureSensor(sender);
        }

        [Auto]
        void OnAfterPropertyUpdated(ConveyorVisual sender, String name)
        {
            if (name == "Length")
            {
                SetDimensions(sender);
            }
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

        #region Event handler
        [Auto]
        void OnReset(ConveyorVisual sender)
        {
            ConfigureSensor(sender);
        }

        [Auto]
        void OnInitialize(ConveyorVisual sender)
        {
            // Set ready for incoming
            SetReadyForIncoming(true);
        }

        [Auto]
        IEnumerable OnRxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Forbid other loads to enter conveyor:
            SetReadyForIncoming(false);

            // Turn on motor:
            sender.IsMotorOn = true;

            // Wait till load hits Sensor:
            while (Sensor.BlockingLoad.Visual != transfer.Load)
                yield return Wait.ForEvent(Sensor.OnBlocked);

            // Turn off motor
            sender.MotorOff();

        }


        [Auto]
        IEnumerable OnTxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            //Check if motor velocity should be decreased due to next conveyor has lower speed
            //If speed decreased then wait for pallet to reach new speed before turning motor on again
            if (OutboundSpeed.CheckOutboundSpeed(sender, transfer.Rx.Visual) && sender.Motor.CurrentSpeed > 0)
            {
                yield return Wait.ForSeconds(GetStopTime(sender, sender.MotorSpeed));
            }

            // Turn on motor:
            sender.MotorOn();
        }

        [Auto]
        void OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            sender.MotorOff();

            //Reset motor speed if it has been changed for outbound
            SetConveyorSpeed(sender);

            //Make conveyor ready to receive new pallet
            SetReadyForIncoming(true);
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
        /// Updates StopDistanceFromEnd property to standard values
        /// </summary>
        /// <param name="sender"></param>
        private void SetStandardSensorOffset(ConveyorVisual sender)
        {
            //Get distance for a pallet to stop
            double stopDistance = GetStopDistance(sender);
            //Set stop distance property
            StopDistanceFromEnd.Value = stopDistance;
        }

        /// <summary>
        /// Setting dimensions of conveyor
        /// </summary>
        /// <param name="sender"></param>
        private void SetDimensions(ConveyorVisual sender)
        {
            //Check width of conveyor
            if (DimensionWidth.Value.Value == "900 mm")
                sender.Width = 0.9;
            else if (DimensionWidth.Value.Value == "1100 mm")
                sender.Width = 1.1;
            else
                sender.Width = 1.3;

            //Get roller visuals
            ConveyorVisual leftRollers = sender.FindChild("LeftRollers") as ConveyorVisual;
            ConveyorVisual middleRollers = sender.FindChild("MiddleRollers") as ConveyorVisual;
            ConveyorVisual rightRollers = sender.FindChild("RightRollers") as ConveyorVisual;

            leftRollers.Length = sender.Length;
            middleRollers.Length = sender.Length;
            rightRollers.Length = sender.Length;

            //Set local height 1 mm below parent conveyor to avoid load creators to snap to any child conveyor

            leftRollers.Location = vector(0, -0.001, sender.Width / 2 - leftRollers.Width / 2);
            middleRollers.Location = vector(0, -0.001, 0);
            rightRollers.Location = vector(0, -0.001, -(sender.Width / 2 - leftRollers.Width / 2));

            //Move sensor temporarily to start of conveyor
            //A get around for sensor to properly update according to width of conveyor
            Sensor.Position = 0.01;

            ConfigureSensor(sender);
            ConfigureConnectors(sender);
        }

        /// <summary>
        /// Configure and locate sensor
        /// </summary>
        /// <param name="sender"></param>
        void ConfigureSensor(ConveyorVisual sender)
        {
            // Locate the sensor depending on the speed and acceleration of the conveyor
            double stopingDistance = GetStopDistance(sender);
            double sensorLoc = sender.Length - stopingDistance - StopDistanceFromEnd.Value;

            // Sanity check and warning:
            if (sensorLoc < 0.4)
                document.Warning("Please check if the length of " + sender + " is suffiecient to stop a pallet", sender);
            if (sensorLoc < 0)
                throw new InvalidOperationException("Please check the length, vMax, Deceleration and SensorOffset settings of " + sender + ". The sensor would have been" +
                    " located before the conveyor.");

            // Set position of end sensor:
            Sensor.Position = sensorLoc;
            Sensor.Overhang = 0.01;
            Sensor.PropertiesUpdated();
        }

        /// <summary>
        /// COnfigure and update location of start and end connector
        /// </summary>
        /// <param name="sender"></param>
        private void ConfigureConnectors(ConveyorVisual sender)
        {
            // Connectors
            Connector start = sender.FindConnector("Start");
            Connector end = sender.FindConnector("End");
            double length = sender.Length;

            // Set:
            start.AutoConfigure = false;
            start.Start = vector(0, 0, 0);
            start.End = vector(0, 0, 0);
            start.Normal = vector(-1, 0, 0);
            start.KeepInBounds = true;
            end.AutoConfigure = false;
            end.Start = vector(length, 0, 0);
            end.End = vector(length, 0, 0);
            end.Normal = vector(1, 0, 0);
            end.KeepInBounds = true;
        }
        #endregion

        #region Aux
        /// <summary>
        /// Gets distance pallet travels from stop of motor until pallet completely still
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private double GetStopDistance(ConveyorVisual sender)
        {
            double stopTime = GetStopTime(sender, 0);
            double stopingDistance = 0.5 * sender.Motor.Deceleration * Math.Pow(stopTime, 2);

            return stopingDistance;
        }

        /// <summary>
        /// Gets time from motor stopped until conveyor has reached given target speed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="targetSpeed"></param>
        /// <returns></returns>
        private double GetStopTime(ConveyorVisual sender, double targetSpeed)
        {
            // Get speed and deceleration of conveyor
            ExprDouble vMax = ConveyorSpeed.Value.MaxSpeed;
            double dec = sender.Motor.Deceleration;
            double stopTime = (vMax.Value - targetSpeed) / dec;

            return stopTime;
        }
        #endregion
    }
}
