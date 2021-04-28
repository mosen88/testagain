#region Namespaces
using System.Collections;
using System.Collections.Generic;
using Demo3D.Native;
using Demo3D.Visuals;
using Demo3D.Utilities;
using System;
using System.ComponentModel;
using System.Linq;
using PalletConveyorCatalog.PalletConveyorToolBox;
#endregion

namespace PalletConveyorCatalog
{
    [Auto]
    public class PalletConveyor
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
        /// Set to true if RxTransfer should be confirmed at StartSensor to enhance performance of the system. Only applicable when trains not created.
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set to true if RxTransfer should be confirmed at StartSensor to enhance performance of the system. Only applicable when trains not created.")]
        SimplePropertyValue<bool> ConfirmRxAtStartSensor;


        /// <summary>
        /// Set width of conveyor
        /// </summary>
        [Auto, Category("Dimensions"), Description("Set width of conveyor")] SimplePropertyValue<CustomEnumeration> DimensionWidth;

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
        /// Set to true if pallets should create train on conveyor
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Set to true if pallets should create train on conveyor")] SimplePropertyValue<bool> CreateTrain;
        /// <summary>
        /// Set number of pallets to wait for to create a train
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Set number of pallets to wait for to create a train")] SimplePropertyValue<int> TrainCapacity;
        /// <summary>
        /// Set max waiting time for conveyor to create a train. If time runs out the conveyor will release pallets even if train not completed.
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Set max waiting time for conveyor to create a train. If time runs out the conveyor will release pallets even if train not completed.")]
        SimplePropertyValue<TimeProperty> MaxWaitingTime;
        /// <summary>
        /// Set position of start sensor used when creating trains.
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Set position of start sensor used when creating trains")] SimplePropertyValue<DistanceProperty> StartSensorPosition;
        /// <summary>
        /// Information of distance between TU:s on this conveyor when creating trains
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Information of distance between TU:s on this conveyor when creating trains"), ReadOnly(true)] SimplePropertyValue<DistanceProperty> DistanceBetweenPallets;
        /// <summary>
        /// Set to true if distance between pallets in train should be reported to be able to fine tune StartSensorPosition
        /// </summary>
        [Auto, Category("_Configuration|TrainSettings"), Description("Set to true if distance between pallets in train should be reported to be able to fine tune StartSensorPosition")] SimplePropertyValue<bool> FindDistanceBetweenPallets;

        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;
        #endregion

        #region Declarations
        Visual Visual { get; set; }
        /// <summary>
        /// Start sensor used to create trains
        /// </summary>
        [Auto] PhotoEye StartSensor;
        /// <summary>
        /// End sensor used to stop pallets at end of conveyor
        /// </summary>
        [Auto] PhotoEye EndSensor;

        /// <summary>
        /// TimeProperty to keep track of when last pallet arrived when creating trains 
        /// </summary>
        private TimeProperty _startWaiting;
        /// <summary>
        /// TimeProperty to keep track of maximum time to wait for more pallets when creating trains
        /// </summary>
        private TimeProperty _endWaiting;
        /// <summary>
        /// Variable to keep track of current loads on conveyor
        /// </summary>
        private List<Visual> _currentLoads;
        /// <summary>
        /// Keep track of number of loads currently on this conveyor
        /// </summary>
        private int _currentNrLoads;
        /// <summary>
        /// ScriptReference to trigger event when new pallet arrives
        /// </summary>
        private ScriptReference _newPalletArrived;
        /// <summary>
        /// Variable to trigger force a train, that has not been completed, to be passed on
        /// </summary>
        private bool _forceIncompleteTrain;
        #endregion

        #region Constants
        /// <summary>
        /// Constant to set property of total number of loads in this loads train
        /// </summary>
        private const string Train = "Train";
        /// <summary>
        /// Constant to set property if train was completed or not
        /// </summary>
        private const string TrainComplete = "TrainComplete";
        /// <summary>
        /// Constant to set property for the index of a load within its train
        /// </summary>
        private const string TrainIndex = "TrainIndex";
        #endregion

        #region Constructor
        public PalletConveyor(Visual sender)
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
                if (sender.Type == "PalletRollerConveyor")
                {
                    ArrayList allowed = new ArrayList() { "900 mm", "1100 mm", "1300 mm" };
                    DimensionWidth.Value = new CustomEnumeration(allowed, "900 mm");
                }
                else
                {
                    ArrayList allowed = new ArrayList() { "850 mm", "975 mm" };
                    DimensionWidth.Value = new CustomEnumeration(allowed, "975 mm");

                }
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
            ConfigureSensors(sender);
        }

        [Auto]
        void OnConfirmRxAtStartSensorUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            //If start sensor to be used to confirm RxTransfer then show StartSensor and set correct category and description
            if (newValue)
            {
                StartSensorPosition.Category = "_Configuration|Performance";
                StartSensorPosition.Description = "Set position of start sensor used when confirming RxTransfer";
                StartSensorPosition.Hidden = false;
                StartSensorPosition.ReadOnly = false;
            }
            else
            {
                //If conveyor should create trains then keep StartSensorPosition property visible
                if (CreateTrain)
                    StartSensorPosition.Hidden = false;
                else
                    StartSensorPosition.Hidden = true;
            }

            //Configure sensors to activate or deactivate start sensor
            ConfigureSensors(sender);
        }

        [Auto]
        void OnDimensionWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check width of conveyor
            if (newValue.Value == "850 mm")
                sender.Width = 0.85;
            else if (newValue.Value == "975 mm")
                sender.Width = 0.975;
            else if (newValue.Value == "900 mm")
                sender.Width = 0.9;
            else if (newValue.Value == "1100 mm")
                sender.Width = 1.1;
            else
                sender.Width = 1.3;

            //To make sensors update its width according to new width of conveyor it is temporarily moved to position -0.01 and the reconfigured  
            EndSensor.Position = -0.01;
            StartSensor.Position = -0.01;

            ConfigureConnectors(sender);
            ConfigureSensors(sender);
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
            ConfigureSensors(sender);
        }

        [Auto]
        void OnCreateTrainUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            //Show or hide properties for creating train of pallets on conveyor
            ShowOrHideTrainProperties(sender);
            //Configure sensors to activate start sensor
            ConfigureSensors(sender);

            //If no trains to be created then set capacity to 1
            if (!newValue)
                TrainCapacity.Value = 1;
        }

        [Auto]
        void OnTrainCapacityUpdated(ConveyorVisual sender, int newValue, int oldValue)
        {
            //Capacity must be >= 1
            if (newValue < 1)
            {
                document.Warning("Train capacity has to >= 1. Capacity reset to old value.", sender);
                TrainCapacity.Value = oldValue;
            }
        }

        [Auto]
        void OnStartSensorPositionUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            //Configure sensors based on the new distance between pallets in train
            ConfigureSensors(sender);
        }

        [Auto]
        void OnFindDistanceBetweenPalletsUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            DistanceBetweenPallets.Hidden = !newValue;
        }

        [Auto]
        void OnAfterPropertyUpdated(ConveyorVisual sender, String name)
        {
            if (name == "Length")
            {
                ConfigureConnectors(sender);
                ConfigureSensors(sender);
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
        void OnInitialize(ConveyorVisual sender)
        {
            // Set ready for incoming
            SetReadyForIncoming(true);

            SetStartValues();
        }

        [Auto]
        IEnumerable OnRxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            Visual pallet = transfer.Load;

            //Increase number of loads and add this load to current loads list
            _currentNrLoads++;
            _currentLoads.Add(transfer.Load);
            //If this is last load in a train that has not been completed then set train to be forced forward
            _forceIncompleteTrain = ForceTrainToProceed(pallet);
            //Trigger event that new pallet has arrived
            _newPalletArrived.RunNowParams();
            // If there are enough pallets. Prohibit new pallets to enter:
            if (_currentNrLoads >= TrainCapacity.Value || !CreateTrain)
                SetReadyForIncoming(false);
            else
                SetReadyForIncoming(true);

            // Turn on motor:
            sender.IsMotorOn = true;

            // Wait till load hits Sensor:
            //If train to be created or RxTransfer to be confirmed on StartSensor then only travel to StartSensor in Rx transfer
            //Else go all the way to the end
            if (CreateTrain || ConfirmRxAtStartSensor)
            {
                while (StartSensor.BlockingLoad.Visual != transfer.Load)
                    yield return Wait.ForEvent(StartSensor.OnBlocked);
            }
            else
            {
                while (EndSensor.BlockingLoad.Visual != transfer.Load)
                    yield return Wait.ForEvent(EndSensor.OnBlocked);
            }

        }

        [Auto]
        IEnumerable OnRxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            //If create train active then check if train is complete
            if (CreateTrain)
            {
                Visual pallet = transfer.Load;

                // Set waiting time:
                _startWaiting = document.Time;
                _endWaiting.Value = document.Time + MaxWaitingTime.Value;

                // If train not forced to be passed on and full train not created yet then wait for other pallets
                if (!_forceIncompleteTrain && _currentNrLoads < TrainCapacity.Value)
                {
                    //Stop motor if conveyor not already receiving a new pallet
                    if (!sender.TransferState.IsReceiving)
                        sender.MotorOff();
                    yield return WaitForOtherPallets();
                }

            }
            else if (!ConfirmRxAtStartSensor)
            {
                //If start sensor not used to confirm RxTransfer the pallet have reached end sensor and Motor should be switched off
                sender.MotorOff();
            }

        }

        [Auto]
        IEnumerable OnProcess(ConveyorVisual sender, Transfer transfer)
        {
            //If train have been created or time of waiting for more pallets ran out  then move pallet to end sensor
            if (CreateTrain)
            {
                // After waiting the train is either complete or the time ran out. In both cases the pallets have to move on:
                //Block conveyor from receiving new pallets
                SetReadyForIncoming(false);

                //If this is first load in a train it should start the motor and set train properties to all current loads
                if (transfer.Load == _currentLoads[0])
                {
                    CreateSetTrain();
                    sender.MotorOn();
                }

                while (EndSensor.BlockingLoad != transfer.Load)
                    yield return Wait.ForEvent(EndSensor.OnBlocked);

                //Report distance between pallets if check is active
                if (FindDistanceBetweenPallets)
                    ReportDistanceBetweenPallets(sender);

                sender.MotorOff();
            }
            else if (ConfirmRxAtStartSensor)
            {
                //If rx transfer was confirmed with start sensor then continue to run the motor until pallet have reached end sensor
                while (EndSensor.BlockingLoad.Visual != transfer.Load)
                    yield return Wait.ForEvent(EndSensor.OnBlocked);

                sender.MotorOff();
            }

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

            //Remove the load from the current loads list already here as its train index should not be changed
            _currentLoads.Remove(transfer.Load);
            // Turn on motor:
            sender.MotorOn();
        }

        [Auto]
        void OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            //Continue to block conveyor until all loads have been transfered out
            SetReadyForIncoming(_readyForIncoming);
        }

        [Auto]
        void OnTxAfterTransfer(ConveyorVisual sender, Transfer transfer)
        {
            //If receiving conveyor do not create trains then remove train properties from pallet
            if (!transfer.Rx.Visual.GetCustomPropertyValue("CreateTrain", false))
            {
                transfer.Load.RemoveCustomProperty(Train);
            }

            _currentNrLoads--;

            // Set RFI to true if the last load on the conveyor leaves it:
            if (_currentNrLoads > 0)
                return;
            //Switch motor off
            sender.MotorOff();

            //Reset motor speed if it has been changed for outbound
            SetConveyorSpeed(sender);

            //Reset variable for forcing incomplete trains
            _forceIncompleteTrain = false;
            SetReadyForIncoming(true);
            Transfer.UserDispatchIn(sender);
        }
        #endregion

        #region CreateTrain
        /// <summary>
        /// Logic of waiting for other pallets to create trains.
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        IEnumerable WaitForOtherPallets()
        {
            // Wait till either there are enough pallets or the pallet waited long enough
            while (document.Time < _endWaiting && _currentNrLoads < TrainCapacity.Value && !_forceIncompleteTrain)
            {
                TimeProperty wait = _endWaiting - _startWaiting;
                if (wait <= 0.0001)
                    break;
                yield return Wait.ForAny
                    (
                        Wait.ForEvent(_newPalletArrived),
                        Wait.ForSeconds(wait)
                    );
            }
        }

        /// <summary>
        /// Set train property to current loads
        /// </summary>
        /// <param name="pallet"></param>
        private void CreateSetTrain()
        {
            bool isTrainCompleted = (_currentNrLoads == TrainCapacity.Value);

            for (int i = 0; i < _currentLoads.Count(); i++)
            {
                _currentLoads[i].AddSimpleProperty(Train, _currentNrLoads, "Size of the train this load belongs to.").Category = "_Information";
                _currentLoads[i].AddSimpleProperty(TrainComplete, isTrainCompleted, "Set if train was completed or not.").Category = "_Information";
                _currentLoads[i].AddSimpleProperty(TrainIndex, i + 1, "THis loads index within current train").Category = "_Information";
            }
        }

        /// <summary>
        /// Check if incompleted train should be forced to proceed
        /// </summary>
        /// <param name="pallet"></param>
        /// <returns></returns>
        private bool ForceTrainToProceed(Visual pallet)
        {
            //Check if pallet already part of a train
            if (pallet.HasCustomProperty(Train))
            {
                //If train has not been completed and this is last load in this train then force train to proceed
                if (!pallet.GetCustomPropertyValue<bool>(TrainComplete) &&
                    pallet.GetCustomPropertyValue<int>(TrainIndex) == pallet.GetCustomPropertyValue<int>(Train))
                {
                    return true;
                }
            }

            return false;
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
        /// Shows or hide properties for making trains of pallets
        /// </summary>
        /// <param name="sender"></param>
        private void ShowOrHideTrainProperties(Visual sender)
        {
            TrainCapacity.Hidden = !CreateTrain.Value;
            TrainCapacity.ReadOnly = !CreateTrain.Value;
            MaxWaitingTime.Hidden = !CreateTrain.Value;
            MaxWaitingTime.ReadOnly = !CreateTrain.Value;
            FindDistanceBetweenPallets.Hidden = !CreateTrain.Value;
            FindDistanceBetweenPallets.ReadOnly = !CreateTrain.Value;

            if (CreateTrain)
            {
                //Hide possibility to ConfirmRxOn transfer as this selection is not applicable when creating trains
                //When creating trains this function always used and not selectable
                ConfirmRxAtStartSensor.Hidden = true;
                ConfirmRxAtStartSensor.ReadOnly = true;
                ConfirmRxAtStartSensor.Value = false;
                StartSensorPosition.Category = "_Configuration|TrainSettings";
                StartSensorPosition.Description = "Set position of start sensor used when creating trains";
                StartSensorPosition.Hidden = false;
                StartSensorPosition.ReadOnly = false;
            }
            else
            {
                //Show possibility to ConfirmRxOn transfer again
                ConfirmRxAtStartSensor.Hidden = false;
                ConfirmRxAtStartSensor.ReadOnly = false;
                StartSensorPosition.Hidden = true;
                StartSensorPosition.ReadOnly = true;
            }

            if (CreateTrain && FindDistanceBetweenPallets)
                DistanceBetweenPallets.Hidden = false;
            else
                DistanceBetweenPallets.Hidden = true;
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

        /// <summary>
        /// Configure and locate sensors
        /// </summary>
        /// <param name="sender"></param>
        void ConfigureSensors(ConveyorVisual sender)
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
            EndSensor.Position = sensorLoc;
            EndSensor.Overhang = 0.01;
            EndSensor.PropertiesUpdated();
            //If train function not active then disable start sensor, put it at start of conveyor and hide it
            if (!CreateTrain && !ConfirmRxAtStartSensor)
            {
                StartSensor.PhysicsEnabled = false;
                StartSensor.Overhang = 0.01;
                StartSensor.Position = 0.01;
                StartSensor.Visible = false;
            }
            else
            {
                //If train function active or Rx should be confirmed on start sensor then enable sensor and position it
                StartSensor.PhysicsEnabled = true;
                StartSensor.Overhang = 0.01;
                StartSensor.Position = (double)StartSensorPosition.Value;
                StartSensor.Visible = true;
            }

            //Trigger update properties for sensors to update according to width of conveyor

            StartSensor.PropertiesUpdated();

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

        /// <summary>
        /// Report distance between pallets in train to use for fine tuning StartSensorPosition
        /// </summary>
        /// <param name="thisPallet"></param>
        private void ReportDistanceBetweenPallets(ConveyorVisual sender)
        {
            //If only one pallet on conveyor then don´t report
            if (_currentLoads.Count() <= 1)
                return;

            //Check length of pallets in convyeors direction
            double[] palletLengths = new double[2];
            if (sender.Type == "PalletRollerConveyor")
            {
                palletLengths[0] = LoadDimensions.GetWidth(_currentLoads[0], true);
                palletLengths[1] = LoadDimensions.GetWidth(_currentLoads[1], true);
            }
            else
            {
                palletLengths[0] = LoadDimensions.GetDepth(_currentLoads[0], true);
                palletLengths[1] = LoadDimensions.GetDepth(_currentLoads[1], true);
            }

            //Get location of pallets in conveyor direction
            double[] palletLocations = new double[2];

            palletLocations[0] = sender.TransformFromWorld(_currentLoads[0].WorldLocation).X;
            palletLocations[1] = sender.TransformFromWorld(_currentLoads[1].WorldLocation).X;

            //Report distance between pallets
            DistanceBetweenPallets.Value = (palletLocations[0] - palletLocations[1]) - (palletLengths[0] / 2 + palletLengths[1] / 2);
        }

        /// <summary>
        /// Initialize variables
        /// </summary>
        private void SetStartValues()
        {
            _startWaiting = 0;
            _endWaiting = 0;
            _newPalletArrived = new ScriptReference();
            _currentLoads = new List<Visual>();
            _currentNrLoads = 0;
            _forceIncompleteTrain = false;
            DistanceBetweenPallets.Value = 0;
        }

        #endregion
    }
}



