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
    //PalletConveyorCatalog.PalletTransferTurnTable 
    public class PalletTransferTurnTable
    {
        #region Properties
        /// <summary>
        /// Set to true if advanced properties should be shown
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set if advanced settings should be shown or not")]
        SimplePropertyValue<bool> ShowAdvancedProperties;

        [Auto, Category("_Configuration"), Description("")]
        SimplePropertyValue<bool> PrintStatus;

        /// <summary>
        /// Select if conveyor speed should be set to a standard value or select other if speed to be set manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of the conveyor. To set manually select other")]
        SimplePropertyValue<CustomEnumeration> ConveyorPerformance;
        /// <summary>
        /// Property to make it possible to manually set conveyor speed
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of conveyor if standard values not used")]
        SimplePropertyValue<SpeedProfile> ConveyorSpeed;

        /// <summary>
        /// Select if chain conveyor speed should be set to a standard value or select other if speed to be set manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of the chain conveyor")]
        SimplePropertyValue<CustomEnumeration> ChainPerformance;
        /// <summary>
        /// Property to make it possible to manually set chain conveyor speed
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set performance speed of chain conveyor if standard values not used")]
        SimplePropertyValue<SpeedProfile> ChainSpeed;
        /// <summary>
        /// Set time to lift up chains
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Time for lifting or lowering chains")]
        SimplePropertyValue<TimeProperty> LiftingTimeChains;

        /// <summary>
        /// Set offset of chains on right side if chains should stick out on side of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set offset of chains on right side of transfer unit (distance chains should stick out on right side")]
        SimplePropertyValue<DistanceProperty> ChainOffsetRight;
        /// <summary>
        /// Set offset of chains on left side if chains should stick out on side of roller conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set offset of chains on left side of transfer unit (distance chains should stick out on left side")]
        SimplePropertyValue<DistanceProperty> ChainOffsetLeft;
        /// <summary>
        /// Select center if chains should be centered on roller conveyor and other if position to be set manually
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set location of lift chains relative RollerConveyor")]
        SimplePropertyValue<CustomEnumeration> ChainPositionMode;
        /// <summary>
        /// Set chains position on roller manually
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Manually set location of lift chains relative RollerConveyor")]
        SimplePropertyValue<DistanceProperty> ChainPosition;

        /// <summary>
        /// Select if chains should start in up or down position on init
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set if chains should be in up or down position at init")]
        SimplePropertyValue<CustomEnumeration> InitPositionChains;
        /// <summary>
        /// Set width of chain transfer conveyor
        /// </summary>
        [Auto, Category("_Configuration|LiftChains"), Description("Set width of chain transfer conveyor")] SimplePropertyValue<CustomEnumeration> ChainWidth;

        /// <summary>
        /// Select time for rotating turntable 90 degrees. If value not as standrad value select other
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Select time for rotating turntable 90 degrees. If value not as standrad value select other")]
        SimplePropertyValue<CustomEnumeration> Turn90DegPerformance;
        /// <summary>
        /// Set time for rotating turntable 90 degrees manually
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set time for rotating turntable 90 degrees manually")]
        SimplePropertyValue<TimeProperty> Turn90DegDuration;
        /// <summary>
        /// If the turn table should return to Home position after task
        /// </summary>
        [Auto, Category("_Configuration|Performance"), Description("Set to true if table should return to home position after every task.")]
        SimplePropertyValue<bool> AutoHome;

        /// <summary>
        /// Set width of conveyor
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set width of conveyor")]
        SimplePropertyValue<CustomEnumeration> ConveyorWidth;
        /// <summary>
        /// Set at which height the turntable should be located
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set at which height the turntable should be located")]
        SimplePropertyValue<DistanceProperty> TurnTableHeight;
        /// <summary>
        /// Set diameter of turntable (total length of turntable)
        /// </summary>
        [Auto, Category("_Configuration|Dimensions"), Description("Set diameter of turntable (total length of turntable)")]
        SimplePropertyValue<CustomEnumeration> TurnTableDiameter;


        #endregion

        #region Declarations
        Visual Visual { get; set; }
        [Auto] ChainConveyor ChainTransfer;
        [Auto] BoxVisual RollerSensor;
        [Auto] BoxVisual ChainSensor;
        private bool _palletArrivedViaChain;
        [Auto] IBuilder app;
        [Auto] Document document;
        [Auto] PrintDelegate print;
        [Auto] VectorDelegate vector;

        #region SetReadyForIncoming
        bool _readyForIncoming;
        void SetReadyForIncoming(bool value)
        {
            _readyForIncoming = value;
            Visual.TransferState.ReadyForIncoming = value;
        }
        #endregion
        #endregion

        #region Constants
        /// <summary>
        /// For FlowDirection. Pallets is about to leave table
        /// </summary>
        private const string _outgoing = "Outgoing";
        /// <summary>
        /// For FlowDirection. Pallets is about to enter table
        /// </summary>
        private const string _incoming = "Incoming";
        /// <summary>
        /// The height of the lift chains
        /// </summary>
        private const double _liftHeightChains = 0.025;
        #endregion

        #region Constructor
        public PalletTransferTurnTable(Visual sender)
        {
            // This will force the binding of all [Auto] members now instead of after the constructor completes
            sender.SetNativeObject(this);
            Visual = sender;

            // Set properties with custom enumeration
            if (!ConveyorPerformance.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "0.2 m/s", "0.35 m/s", "Other" };
                ConveyorPerformance.Value = new CustomEnumeration(allowed, "0.35 m/s");
            }

            if (!Turn90DegPerformance.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "4 s", "Other" };
                Turn90DegPerformance.Value = new CustomEnumeration(allowed, "4 s");
            }

            if (!ConveyorWidth.Value.Value.Any())
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

            if (!TurnTableDiameter.Value.Value.Any())
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

            if (!ChainPositionMode.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "Center", "Other" };
                ChainPositionMode.Value = new CustomEnumeration(allowed, "Center");
            }
            if (!ChainWidth.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "850 mm", "975 mm" };
                ChainWidth.Value = new CustomEnumeration(allowed, "975 mm");
            }
            if (!InitPositionChains.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "Down", "Up" };
                InitPositionChains.Value = new CustomEnumeration(allowed, "Down");
            }
            if (!ChainPerformance.Value.Value.Any())
            {
                ArrayList allowed = new ArrayList() { "0.2 m/s", "0.35 m/s", "Other" };
                ChainPerformance.Value = new CustomEnumeration(allowed, "0.35 m/s");
            }
        }
        #endregion

        #region EventHandler
        [Auto]
        void OnReset(ConveyorVisual sender)
        {
            // Reset motor to ensure deterministic behaviour:
            ResetMotor();

            // Set initial location of chains:
            ConfigureChain(sender);

            // Set sensors:
            SetSensors(sender);

            // This should not be necessary:
            ChainTransfer.PhysicsEnabled = true;

            // Set start value for variables:
            _palletArrivedViaChain = false;
        }

        [Auto]
        void OnInitialize(ConveyorVisual sender)
        {
            // Set ready for incoming to true:
            SetReadyForIncoming(true);
        }

        [Auto]
        IEnumerable OnRxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Sanity check:
            if (transfer == null) yield break;

            // Set ready for incoming to false;
            SetReadyForIncoming(false);


            // Turn table to target:
            yield return TurnTurnTableTo(transfer.Rx, transfer.RxConnector, _incoming, false, transfer.Load);

            // Remember where the pallet came from, set correct motor directions and raise or lower the chain:
            // Pallet must use chain:
            if (transfer.Tx.Visual is ChainConveyor)
            {
                _palletArrivedViaChain = true;
                SetChainMotorDirectionIncomingTransfer(transfer, ChainTransfer);
                yield return RaiseChain();
            }
            // Pallet arrives from Pallet transfer conveyor:
            else if (transfer.Tx.Visual.Type == "PalletTransferUnit")
            {
                // Pallet uses chain:
                if (transfer.TxConnectorName == "Left" || transfer.TxConnectorName == "Right")
                {
                    _palletArrivedViaChain = true;
                    SetChainMotorDirectionIncomingTransfer(transfer, ChainTransfer);
                    yield return RaiseChain();
                }
                // Pallet uses rollers:
                else
                {
                    _palletArrivedViaChain = false;
                    SetRollerMotorDirectionIncomingTransfer(transfer, sender);
                    yield return LowerChain();
                }
            }
            // Pallet arrives from straight:
            else
            {
                _palletArrivedViaChain = false;
                SetRollerMotorDirectionIncomingTransfer(transfer, sender);
                yield return LowerChain();
            }
        }

        [Auto]
        IEnumerable OnRxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Turn on motor of activated visual:
            var activatedVisual = GetActiveVisualIncomingTransfer(transfer);
            activatedVisual.MotorOn();

            // Logging:
            if (PrintStatus.Value)
                print(Visual + ": active visual incoming " + activatedVisual);

            // Simulate load:
            activatedVisual.SimulateLoad(transfer.Load);

            // Logging:
            if (PrintStatus.Value)
                print(Visual + ": Simulate " + transfer.Load + " with " + activatedVisual);

            // Wait till pallet hits sensor:
            yield return WaitTillPalletHitsSensor(activatedVisual == ChainTransfer ? ChainSensor : RollerSensor, transfer.Load);

            // Wait for pallet to move correct distance before stopping motors to let the pallet stop in center of transfer unit
            yield return WaitToStopMotor(activatedVisual, transfer.Load);

            // Switch off all motors (We wait for the pallet to stop OnTxBeforeTransfer because we might not need to stop at all):
            SwitchOffAllMotors();
        }

        [Auto]
        IEnumerable OnTxBeforeTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Sanity check:
            if (transfer == null) yield break;

            // Turn table to target:
            yield return TurnTurnTableTo(transfer.Tx, transfer.TxConnector, _outgoing, false, transfer.Load);

            // Set motor direction of chain or roller and raise or lower the chains:
            // Pallet must use chain:
            if (transfer.Rx.Visual is ChainConveyor)
            {
                SetChainMotorDirectionOutgoingTransfer(transfer, ChainTransfer);
                yield return RaiseChain(transfer.Load);
            }
            // Special case: Pallet transfer:
            else if (transfer.Rx.Visual.Type == "PalletTransferUnit")
            {
                // Pallet must use chain if Rx in transfer conveyor and the side is used:
                if (transfer.RxConnectorName == "Left" || transfer.RxConnectorName == "Right")
                {
                    SetChainMotorDirectionOutgoingTransfer(transfer, ChainTransfer);
                    yield return RaiseChain(transfer.Load);
                }
                else
                {
                    SetRollerMotorDirectionOutgoingTransfer(transfer, sender);
                    yield return LowerChain(transfer.Load);
                }
            }
            // Pallet must use rollers:
            else
            {
                SetRollerMotorDirectionOutgoingTransfer(transfer, sender);
                yield return LowerChain();
            }
        }

        [Auto]
        IEnumerable OnTxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Get activated visual:
            var activatedVisual = GetActiveVisualOutgoingTransfer(transfer);

            // Make sure the load is not simulated by any other entity than the acitvated visual:
            activatedVisual.SimulateLoad(transfer.Load);

            //Check if motor velocity should be decreased due to next conveyor has lower speed
            //If speed decreased then wait for pallet to reach new speed before turning motor on again
            if (OutboundSpeed.CheckOutboundSpeed(activatedVisual, transfer.Rx.Visual) && sender.Motor.CurrentSpeed > 0)
            {
                yield return Wait.ForSeconds(StopTime(activatedVisual, activatedVisual.MotorSpeed));
            }

            // Switch on motor of activated visual:
            activatedVisual.MotorOn();
        }

        [Auto]
        void OnTxAfterTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Switch off all motors:
            SwitchOffAllMotors();

            //Reset motor speed if it has been changed for outbound
            SetConveyorSpeed(sender);
        }

        [Auto]
        IEnumerable OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            // Sanity check:
            if (transfer == null)
                yield break;

            // Go to home position after task:
            if (AutoHome.Value)
            {
                //Make sure no other pallet transfers into this conveyor before it has moved back to home pos
                if (PrintStatus.Value)
                    print(Visual + " OnTxTransferComplete");
                SetReadyForIncoming(false);
                document.Start(TurnHome);
            }
            // Try to get a new transfer:
            else
            {
                SetReadyForIncoming(true);
                Transfer.UserDispatchIn(sender);
            }

        }

        /// <summary>
        /// Turn home after each job:
        /// </summary>
        /// <returns></returns>
        private IEnumerable TurnHome()
        {
            // Get start connector:
            var start = Visual.FindConnector("Start");
            if (start == null)
                throw new InvalidOperationException("Could not find Connector Start of " + Visual);

            // Turn home (to start 
            yield return TurnTurnTableTo(Visual, start, _incoming, true, null);

            // Set ready for incoming to true and try to dispatch again:
            SetReadyForIncoming(true);
            Transfer.UserDispatchIn(Visual);
        }

        /// <summary>
        /// Moves chain to roller level
        /// </summary>
        /// <param name="loadToSimulate"></param>
        /// <returns></returns>
        private IEnumerable LowerChain(Visual loadToSimulate = null)
        {
            if (PrintStatus.Value)
                print(Visual + ": LowerChain " + ChainTransfer.LocalY);
            // Lower the lift if it is not already down
            if (ChainTransfer.LocationY != 0)
            {
                //Check if load to be simulated
                if (loadToSimulate != null)
                    ChainTransfer.SimulateLoad(loadToSimulate);
                //Move chains to 0 location
                ChainTransfer.MoveTo(Visual, vector(ChainTransfer.LocationX, 0, ChainTransfer.LocationZ), _liftHeightChains / LiftingTimeChains.Value);
                yield return Wait.ForMove(ChainTransfer);

            }
            if (PrintStatus.Value)
                print(Visual + ": LowerChain finished");
        }

        /// <summary>
        /// Raises chain to lift height of chains
        /// </summary>
        /// <param name="loadToSimulate"></param>
        /// <returns></returns>
        private IEnumerable RaiseChain(Visual loadToSimulate = null)
        {
            if (PrintStatus.Value)
                print(Visual + ": RaiseChain " + ChainTransfer.LocalY);
            // Raise the lift if it is not already up
            if (ChainTransfer.LocationY != _liftHeightChains)
            {
                //Check if load to be simulated
                if (loadToSimulate != null)
                    ChainTransfer.SimulateLoad(loadToSimulate);
                //Move chains to LiftHeight
                ChainTransfer.MoveTo(Visual, vector(ChainTransfer.LocationX, _liftHeightChains, ChainTransfer.LocationZ), _liftHeightChains / LiftingTimeChains.Value);
                yield return Wait.ForMove(ChainTransfer);

            }
            if (PrintStatus.Value)
                print(Visual + ": RaiseChain finished");
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
                ConveyorSpeed.Hidden = false;
                ConveyorSpeed.ReadOnly = true;
                SetStandardConveyorPerformance();
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

            SetSensors(sender);
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
            SetSensors(sender);
        }

        [Auto]
        void OnMoved(ConveyorVisual sender, MatrixUpdateType type)
        {
            TurnTableHeight.Value = sender.ConveyorHeight;
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
                ChainSpeed.Hidden = false;
                ChainSpeed.ReadOnly = true;
                SetStandardConveyorPerformance();
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
        void OnChainWidthUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            //Check width of conveyor
            if (newValue.Value == "850 mm")
                ChainTransfer.Width = 0.85;
            else
                ChainTransfer.Width = 0.975;

            //SetSensors(sender);
        }

        [Auto]
        void OnChainOffsetRightUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            ConfigureChain(sender);

        }

        [Auto]
        void OnChainOffsetLeftUpdated(ConveyorVisual sender, DistanceProperty newValue, DistanceProperty oldValue)
        {
            ConfigureChain(sender);

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

            // SetSensors(sender);
        }

        [Auto]
        void OnInitPositionChainsUpdated(ConveyorVisual sender, CustomEnumeration newValue, CustomEnumeration oldValue)
        {
            ConfigureChain(sender);
        }
        #endregion

        #region StopTime
        /// <summary>
        /// Calculates time to wait for pallet to decellerate to 0 m/s
        /// </summary>
        /// <param name="conveyor"></param>
        /// <returns></returns>
        private TimeProperty StopTime(ConveyorVisual conveyor, double targetSpeed)
        {
            ExprDouble vMax;

            if (conveyor.Name == "ChainTransfer")
                vMax = ChainSpeed.Value.MaxSpeed;
            else
                vMax = ConveyorSpeed.Value.MaxSpeed;

            var dec = conveyor.Motor.Deceleration;
            var stopTime = (vMax.Value - targetSpeed) / dec;
            if (stopTime < 0)
                throw new Exception(Visual + ": Attention. Stop time is below zero");
            return stopTime;
        }
        #endregion

        #region Turning
        /// <summary>
        /// Calculate the turn table to its target position and wait before that if necessary pallet has completey stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="con"></param>
        /// <param name="flowDirection"></param>
        /// <param name="autoHomeMovement"></param>
        /// <returns></returns>
        private IEnumerable TurnTurnTableTo(Visual sender, Connector con, string flowDirection, bool autoHomeMovement, Visual load)
        {
            // Get angle (add 180° if outgoing):
            var angle = flowDirection == _incoming ? GetAngleOfConnector(con) : 180 + GetAngleOfConnector(con);

            // Add additional angle if incoming from chain conveyor:
            var connectedVisual = con.ConnectedTo.Parent;
            if (connectedVisual is ChainConveyor)
                angle += 90;
            else
            {
                if (connectedVisual.Type == "PalletTransferUnit")
                {
                    if (con.ConnectedTo.Name == "Left" || con.ConnectedTo.Name == "Right")
                        angle += 90;
                }
            }

            // Cast turn table:
            var turnTable = CastVisualToTurnTableConveyor(sender);

            // Add additional angle if motor of turn table is reverse:
            if (turnTable.Motor.Direction == MotorDirection.Reverse)
                angle += 180;

            // Set target position:
            turnTable.TargetPosition = angle;

            // Get difference between current and target position (Round as well):
            var diff = Math.Round(Math.Abs(turnTable.Position - turnTable.TargetPosition), 0);

            // Logging:
            if (PrintStatus.Value)
                print(Visual + " diff (round) " + diff);

            // Wait for move it the table has to turn:
            if (diff > 0.01 && Math.Abs(diff - 360) > 0.01 && Math.Abs(diff - 180) > 0.01)
            {
                // Wait till the pallet completely stopped before turning (only if this pallet is outgoing):
                if (flowDirection == _outgoing)
                {
                    if (_palletArrivedViaChain)
                        //Wait for pallet to completely stop
                        yield return Wait.ForSeconds(StopTime(ChainTransfer, 0));
                    else
                        //Wait for pallet to completely stop
                        yield return Wait.ForSeconds(StopTime((ConveyorVisual)sender, 0));
                }

                // Stick load to turn table:
                if (flowDirection == _outgoing)
                    load.StickKinematic(sender);

                // Rotate turn table and wait till move is finished:
                turnTable.RotateToTarget();
                yield return Wait.ForMove(turnTable);

                // Unstick load:
                if (flowDirection == _outgoing)
                    load.Unstick();
            }
        }

        /// <summary>
        /// Returns the angle by the connector name (e.g. C270 = 270)
        /// </summary>
        /// <param name="connector"></param>
        /// <returns></returns>
        private double GetAngleOfConnector(Connector connector)
        {
            // Get connector name
            var connectorName = connector.Name;

            switch (connectorName)
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
        private void SetStandardConveyorPerformance()
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
            if (ChainPerformance.Value.Value == "0.2 m/s")
            {
                ChainSpeed.Value.MaxSpeed = 0.2;
                ChainSpeed.Value.Acceleration = 0.4;
                ChainSpeed.Value.Deceleration = 0.4;
            }
            else if (ChainPerformance.Value.Value == "0.35 m/s")
            {
                print("0,35");
                ChainSpeed.Value.MaxSpeed = 0.35;
                ChainSpeed.Value.Acceleration = 0.4;
                ChainSpeed.Value.Deceleration = 0.4;
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
            ChainTransfer.Motor.Speed = ChainSpeed.Value.MaxSpeed;
            ChainTransfer.Motor.Acceleration = ChainSpeed.Value.Acceleration;
            ChainTransfer.Motor.Deceleration = ChainSpeed.Value.Deceleration;
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
            var turnTableRing = sender.FindChild("TurnTableRing") as CylinderVisual;

            // Sanity check:
            if (turnTableRing == null)
                throw new Exception(Visual + ": Could not find child TurnTableRing");

            // Set dimensions and location of the ring:
            turnTableRing.InnerDiameter = diameter;
            turnTableRing.Diameter = diameter + 0.02;
            turnTableRing.Location = vector(conveyorLength / 2, -0.12, 0);

            // Configure connectors one by one:
            foreach (var connector in sender.AllConnectors)
            {
                // Get angle of connector:
                var angle = GetAngleOfConnector(connector);

                // Switch off AutoConfigure:
                connector.AutoConfigure = false;

                // Calculate x and z location for connector
                var xLocation = diameter / 2 - Math.Cos((Math.PI / 180) * angle) * diameter / 2 - (diameter - conveyorLength) / 2;
                var zLocation = Math.Sin((Math.PI / 180) * angle) * diameter / 2;
                connector.Start = connector.End = vector(xLocation, 0, zLocation);
            }
        }
        #endregion

        #region Aux
        private TurntableConveyor CastVisualToTurnTableConveyor(Visual visual)
        {
            TurntableConveyor conv = (TurntableConveyor)visual;
            if (conv == null)
                throw new InvalidCastException("Could not case " + visual + " as TurntableConveyor");
            else
                return conv;
        }

        /// <summary>
        /// Reset motors direction and turn them off
        /// </summary>
        private void ResetMotor()
        {
            ChainTransfer.Motor.Direction = MotorDirection.Forwards;
            ChainTransfer.MotorOff();
            ((ConveyorVisual)Visual).Motor.Direction = MotorDirection.Forwards;
            ((ConveyorVisual)Visual).MotorOff();
            SetConveyorSpeed((ConveyorVisual)Visual);
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

            double initialHeight = (InitPositionChains.Value.Value == "Down" ? 0 : _liftHeightChains);

            ChainTransfer.Location = vector(ChainPosition.Value, initialHeight, -ChainOffsetLeft.Value - (sender.Width - 0.04) / 2);
        }

        /// <summary>
        /// Sets direction of chain conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="chain"></param>
        private void SetChainMotorDirectionIncomingTransfer(Transfer transfer, ChainConveyor chain)
        {
            var distanceRel = chain.TransformFromWorld(transfer.TxConnector.WorldCenter);
            var distance =
                Math.Round(Math.Sqrt(Math.Pow(distanceRel.X, 2) + Math.Pow(distanceRel.Y, 2) + Math.Pow(distanceRel.Z, 2)), 2);
            chain.Motor.Direction = distance < chain.Length ? MotorDirection.Forwards : MotorDirection.Reverse;
            if (PrintStatus.Value)
                print(Visual + ": Distance between load and chain: " + distance + " Chain motor direction " + chain.Motor.Direction + " Incoming");
        }

        /// <summary>
        /// Sets direction of roller conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="conveyor"></param>
        private void SetRollerMotorDirectionIncomingTransfer(Transfer transfer, ConveyorVisual conveyor)
        {
            var distanceRel = conveyor.TransformFromWorld(transfer.TxConnector.WorldCenter);
            var distance =
                Math.Round(Math.Sqrt(Math.Pow(distanceRel.X, 2) + Math.Pow(distanceRel.Y, 2) + Math.Pow(distanceRel.Z, 2)), 2);
            conveyor.Motor.Direction = distance < conveyor.Length ? MotorDirection.Forwards : MotorDirection.Reverse;
            if (PrintStatus.Value)
                print(Visual + ": Distance between load and conveyor: " + distance + " conveyor motor direction " + conveyor.Motor.Direction + " Incoming");
        }

        /// <summary>
        /// Sets direction of chain conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="chain"></param>
        private void SetChainMotorDirectionOutgoingTransfer(Transfer transfer, ChainConveyor chain)
        {
            var distanceRel = chain.TransformFromWorld(transfer.RxConnector.WorldCenter);
            var distance =
                Math.Round(Math.Sqrt(Math.Pow(distanceRel.X, 2) + Math.Pow(distanceRel.Y, 2) + Math.Pow(distanceRel.Z, 2)), 2);

            chain.Motor.Direction = distance >= chain.Length ? MotorDirection.Forwards : MotorDirection.Reverse;

            if (PrintStatus.Value)
                print(Visual + ": Distance between load and chain: " + distance + " Chain motor direction " + chain.Motor.Direction + " Outgoing");
        }

        /// <summary>
        /// Sets direction of roller conveyor for incoming transfers
        /// </summary>
        /// <param name="transfer"></param>
        /// <param name="conveyor"></param>
        private void SetRollerMotorDirectionOutgoingTransfer(Transfer transfer, ConveyorVisual conveyor)
        {
            // Get the distance between the Rx connector and the conveyor
            var distanceRel = conveyor.TransformFromWorld(transfer.RxConnector.WorldCenter);
            var distance = Math.Round(Math.Sqrt(Math.Pow(distanceRel.X, 2) + Math.Pow(distanceRel.Y, 2) + Math.Pow(distanceRel.Z, 2)), 2);
            conveyor.Motor.Direction = distance >= conveyor.Length ? MotorDirection.Forwards : MotorDirection.Reverse;

            // Logging:
            if (PrintStatus.Value)
                print(Visual + ": Distance between load and conveyor: " + distance + " conveyor motor direction " + conveyor.Motor.Direction + " Outgoing");
        }

        /// <summary>
        /// Returns the Visual that receive the load. Either Chain or conveyor.
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private ConveyorVisual GetActiveVisualIncomingTransfer(Transfer transfer)
        {
            // Return chain:
            if (transfer.Tx.Visual is ChainConveyor)
                return ChainTransfer;
            // Special case: Pallet transfer:
            if (transfer.Tx.Visual.Type == "PalletTransferUnit")
            {
                // Pallet must use chain if Rx in transfer conveyor and the side is used:
                if (transfer.TxConnectorName == "Left" || transfer.TxConnectorName == "Right")
                    return ChainTransfer;
                return (ConveyorVisual)Visual;
            }
            // Return roller conveyor:
            return (ConveyorVisual)Visual;
        }

        /// <summary>
        /// Returns the Visual that receive the load. Either Chain or conveyor.
        /// </summary>
        /// <param name="transfer"></param>
        /// <returns></returns>
        private ConveyorVisual GetActiveVisualOutgoingTransfer(Transfer transfer)
        {
            if (PrintStatus.Value)
                print(Visual + ": RxVisual is " + transfer.Rx.Visual + "; is chainVisual: " + (transfer.Rx.Visual is ChainConveyor));
            // Return chain:
            if (transfer.Rx.Visual is ChainConveyor)
                return ChainTransfer;
            // Return roller conveyor:
            else
                return (ConveyorVisual)Visual;
        }

        /// <summary>
        /// Wait until pallet reaches active sensor
        /// </summary>
        /// <param name="activeSensor"></param>
        /// <param name="load"></param>
        /// <returns></returns>
        private IEnumerable WaitTillPalletHitsSensor(BoxVisual activeSensor, Visual load)
        {
            // Disbale all sensors:
            DisableAllSensors();

            // Get right sensor:
            activeSensor.PhysicsEnabled = true;

            if (PrintStatus.Value)
                print(Visual + ": Wait till " + load + " blocks " + activeSensor);

            // Wait till the pallet hits the sensor:
            while (activeSensor.BlockingLoad != load)
                yield return Wait.ForEvent(activeSensor.OnBlocked);

            if (PrintStatus.Value)
                print(Visual + ": Finished waiting for " + load + " blocking " + activeSensor);
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
        /// Set dimensions and location of sensors
        /// </summary>
        /// <param name="sender"></param>
        private void SetSensors(ConveyorVisual sender)
        {
            //Set width of roller sensor
            RollerSensor.Width = sender.Width + 0.07;
            // Set position: 
            RollerSensor.Location = vector(ChainPosition.Value, 0.075, 0);
            RollerSensor.Parent = Visual;
            RollerSensor.PhysicsEnabled = true;

            //Set width of chain sensor
            ChainSensor.Width = ChainTransfer.Width + 0.1;
            ChainSensor.Parent = ChainTransfer;

            // Set position:
            ChainSensor.Location = vector(ChainTransfer.Length / 2, _liftHeightChains + 0.075, 0);
            ChainSensor.PhysicsEnabled = true;
        }

        /// <summary>
        /// Switch all conveyor motors off
        /// </summary>
        private void SwitchOffAllMotors()
        {
            // Turn off both motors:
            var conveyor = (ConveyorVisual)Visual;
            conveyor.MotorOff();
            ChainTransfer.MotorOff();
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
            var vMax = conveyor.Motor.Speed;
            var dec = conveyor.Motor.Deceleration;
            var stopTime = vMax.Value / dec;
            if (stopTime < 0)
                throw new Exception(Visual + ": Attention. Stop time is below zero");
            var stopingDistance = 0.5 * dec * Math.Pow(stopTime, 2);

            //Get length of pallet in conveyors direction
            var palletLength = conveyor.Name == "ChainTransfer" ? LoadDimensions.GetDepth(load, true) : LoadDimensions.GetWidth(load, true);

            //Pallet should move half length minus stoping distance before stopping conveyor
            var waitTime = (palletLength / 2 - stopingDistance) / vMax.Value;

            if (PrintStatus.Value)
                print(Visual + ": Start waiting till motor can be switched off. Load. " + load + ". Length: " + palletLength);

            yield return Wait.ForSeconds(waitTime);
        }
        #endregion
    }
}
