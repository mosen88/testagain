using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using Demo3D.Native;
using Demo3D.Utilities;
using Demo3D.Visuals;

namespace PalletConveyorCatalog
{
    //PalletConveyorCatalog.PalletSlaveConveyor
    public class PalletSlaveConveyor
    {
        #region Declaration
        //Properties defined for this native object
        private Visual Visual { get; set; }
        [Auto] private IBuilder app;
        [Auto] private Document document;
        [Auto] private PrintDelegate print;
        [Auto] private VectorDelegate vector;

        /// <summary>
        /// The master visual (the next conveyor)
        /// </summary>
        private Visual _master;

        #region SetReadyForIncoming
        private bool _readyForIncoming;
        private void SetReadyForIncoming(bool value)
        {
            _readyForIncoming = value;
            Visual.TransferState.ReadyForIncoming = value;
            if (PrintStatus.Value)
                print(Visual + ": RFI set to " + _readyForIncoming);
        }
        #endregion
        #endregion

        #region Properties
        /// <summary>
        /// Set to true to enable logging
        /// </summary>
        [Auto, Category("_Configuration"), Description("Set to true to enable logging")]
        SimplePropertyValue<bool> PrintStatus;
        #endregion

        #region Constructor
        public PalletSlaveConveyor(ConveyorVisual sender)
        {
            Visual = sender;
            sender.SetNativeObject(this);
        }
        #endregion

        #region Event handler
        [Auto]
        void OnReset(ConveyorVisual sender)
        {
            sender.Motor.InitialState = MotorState.Off;

            // Locate sensors and connectora:
            LocateConnectorsAndSensors();
        }

        [Auto]
        void OnInitialize(ConveyorVisual sender)
        {
            // Be ready for incoming
            SetReadyForIncoming(true);
            // Get master and do some sanity checks
            _master = sender.Next;

            // Add function listener:
            var sensor = (PhotoEye)Visual.FindImmediateChild("EndSensor");
            sensor.OnBlocked.Clear();
            sensor.OnBlocked.NativeListeners += Sensor_OnBlocked;
        }

        [Auto]
        IEnumerable OnDispatchIn(ConveyorVisual sender)
        {
            // Mark event as handled
            Demo3D.Script.ScriptThread.CurrentEventHandled = true;

            // Wait until master is ready
            if (!_master.TransferState.ReadyForIncoming)
                yield return Wait.UntilTrue(() => _master.TransferState.ReadyForIncoming, _master.TransferState);

            // Start the transfer
            var nextTransfer = Visual.TransferState.FirstIncomingWaitingToStart;
            nextTransfer?.Run();
        }

        [Auto]
        IEnumerable OnRxTransfer(ConveyorVisual sender, Transfer tranfer)
        {
            // Not ready for incoming until load arrived at master
            SetReadyForIncoming(false);

            // Switch on motor:
            sender.MotorOn();

            // Wait till load hits sensor
            var sensor = (PhotoEye)Visual.FindImmediateChild("EndSensor");
            yield return Wait.ForEvent(sensor.OnBlocked);

            sender.MotorOff();
        }

        [Auto]
        void OnTxTransfer(ConveyorVisual sender, Transfer transfer)
        {
            // Switch on motor:
            sender.MotorOn();
        }

        [Auto]
        void OnTxTransferComplete(ConveyorVisual sender, Transfer transfer)
        {
            // Ready for incoming again
            SetReadyForIncoming(true);
            // Switch off motor if there are any incoming loads:
            if (!Visual.TransferState.Incoming.Any())
                sender.MotorOff();
            else
                Transfer.UserDispatchIn(sender);
        }
        #endregion

        #region Events of children
        private void Sensor_OnBlocked(PhotoEye sensor, Visual load)
        {
            // Ignore spontaneous loads if the conveyor is handling any loads already
            if (Visual.TransferState.CapacityInUse > 0) return;

            SetReadyForIncoming(false);
            Transfer.ForceProcessComplete(Visual, load);
            Transfer.Dispatch(Visual);
        }
        #endregion

        #region Aux
        /// <summary>
        /// Locate all connectors and sensors
        /// </summary>
        private void LocateConnectorsAndSensors()
        {
            var conveyor = (ConveyorVisual)Visual;
            // Sensors:
            // Locate the sensor depending on the speed and acceleration of the conveyor
            var sensor = (PhotoEye)Visual.FindImmediateChild("EndSensor");
            var maximumSpeed = conveyor.Motor.Speed;
            var motorDeceleration = conveyor.Motor.Deceleration;
            var stopTime = maximumSpeed.Value / motorDeceleration;
            var stopingDistance = 0.5 * motorDeceleration * Math.Pow(stopTime, 2);
            var sensorPosition = conveyor.Length - stopingDistance;

            // Set position:
            sensor.Position = sensorPosition - 0.0001;

            // Connectors:
            var startConnector = Visual.FindConnector("Start");
            var endConnector = Visual.FindConnector("End");
            var startPosition = vector(0, 0, 0);
            var endPosition = vector(conveyor.Length, 0, 0);
            startConnector.Start = startPosition;
            startConnector.End = startPosition;
            endConnector.Start = endPosition;
            endConnector.End = endPosition;
        }
        #endregion
    }
}
