#region Namespaces
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Demo3D.Native;
using Demo3D.Utilities;
using Demo3D.Visuals;
using PalletConveyorCatalog.PalletConveyorToolBox;
#endregion

namespace PalletConveyorCatalog
{
    [Auto]
    public class Pallet_Transfer
    {
        #region Properties
        /// <summary>
        /// Set to true if advanced properties should be shown
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set if advanced settings should be shown or not")] SimplePropertyValue<bool> ShowAdvancedProperties;

        /// <summary>
        /// Select if roller conveyor speed should be set to a standard value or select other if speed to be set manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of the roller conveyor")] SimplePropertyValue<CustomEnumeration> RollerPerformance;
        /// <summary>
        /// Property to make it possible to manually set roller conveyor speed
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of roller conveyor if standard values not used")] SimplePropertyValue<SpeedProfile> RollerSpeed;
        /// <summary>
        /// Select if chain conveyor speed should be set to a standard value or select other if speed to be set manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of the chain conveyor")] SimplePropertyValue<CustomEnumeration> ChainPerformance;
        /// <summary>
        /// Property to make it possible to manually set chain conveyor speed
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of chain conveyor if standard values not used")] SimplePropertyValue<SpeedProfile> ChainSpeed;
        /// <summary>
        /// Set time to lift up chains
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Time for lifting or lowering chains")] SimplePropertyValue<TimeProperty> LiftingTimeChains;
        /// <summary>
        /// Set to true if RxTransfer should be confirmed at StartSensor to enhance performance of the system.
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set to true if RxTransfer should be confirmed at StartSensor to enhance performance of the system.")]
        SimplePropertyValue<bool> ConfirmRxAtStartSensor;
        /// <summary>
        /// Set position of start sensor used when confirming RxTransfer
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set position of start sensor used when confirming RxTransfer")] SimplePropertyValue<DistanceProperty> StartSensorPosition;


        /// <summary>
        /// Set width of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|RollerConveyor"), Description("Set width of roller conveyor")] SimplePropertyValue<CustomEnumeration> RollerConveyorWidth;
        /// <summary>
        /// Set length of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|RollerConveyor"), Description("Set length of roller conveyor")] SimplePropertyValue<DistanceProperty> RollerConveyorLength;
        /// <summary>
        /// Set height of transfer unit from ground floor
        /// </summary>
        [Auto, Category("_Configuration|RollerConveyor"), Description("Set height of conveyor from ground floor")] SimplePropertyValue<DistanceProperty> RollerConveyorHeight;

        /// <summary>
        /// Set offset of chains on right side if chains should stick out on side of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set offset of chains on right side of transfer unit (distance chains should stick out on right side")] SimplePropertyValue<DistanceProperty> ChainOffsetRight;
        /// <summary>
        /// Set offset of chains on left side if chains should stick out on side of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set offset of chains on left side of transfer unit (distance chains should stick out on left side")] SimplePropertyValue<DistanceProperty> ChainOffsetLeft;
        /// <summary>
        /// Select center if chains should be centered on roller conveyor and other if position to be set manually
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set location of lift chains relative RollerConveyor")] SimplePropertyValue<CustomEnumeration> ChainPositionMode;
        /// <summary>
        /// Set chains position on roller manually
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Manually set location of lift chains relative RollerConveyor")] SimplePropertyValue<DistanceProperty> ChainPosition;

        /// <summary>
        /// Select if chains should start in up or down position on init
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set if chains should be in up or down position at init")] SimplePropertyValue<CustomEnumeration> InitPositionChains;
        /// <summary>
        /// Set width of chain transfer conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set width of chain transfer conveyor")] SimplePropertyValue<CustomEnumeration> ChainWidth;

        /// <summary>
        /// Set to true if chains should move back to init position at end of each cycle
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set if chains should move back to init position after finished cycle")] SimplePropertyValue<bool> AutoHome;

        /// <summary>
        /// Set to true if sensors should be visible
        /// </summary>
        [Auto, Category("_Configuration|Sensors"), Description("Set if sensors should be visible or not")] SimplePropertyValue<bool> ShowSensors;

        /// <summary>
        /// Set direction of start connector
        /// </summary>
        [Auto, Category("_Configuration|Connectors"), Description("Set direction of start connector")] SimplePropertyValue<CustomEnumeration> DirectionStartConnector;
        /// <summary>
        /// Set direction of end connector
        /// </summary>
        [Auto, Category("_Configuration|Connectors"), Description("Set direction of end connector")] SimplePropertyValue<CustomEnumeration> DirectionEndConnector;
        /// <summary>
        /// Set direction of left connector
        /// </summary>
        [Auto, Category("_Configuration|Connectors"), Description("Set direction of left connector")] SimplePropertyValue<CustomEnumeration> DirectionLeftConnector;
        /// <summary>
        /// Set direction of right connector
        /// </summary>
        [Auto, Category("_Configuration|Connectors"), Description("Set direction of right connector")] SimplePropertyValue<CustomEnumeration> DirectionRightConnector;



        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;
        #endregion

        #region Declarations
        Visual Visual { get; set; }

        [Auto] BoxVisual RollerSensor;
        [Auto] BoxVisual ChainSensor;
        [Auto] PhotoEye StartSensor;
        [Auto] ChainConveyor ChainTransfer;

        /// <summary>
        /// Variable to keep track of where pallet arrived from
        /// </summary>
        bool _palletFromSide;

        /// <summary>
        /// Variable to keep track of transfers status
        /// </summary>
        bool _preparingForTxTransfer;
        /// <summary>
        /// Scriptreference triggered when chains have stopped moving
        /// </summary>
        ScriptReference _readyForTxTransfer;
        #endregion

        #region Constants
        const double LiftHeightChains = 0.025;
        #endregion

        #region Constructor
        public Pallet_Transfer(Visual sender)
        {
            // This will force the binding of all [Auto] members now instead of after the constructor completes
            sender.SetNativeObject(this);
            Visual = sender;

            // Set properties with custom enumeration
            if (RollerPerformance.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "0.2 m/s", "0.35 m/s", "Other" };
                RollerPerformance.Value = new CustomEnumeration(allowed, "0.35 m/s");
            }

            if (ChainPerformance.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "0.2 m/s", "0.35 m/s", "Other" };
                ChainPerformance.Value = new CustomEnumeration(allowed, "0.35 m/s");
            }

            if (RollerConveyorWidth.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "900 mm", "1100 mm", "1300 mm" };
                RollerConveyorWidth.Value = new CustomEnumeration(allowed, "900 mm");
            }

            if (ChainPositionMode.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "Center", "Other" };
                ChainPositionMode.Value = new CustomEnumeration(allowed, "Center");
            }

            if (InitPositionChains.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "Down", "Up" };
                InitPositionChains.Value = new CustomEnumeration(allowed, "Down");
            }

            if (ChainWidth.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "850 mm", "975 mm" };
                ChainWidth.Value = new CustomEnumeration(allowed, "975 mm");
            }

            if (DirectionStartConnector.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "InOut", "In", "Out", "None" };
                DirectionStartConnector.Value = new CustomEnumeration(allowed, "InOut");
            }

            if (DirectionEndConnector.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "InOut", "In", "Out", "None" };
                DirectionEndConnector.Value = new CustomEnumeration(allowed, "InOut");
            }

            if (DirectionLeftConnector.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "InOut", "In", "Out", "None" };
                DirectionLeftConnector.Value = new CustomEnumeration(allowed, "InOut");
            }

            if (DirectionRightConnector.Value.Value.Count() == 0)
            {
                ArrayList allowed = new ArrayList() { "InOut", "In", "Out", "None" };
                DirectionRightConnector.Value = new CustomEnumeration(allowed, "InOut");
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
        void OnRollerPerformanceUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //If not standard performance selected then show speed property for user to manually set speed
            if (newValue.Value == "Other")
            {
                RollerSpeed.Hidden = false;
                RollerSpeed.ReadOnly = false;
            }
            else
            {
                RollerSpeed.Hidden = true;
                RollerSpeed.ReadOnly = true;
                SetStandardConveyorPerformance(sender);
            }
        }

        [Auto]
        void OnRollerSpeedUpdated(ConveyorVisual sender, SpeedProfile newValue, SpeedProfile oldValue)
        {
            //Set motor speed of conveyors
            SetConveyorSpeed(sender);
        }

        [Auto]
        void OnChainPerformanceUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //If not standard performance selected then show speed property for user to manually set speed
            if (newValue.Value == "Other")
            {
                ChainSpeed.Hidden = false;
                ChainSpeed.ReadOnly = false;
            }
            else
            {
                ChainSpeed.Hidden = true;
                ChainSpeed.ReadOnly = true;
                SetStandardConveyorPerformance(ChainTransfer);
            }
        }

        [Auto]
        void OnChainSpeedUpdated(ConveyorVisual sender, SpeedProfile newValue, SpeedProfile oldValue)
        {
            //Set motor speed of conveyors
            SetConveyorSpeed(sender);
        }

        [Auto]
        void OnLiftingTimeChainsUpdated(ConveyorVisual sender, TimeProperty newValue, TimeProperty oldValue)
        {
            //Configure lift chains
            ConfigureChain(sender);
        }

        [Auto]
        void OnConfirmRxAtStartSensorUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            //If start sensor to be used to confirm RxTransfer then show StartSensorPosition property
            if (newValue)
            {
                StartSensorPosition.Hidden = false;
                StartSensorPosition.ReadOnly = false;
            }
            else
            {
                StartSensorPosition.Hidden = true;
                StartSensorPosition.ReadOnly = true;
            }

            //Check if start sensor should be activated or not
            SetSensors(sender);
        }

        [Auto]
        void OnStartSensorPositionUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            //Configure sensors based on the new position of the start sensor
            SetSensors(sender);
        }

        [Auto]
        void OnRollerConveyorWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check width of conveyor
            if (newValue.Value == "900 mm")
                sender.Width = 0.9;
            else if (newValue.Value == "1100 mm")
                sender.Width = 1.1;
            else
                sender.Width = 1.3;

            ConfigureEndConnectors(sender);
            ConfigureChain(sender);
            SetSensors(sender);
        }

        [Auto]
        void OnRollerConveyorLengthUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            //Set length of conveyor
            sender.Length = newValue;
            ConfigureEndConnectors(sender);
            //Check if chain position should be updated
            if (ChainPositionMode.Value.Value == "Center")
                ChainPosition.Value = sender.Length / 2;
        }

        [Auto]
        void OnChainWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check width of conveyor
            if (newValue.Value == "850 mm")
                ChainTransfer.Width = 0.85;
            else
                ChainTransfer.Width = 0.975;

            SetSensors(sender);
        }

        [Auto]
        void OnRollerConveyorHeightUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            //Set height of conveyor
            sender.ConveyorHeight = newValue;
        }

        [Auto]
        void OnChainOffsetRightUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            ConfigureChain(sender);
            ConfigureSideConnectors(sender);
        }

        [Auto]
        void OnChainOffsetLeftUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            ConfigureChain(sender);
            ConfigureSideConnectors(sender);
        }

        [Auto]
        void OnChainPositionModeUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Set position of chain
            if (newValue.Value == "Center")
            {
                ChainPosition.Hidden = true;
                ChainPosition.ReadOnly = true;
                ChainPosition.Value = sender.Length / 2;
            }
            else
            {
                ChainPosition.Hidden = false;
                ChainPosition.ReadOnly = false;
            }
        }

        [Auto]
        void OnChainPositionUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            ConfigureChain(sender);
            ConfigureSideConnectors(sender);
            SetSensors(sender);
        }

        [Auto]
        void OnInitPositionChainsUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureChain(sender);
        }

        [Auto]
        void OnShowSensorsUpdated(ConveyorVisual sender, bool newValue, bool oldValue)
        {
            if (newValue)
            {
                RollerSensor.Visible = true;
                ChainSensor.Visible = true;
            }
            else
            {
                RollerSensor.Visible = false;
                ChainSensor.Visible = false;
            }
        }

        [Auto]
        void OnDirectionStartConnectorUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureConnectorDirection("Start", newValue.Value);
        }

        [Auto]
        void OnDirectionEndConnectorUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureConnectorDirection("End", newValue.Value);
        }

        [Auto]
        void OnDirectionLeftConnectorUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureConnectorDirection("Left", newValue.Value);
        }

        [Auto]
        void OnDirectionRightConnectorUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureConnectorDirection("Right", newValue.Value);
        }

        [Auto]
        void OnConnected(ConveyorVisual sender, Connector connector, Connector info)
        {
            ShowOrHideRollerSides(sender);
        }

        [Auto]
        void OnDisconnected(ConveyorVisual sender, Connector connector, Connector info)
        {
            ShowOrHideRollerSides(sender);
        }

        [Auto]
        void OnMoved(ConveyorVisual sender, MatrixUpdateType type)
        {
            RollerConveyorHeight.Value = sender.ConveyorHeight;
        }

        [Auto]
        void OnAfterPropertyUpdated(ConveyorVisual sender, String name)
        {
            if (name == "Length")
            {
                ConfigureEndConnectors(sender);
                ConfigureChain(sender);
                ConfigureSideConnectors(sender);
                SetSensors(sender);
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

        #region EventHandler
        [Auto]
        void OnReset(ConveyorVisual sender)
        {
            // Reset motor to ensure deterministic behaviour:
            ResetMotor();

            //Set init location of chains
            ConfigureChain(sender);

            // Locate Sensors:
            SetSensors(sender);

        }

        [Auto]
        void OnInitialize(Visual sender)
        {
            // Set ready for incoming
            SetReadyForIncoming(true);

            _readyForTxTransfer = new ScriptReference();
            _preparingForTxTransfer = false;
        }

        [Auto]
        IEnumerable OnRxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Forbid other loads to enter conveyor:
            SetReadyForIncoming(false);

            // This should not be necessary:
            ChainTransfer.PhysicsEnabled = true;

            // Pallet arrives from side
            if (PalletArrivesFromSide(transfer))
            {
                _palletFromSide = true;
                SetChainMotorDirectionIncomingTransfer(transfer, ChainTransfer);
                yield return RaiseChain();
            }
            // Pallet arrives from straight:
            else
            {
                _palletFromSide = false;
                SetRollerMotorDirectionIncomingTransfer(transfer, sender);
                yield return LowerChain();
            }
        }

        [Auto]
        IEnumerable OnRxTransfer(Visual sender, Transfer transfer)
        {
            // Turn on motor of activated visual:
            ConveyorVisual activatedVisual = GetActiveVisualIncomingTransfer(transfer);

            activatedVisual.MotorOn();
            //If active visual is chain conveyor then let it simulate load
            if (activatedVisual == ChainTransfer)
                activatedVisual.SimulateLoad(transfer.Load);

            //If rx transfer not to be confirmed on start sensor then let pallet travel all the way to activated transfer sensor
            if (!ConfirmRxAtStartSensor || activatedVisual == ChainTransfer)
            {
                // Wait till pallet hits sensor:
                yield return WaitTillPalletHitsSensor(transfer);

                //Wait for pallet to move correct distance before stopping motors to let the pallet stop in center of transfer unit
                yield return WaitToStopMotor(activatedVisual, transfer.Load);

                SwitchOffAllMotors();
            }
            else
            {
                //Travel to start sensor and let pallet go into OnProcess so RXTransfer will be confirmed
                // Wait till the pallet hits the start sensor:
                while (StartSensor.BlockingLoad != transfer.Load)
                    yield return Wait.ForEvent(StartSensor.OnBlocked);
            }

        }

        /// <summary>
        /// If start sensor used to confirm RxTransfer then continue to move pallet to chain sensor
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="transfer"></param>
        /// <returns></returns>
        [Auto]
        IEnumerable OnProcess(ConveyorVisual sender, Transfer transfer)
        {
            ConveyorVisual activatedVisual = GetActiveVisualIncomingTransfer(transfer);

            //If RxTransfer already confirmed then let pallet move to transfer sensor here
            if (ConfirmRxAtStartSensor && activatedVisual == sender)
            {
                // Wait till pallet hits sensor:
                yield return WaitTillPalletHitsSensor(transfer);

                //Wait for pallet to move correct distance before stopping motors to let the pallet stop in center of transfer unit
                yield return WaitToStopMotor(activatedVisual, transfer.Load);

                SwitchOffAllMotors();
            }
        }

        /// <summary>
        /// Move chains when transfer created to let it do it before previous transfer is completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="transfer"></param>
        /// <returns></returns>
        [Auto]
        IEnumerable OnTransferCreated(ConveyorVisual sender, Transfer transfer)
        {
            //Stop tx transfer from running until transfer unit ready
            _preparingForTxTransfer = true;

            // Pallet leaves on side
            if (PalletLeavesToSide(transfer))
            {
                //If pallet arrived from straight then wait for pallet to stop completely
                if (!_palletFromSide)
                    yield return WaitForStopTime(sender, 0);

                SetChainMotorDirectionOutgoingTransfer(transfer, ChainTransfer);

                yield return RaiseChain(transfer.Load);
            }
            // Pallet leaves straight:
            else
            {
                //If pallet arrived from side then wait for pallet to stop completely
                if (_palletFromSide)
                    yield return WaitForStopTime(ChainTransfer, 0);

                SetRollerMotorDirectionOutgoingTransfer(transfer, sender);

                yield return LowerChain(transfer.Load);
            }

            //Trigger event that transfer unit ready for TxTransfer
            _preparingForTxTransfer = false;
            _readyForTxTransfer.RunNowParams();
        }

        /// <summary>
        /// Wait for chains to stop moving before continuing with the transfer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="transfer"></param>
        /// <returns></returns>
        [Auto]
        IEnumerable OnTxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            if (_preparingForTxTransfer)
                yield return Wait.ForEvent(_readyForTxTransfer);
        }

        [Auto]
        IEnumerable OnTxTransfer(Visual sender, Transfer transfer)
        {
            // Turn on motor of activated visual:
            ConveyorVisual activatedVisual = GetActiveVisualOutgoingTransfer(transfer);

            activatedVisual.SimulateLoad(transfer.Load);

            //Check if motor velocity should be decreased due to next conveyor has lower speed
            //If speed decreased then wait for pallet to reach new speed before turning motor on again
            if (OutboundSpeed.CheckOutboundSpeed(activatedVisual, transfer.Rx.Visual) && activatedVisual.Motor.CurrentSpeed > 0)
            {
                yield return WaitForStopTime(activatedVisual, activatedVisual.MotorSpeed);
            }

            activatedVisual.MotorOn();
        }

        [Auto]
        void OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            //Unstick load from this conveyor
            transfer.Load.Unstick();
            SwitchOffAllMotors();

            //Reset motor speed if it has been changed for outbound
            SetConveyorSpeed(sender);

            //Keep conveyor blocked until cycle is finished
            SetReadyForIncoming(false);
            document.Run(() => FinishTxTransfer(sender));
        }

        #endregion

        #region Dynamics
        /// <summary>
        /// Wait for motors to be completely stopped and move back to init pos if applicable
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private IEnumerable FinishTxTransfer(ConveyorVisual sender)
        {
            //Wait until motor is completely stopped
            yield return WaitForStopTime(sender, 0);

            //If autohome active then move chains back to init pos before letting new load in
            //Else set transfer ready for incoming
            if (AutoHome)
            {
                if (InitPositionChains.Value.Value == "Down")
                    yield return LowerChain();
                else
                    yield return RaiseChain();
            }

            // Conveyor can receive another load now:
            SetReadyForIncoming(true);
            Transfer.UserDispatchIn(sender);

        }

        /// <summary>
        /// Moves chain to roller level
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        private IEnumerable LowerChain(Visual loadToSimulate = null)
        {
            // Lower the lift if it is not already down
            if (ChainTransfer.LocationY != 0)
            {
                //Check if load to be simulated
                if (loadToSimulate != null)
                    ChainTransfer.SimulateLoad(loadToSimulate);
                //Move chains to 0 location
                ChainTransfer.MoveTo(Visual, vector(ChainTransfer.LocationX, 0, ChainTransfer.LocationZ), LiftHeightChains / LiftingTimeChains.Value);
                yield return Wait.ForMove(ChainTransfer);

            }
        }

        /// <summary>
        /// Raises chain to lift height of chains
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        private IEnumerable RaiseChain(Visual loadToSimulate = null)
        {
            // Raise the lift if it is not already up
            if (ChainTransfer.LocationY != LiftHeightChains)
            {
                //Check if load to be simulated
                if (loadToSimulate != null)
                    ChainTransfer.SimulateLoad(loadToSimulate);
                //Move chains to LiftHeight
                ChainTransfer.MoveTo(Visual, vector(ChainTransfer.LocationX, LiftHeightChains, ChainTransfer.LocationZ), LiftHeightChains / LiftingTimeChains.Value);
                yield return Wait.ForMove(ChainTransfer);

            }
        }

        /// <summary>
        /// Sets direction of roller conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="conv"></param>
        private void SetRollerMotorDirectionIncomingTransfer(Transfer transfer, ConveyorVisual conv)
        {
            if (transfer.RxConnectorName == "Start")
                conv.Motor.Direction = MotorDirection.Forwards;
            else if (transfer.RxConnectorName == "End")
                conv.Motor.Direction = MotorDirection.Reverse;
            else
                throw new InvalidOperationException("Name of Connector is: " + transfer.RxConnectorName + ". Should be Start or End because the motor of the roller conveyor is being changed here.");
        }

        /// <summary>
        /// Sets direction of roller conveyor for outgoing transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="conv"></param>
        private void SetRollerMotorDirectionOutgoingTransfer(Transfer transfer, ConveyorVisual conv)
        {
            if (transfer.TxConnectorName == "Start")
                conv.Motor.Direction = MotorDirection.Reverse;
            else if (transfer.TxConnectorName == "End")
                conv.Motor.Direction = MotorDirection.Forwards;
            else
                throw new InvalidOperationException("Name of Connector is: " + transfer.TxConnectorName + ". Should be Start or End because the motor of the roller conveyor is being changed here.");
        }

        /// <summary>
        /// Sets direction of chain conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="chain"></param>
        private void SetChainMotorDirectionIncomingTransfer(Transfer transfer, ChainConveyor chain)
        {
            if (transfer.RxConnectorName == "Left")
                chain.Motor.Direction = MotorDirection.Forwards;
            else if (transfer.RxConnectorName == "Right")
                chain.Motor.Direction = MotorDirection.Reverse;
            else
                throw new InvalidOperationException("Name of Connector is: " + transfer.RxConnectorName + ". Should be Right or Left because the motor of the chain is being changed here.");
        }

        /// <summary>
        /// Sets direction of chain conveyor for outgoing transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="chain"></param>
        private void SetChainMotorDirectionOutgoingTransfer(Transfer transfer, ChainConveyor chain)
        {
            if (transfer.TxConnectorName == "Left")
                chain.Motor.Direction = MotorDirection.Reverse;
            else if (transfer.TxConnectorName == "Right")
                chain.Motor.Direction = MotorDirection.Forwards;
            else
                throw new InvalidOperationException("Name of Connector is: " + transfer.TxConnectorName + ". Should be Right or Left because the motor of the chain is being changed here.");
        }
        #endregion

        #region WaitToStop
        /// <summary>
        /// Wait until pallet reaches active sensor
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private IEnumerable WaitTillPalletHitsSensor(Transfer transfer)
        {
            // Disbale all sensors:
            DisableAllSensors();

            // Get right sensor:
            string rxConName = transfer.RxConnectorName;
            BoxVisual activeSensor = GetActiveSensor(rxConName);
            activeSensor.PhysicsEnabled = true;

            // Wait till the pallet hits the sensor:
            while (activeSensor.BlockingLoad != transfer.Load)
                yield return Wait.ForEvent(activeSensor.OnBlocked);
        }

        /// <summary>
        /// Calculates time and waits for time needed before stopping the motors
        /// </summary>
        /// <param name="conveyor"></param>
        /// <param name="load"></param>
        /// <returns></returns>
        private IEnumerable WaitToStopMotor(ConveyorVisual conveyor, Visual load)
        {
            //Get time for pallet to be stopped from when motor is turned off
            ExprDouble vMax = conveyor.Motor.Speed;
            double dec = conveyor.Motor.Deceleration;
            double stopTime = vMax.Value / dec;
            double stopingDistance = 0.5 * dec * Math.Pow(stopTime, 2);

            //Get length of pallet in conveyors direction
            double palletLength;
            if (conveyor.Name == "ChainTransfer")
                palletLength = LoadDimensions.GetDepth(load, true);
            else
                palletLength = LoadDimensions.GetWidth(load, true);

            //Pallet should move half length minus stoping distance before stopping conveyor
            double waitTime = (palletLength / 2 - stopingDistance) / vMax.Value;

            yield return Wait.ForSeconds(waitTime);
        }

        /// <summary>
        /// Waiting time for pallet to reache target speed
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private IEnumerable WaitForStopTime(ConveyorVisual sender, double targetSpeed)
        {
            ExprDouble vMax;

            if (sender.Name == "ChainTransfer")
                vMax = ChainSpeed.Value.MaxSpeed;
            else
                vMax = RollerSpeed.Value.MaxSpeed;

            double dec = sender.Motor.Deceleration;
            double stopTime = (vMax.Value - targetSpeed) / dec;

            yield return Wait.ForSeconds(stopTime);
        }

        /// <summary>
        /// Waiting time for conveyor to get up to max speed from the moment the motor is switched on
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private IEnumerable WaitForAccTime(ConveyorVisual sender)
        {
            ExprDouble vMax = sender.Motor.Speed;
            double acc = sender.Motor.Acceleration;
            double accTime = vMax.Value / acc;

            yield return Wait.ForSeconds(accTime);
        }
        #endregion

        #region Configuration
        /// <summary>
        /// Set dimensions and location of sensors
        /// </summary>
        /// <param name="sender"></param>
        void SetSensors(ConveyorVisual sender)
        {
            //Set width of roller sensor
            RollerSensor.Width = sender.Width + 0.07;

            //Set width of chain sensor
            ChainSensor.Width = ChainTransfer.Width + 0.1;

            // Set position:
            RollerSensor.Location = vector(ChainPosition.Value, 0.075, 0);
            ChainSensor.Location = vector(ChainPosition.Value, LiftHeightChains + 0.075, 0);

            //If start sensor used for confirming rx transfer then activate it
            if (!ConfirmRxAtStartSensor)
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

        }

        /// <summary>
        /// Shows or hids advanced properties
        /// </summary>
        /// <param name="sender"></param>
        private void ShowOrHideAdvancedProperties(Visual sender)
        {
            sender.HideAdvancedProperties = !ShowAdvancedProperties.Value;
            ChainTransfer.HideAdvancedProperties = !ShowAdvancedProperties.Value;
            ChainSensor.HideAdvancedProperties = !ShowAdvancedProperties.Value;
            RollerSensor.HideAdvancedProperties = !ShowAdvancedProperties.Value;

        }

        /// <summary>
        /// Hiding or showing sides of roller conveyor depending if sides connected or not
        /// </summary>
        /// <param name="sender"></param>
        private void ShowOrHideRollerSides(ConveyorVisual sender)
        {
            //Get connectors
            Connector leftCon = sender.FindConnector("Left");
            Connector rightCon = sender.FindConnector("Right");

            //If side connected then hide side
            ((StraightRollerConveyor)sender).LeftSide.SideVisible = !leftCon.IsConnected;
            ((StraightRollerConveyor)sender).RightSide.SideVisible = !rightCon.IsConnected;

        }

        /// <summary>
        /// Set conveyor speed to standard values
        /// </summary>
        /// <param name="conveyor"></param>
        private void SetStandardConveyorPerformance(ConveyorVisual conveyor)
        {
            if (conveyor == ChainTransfer)
            {
                if (ChainPerformance.Value.Value == "0.2 m/s")
                {
                    ChainSpeed.Value.MaxSpeed = 0.2;
                    ChainSpeed.Value.Acceleration = 0.4;
                    ChainSpeed.Value.Deceleration = 0.4;
                }
                else if (ChainPerformance.Value.Value == "0.35 m/s")
                {
                    ChainSpeed.Value.MaxSpeed = 0.35;
                    ChainSpeed.Value.Acceleration = 0.4;
                    ChainSpeed.Value.Deceleration = 0.4;
                }
            }
            else
            {
                if (RollerPerformance.Value.Value == "0.2 m/s")
                {
                    RollerSpeed.Value.MaxSpeed = 0.2;
                    RollerSpeed.Value.Acceleration = 0.4;
                    RollerSpeed.Value.Deceleration = 0.4;
                }
                else if (RollerPerformance.Value.Value == "0.35 m/s")
                {
                    RollerSpeed.Value.MaxSpeed = 0.35;
                    RollerSpeed.Value.Acceleration = 0.4;
                    RollerSpeed.Value.Deceleration = 0.4;
                }

            }

        }

        /// <summary>
        /// Sets the actual speed property for the conveyor motors
        /// </summary>
        /// <param name="sender"></param>
        private void SetConveyorSpeed(ConveyorVisual sender)
        {
            sender.Motor.Speed = RollerSpeed.Value.MaxSpeed;
            sender.Motor.Acceleration = RollerSpeed.Value.Acceleration;
            sender.Motor.Deceleration = RollerSpeed.Value.Deceleration;

            ChainTransfer.Motor.Speed = ChainSpeed.Value.MaxSpeed;
            ChainTransfer.Motor.Acceleration = ChainSpeed.Value.Acceleration;
            ChainTransfer.Motor.Deceleration = ChainSpeed.Value.Deceleration;
        }

        /// <summary>
        /// Configure dimensions and location of chains
        /// </summary>
        /// <param name="sender"></param>
        private void ConfigureChain(ConveyorVisual sender)
        {
            if (ChainPositionMode.Value.Value == "Center")
                ChainPosition.Value = sender.Length / 2;

            ChainTransfer.Length = (sender.Width - 0.04) + (ChainOffsetLeft.Value + ChainOffsetRight.Value);

            double initialHeight = (InitPositionChains.Value.Value == "Down" ? 0 : LiftHeightChains);

            ChainTransfer.RotationYDegrees = 90;

            ChainTransfer.Location = vector(ChainPosition.Value, initialHeight, ChainOffsetLeft.Value + (sender.Width - 0.04) / 2);

        }

        /// <summary>
        /// Set direction of connector
        /// </summary>
        /// <param name="connectorName"></param>
        /// <param name="direction"></param>
        private void ConfigureConnectorDirection(string connectorName, string direction)
        {
            //Get connector
            Connector connector = Visual.FindConnector(connectorName);

            if (direction == "InOut")
                connector.TransferDirection = TransferDirection.InOut;
            else if (direction == "In")
                connector.TransferDirection = TransferDirection.In;
            else if (direction == "Out")
                connector.TransferDirection = TransferDirection.Out;
            else
                connector.TransferDirection = TransferDirection.None;
        }

        /// <summary>
        /// Configurate side connectors
        /// </summary>
        /// <param name="sender"></param>
        private void ConfigureSideConnectors(ConveyorVisual sender)
        {
            // Configure the side connectors depending on the chosen mode:
            // Declare variables:
            double xPos = ChainPosition.Value;
            double leftOffset = sender.Width / 2 + ChainOffsetLeft.Value;
            double rightOffset = sender.Width / 2 + ChainOffsetRight.Value;

            // Get the connectors:
            Connector left = sender.FindConnector("Left");
            Connector right = sender.FindConnector("Right");

            // Set the connectors:
            left.AutoConfigure = false;
            left.Start = vector(xPos, LiftHeightChains, leftOffset);
            left.End = vector(xPos, LiftHeightChains, leftOffset);
            left.Normal = vector(0, 0, 1);
            left.KeepInBounds = true;
            right.AutoConfigure = false;
            right.Start = vector(xPos, LiftHeightChains, -rightOffset);
            right.End = vector(xPos, LiftHeightChains, -rightOffset);
            right.Normal = vector(0, 0, -1);
            right.KeepInBounds = true;
        }

        /// <summary>
        /// Set location of start and end connectors
        /// </summary>
        /// <param name="sender"></param>
        private void ConfigureEndConnectors(ConveyorVisual sender)
        {
            // Get the connectors:
            Connector start = sender.FindConnector("Start");
            Connector end = sender.FindConnector("End");

            start.AutoConfigure = false;
            start.Start = vector(0, 0, 0);
            start.End = vector(0, 0, 0);
            start.Normal = vector(-1, 0, 0);
            start.KeepInBounds = true;
            end.AutoConfigure = false;
            end.Start = vector(sender.Length, 0, 0);
            end.End = vector(sender.Length, 0, 0);
            end.Normal = vector(1, 0, 0);
            end.KeepInBounds = true;
        }
        #endregion

        #region Aux
        /// <summary>
        /// Switch all conveyor motors off
        /// </summary>
        private void SwitchOffAllMotors()
        {
            // Turn off both motors:
            ConveyorVisual conv = CastVisualToConveyorVisual(Visual);

            conv.MotorOff();
            ChainTransfer.MotorOff();
        }

        /// <summary>
        /// Disable all sensors
        /// </summary>
        private void DisableAllSensors()
        {
            RollerSensor.PhysicsEnabled = false;
            ChainSensor.PhysicsEnabled = false;
        }

        /// <summary>
        /// Get active sensor to use for stopping load at center of chains
        /// </summary>
        /// <param name="rxConName"></param>
        /// <returns></returns>
        BoxVisual GetActiveSensor(string rxConName)
        {
            BoxVisual activeSensor = null;

            // Get the right sensor:
            switch (rxConName)
            {
                case "Start":
                    activeSensor = RollerSensor;
                    break;
                case "End":
                    activeSensor = RollerSensor;
                    break;
                case "Right":
                    activeSensor = ChainSensor;
                    break;
                case "Left":
                    activeSensor = ChainSensor;
                    break;
            }
            return activeSensor;
        }

        /// <summary>
        /// Returns the Visual that receive the load. Either Chain or conveyor.
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        ConveyorVisual GetActiveVisualIncomingTransfer(Transfer transfer)
        {
            // Return chain:
            if (PalletArrivesFromSide(transfer))
                return ChainTransfer;
            // Return roller conveyor:
            else
                return CastVisualToConveyorVisual(Visual);
        }

        /// <summary>
        /// Returns the Visual that delivers the load. Either Chain or conveyor.
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        ConveyorVisual GetActiveVisualOutgoingTransfer(Transfer transfer)
        {
            // Return chain:
            if (PalletLeavesToSide(transfer))
                return ChainTransfer;
            // Return roller conveyor:
            else
                return CastVisualToConveyorVisual(Visual);
        }

        /// <summary>
        /// Cast visual to conveyor visual
        /// </summary>
        /// <param name="visual"></param>
        /// <returns></returns>
        ConveyorVisual CastVisualToConveyorVisual(Visual visual)
        {
            ConveyorVisual conv = (ConveyorVisual)visual;
            if (conv == null)
                throw new InvalidCastException("Could not case " + visual + " as ConveyorVisual");
            else
                return conv;
        }

        /// <summary>
        /// Get active connector
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Connector GetConnectorByName(string name)
        {
            Connector con = Visual.FindConnector(name);
            if (con == null)
                throw new InvalidOperationException("Could not get connector " + name + " of " + Visual);
            else
                return con;
        }

        /// <summary>
        /// Check if pallet arrives from side connection
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private bool PalletArrivesFromSide(Transfer transfer)
        {
            string rxName = transfer.RxConnectorName;
            if (rxName == "Left" || rxName == "Right")
                return true;
            else if (rxName == "Start" || rxName == "End")
                return false;
            else
                throw new InvalidOperationException("RxConnectorName of " + transfer.Rx + " is " + rxName + "! Should be right, left, start or end!");
        }

        /// <summary>
        /// Check if pallet should leave via side connector
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private bool PalletLeavesToSide(Transfer transfer)
        {
            string txName = transfer.TxConnectorName;
            if (txName == "Left" || txName == "Right")
                return true;
            else if (txName == "Start" || txName == "End")
                return false;
            else
                throw new InvalidOperationException("TxConnectorName of " + transfer.Tx + " is " + txName + "! Should be right, left, start or end!");
        }



        /// <summary>
        /// Reset motors direction and turn them off
        /// </summary>
        void ResetMotor()
        {
            ChainTransfer.Motor.Direction = MotorDirection.Forwards;
            ChainTransfer.MotorOff();
            ((ConveyorVisual)Visual).Motor.Direction = MotorDirection.Forwards;
            ((ConveyorVisual)Visual).MotorOff();
        }

        #endregion

    }
}