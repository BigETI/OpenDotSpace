using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace OpenDotSpacePrograms.Programs.Elevator
{
    public sealed class Program : MyGridProgram
    {
        /// <summary>
        /// This programs controls an elevator.
        /// 
        /// Required are pistons to move the elevator and doors for each elevation
        /// 
        /// ---
        /// 
        /// Programmable block custom data format:
        ///
        /// ElevatorName=Name of the elevator
        /// WaitForClosingDoorsTime=Time in seconds needed to wait for the doors to close (optional, default 3 seconds)
        /// PistonsSpeed=Speed in meters per second of all the pistons summed up (optional, default 2 meters per second)
        /// 
        /// Example:
        /// 
        /// ElevatorName=MyElevator
        /// WaitForClosingDoorsTime=3
        /// PistonsSpeed=2
        /// 
        /// ---
        /// 
        /// Piston custom data format:
        /// 
        /// ElevatorName=Name of the elevator
        /// 
        /// Example:
        /// 
        /// ElevatorName=MyElevator
        /// 
        /// ---
        /// 
        /// Door custom data format:
        /// 
        /// ElevatorName=Name of the elevator
        /// ElevatorDoorName=Name of the elevator door
        /// PistonsDistance=The distance in meters pistons have to move summed up to reach this door
        /// </summary>

        private enum EElevatorState
        {
            WaitingForInput,

            WaitingToCloseDoors,

            ClosingDoors,

            MovingElevator,

            OpeningDoors
        }

        private delegate void KeyValuePairParsedDelegate(string key, string value);

        private struct ElevatorDoor
        {
            public IMyDoor Door { get; }

            public float PistonsDistance { get; }

            public ElevatorDoor(IMyDoor door, float pistonsDistance)
            {
                Door = door;
                PistonsDistance = pistonsDistance;
            }
        };

        private static readonly string[] newLineDelimiters = new string[] { "\r\n", "\n", "\r" };

        private static readonly string[] commandDelimiters = new string[] { " ", "\t" };

        private readonly List<IMyDoor> doors = new List<IMyDoor>();

        private readonly List<IMyPistonBase> pistons = new List<IMyPistonBase>();

        private readonly List<ElevatorDoor> elevatorDoorQueue = new List<ElevatorDoor>();

        private string oldCustomData = string.Empty;

        private static readonly double defaultWaitForClosingDoorsTime = 3.0;

        private static readonly float defaultPistonsSpeed = 2.0f;

        private string elevatorName = string.Empty;

        private double waitingForClosingDoorsTime = defaultWaitForClosingDoorsTime;

        private float pistonsSpeed = defaultPistonsSpeed;

        private Dictionary<string, ElevatorDoor> elevatorDoors;

        private EElevatorState elevatorState = EElevatorState.WaitingForInput;

        private DateTime waitingForClosingElevatorDoorsStartDateTime;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private static void ParseCustomDataKeyValuePairs(IMyTerminalBlock terminalBlock, KeyValuePairParsedDelegate onKeyValuePairParsed)
        {
            string[] custom_data_lines = terminalBlock.CustomData.Split(newLineDelimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (string custom_data_line in custom_data_lines)
            {
                int equals_index = custom_data_line.IndexOf('=');
                if ((equals_index > 0) && (custom_data_line.Length > (equals_index + 1)))
                {
                    onKeyValuePairParsed(custom_data_line.Substring(0, equals_index).Trim().ToLowerInvariant(), custom_data_line.Substring(equals_index + 1).Trim());
                }
            }
        }

        private void StartWaitingForClosingElevatorDoors()
        {
            waitingForClosingElevatorDoorsStartDateTime = DateTime.Now;
        }

        private bool IsWaitingForClosingDoorsFinished =>
            (DateTime.Now - waitingForClosingElevatorDoorsStartDateTime).TotalSeconds > waitingForClosingDoorsTime;

        public void Main(string argument, UpdateType updateSource)
        {
            if (oldCustomData != Me.CustomData)
            {
                elevatorName = string.Empty;
                waitingForClosingDoorsTime = defaultWaitForClosingDoorsTime;
                pistonsSpeed = defaultPistonsSpeed;
                ParseCustomDataKeyValuePairs
                (
                    Me,
                    (key, value) =>
                    {
                        switch (key)
                        {
                            case "elevatorname":
                                elevatorName = value.ToLowerInvariant();
                                break;
                            case "waitforclosingdoorstime":
                                try
                                {
                                    double wait_for_closing_doors_time =
                                        double.Parse
                                        (
                                            value,
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture
                                        );
                                    waitingForClosingDoorsTime = (wait_for_closing_doors_time < 0.0) ? waitingForClosingDoorsTime : wait_for_closing_doors_time;
                                }
                                catch (Exception e)
                                {
                                    Echo($"[EXCEPTION] {e}");
                                }
                                break;
                            case "pistonsspeed":
                                try
                                {
                                    float pistons_speed =
                                        float.Parse
                                        (
                                            value,
                                            System.Globalization.NumberStyles.Any,
                                            System.Globalization.CultureInfo.InvariantCulture
                                        );
                                    pistonsSpeed = (pistons_speed > float.Epsilon) ? pistons_speed : pistonsSpeed;
                                }
                                catch (Exception e)
                                {
                                    Echo($"[EXCEPTION] {e}");
                                }
                                break;
                        }
                    }
                );
                oldCustomData = Me.CustomData;
            }
            if (elevatorName.Length > 0)
            {
                if (elevatorDoors == null)
                {
                    elevatorDoors = new Dictionary<string, ElevatorDoor>();
                    InitializeBlocks();
                }
                string[] command_parts = argument.Split(commandDelimiters, StringSplitOptions.RemoveEmptyEntries);
                if (command_parts.Length > 0)
                {
                    switch (command_parts[0].ToLowerInvariant())
                    {
                        case "reload":
                            InitializeBlocks();
                            break;
                        case "call":
                            if (command_parts.Length > 1)
                            {
                                string elevator_door_name = command_parts[1].ToLowerInvariant();
                                if (elevatorDoors.ContainsKey(elevator_door_name))
                                {
                                    ElevatorDoor elevator_door = elevatorDoors[elevator_door_name];
                                    bool is_inserting_elevator_door = true;
                                    int elevator_door_insertion_index = 0;
                                    float closest_distance = float.PositiveInfinity;
                                    for (int elevator_door_index = 0; elevator_door_index < elevatorDoorQueue.Count; elevator_door_index++)
                                    {
                                        ElevatorDoor enqueued_elevator_door = elevatorDoorQueue[elevator_door_index];
                                        if (elevator_door.Door == enqueued_elevator_door.Door)
                                        {
                                            is_inserting_elevator_door = false;
                                            break;
                                        }
                                        float distance = Math.Abs(elevator_door.PistonsDistance - enqueued_elevator_door.PistonsDistance);
                                        if (closest_distance > distance)
                                        {
                                            closest_distance = distance;
                                            elevator_door_insertion_index = elevator_door_index;
                                        }
                                    }
                                    if (is_inserting_elevator_door)
                                    {
                                        elevatorDoorQueue.Insert(elevator_door_insertion_index, elevator_door);
                                        foreach (IMyPistonBase piston in pistons)
                                        {
                                            if (piston.IsWorking)
                                            {
                                                if ((piston.Status == PistonStatus.Extending) || (piston.Status == PistonStatus.Retracting))
                                                {
                                                    piston.Velocity = 0.0f;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Echo($"[ERROR] Invalid elevator door \"{elevator_door_name}\".");
                                }
                            }
                            break;
                    }
                }
                bool is_performing_tick_again = true;
                uint tick_count = 0U;
                while (is_performing_tick_again)
                {
                    is_performing_tick_again = false;
                    if ((elevatorState != EElevatorState.WaitingForInput) && (elevatorDoorQueue.Count <= 0))
                    {
                        elevatorState = EElevatorState.WaitingForInput;
                    }
                    switch (elevatorState)
                    {
                        case EElevatorState.WaitingForInput:
                            if (elevatorDoorQueue.Count > 0)
                            {
                                elevatorState = EElevatorState.WaitingToCloseDoors;
                                StartWaitingForClosingElevatorDoors();
                                is_performing_tick_again = true;
                            }
                            break;
                        case EElevatorState.WaitingToCloseDoors:
                            if (IsWaitingForClosingDoorsFinished)
                            {
                                elevatorState = EElevatorState.ClosingDoors;
                                is_performing_tick_again = true;
                            }
                            break;
                        case EElevatorState.ClosingDoors:
                            bool are_all_doors_closed = true;
                            foreach (ElevatorDoor elevator_door in elevatorDoors.Values)
                            {
                                if (elevator_door.Door.IsFunctional)
                                {
                                    switch (elevator_door.Door.Status)
                                    {
                                        case DoorStatus.Opening:
                                        case DoorStatus.Closing:
                                            are_all_doors_closed = false;
                                            break;
                                        case DoorStatus.Open:
                                            are_all_doors_closed = false;
                                            elevator_door.Door.Enabled = true;
                                            elevator_door.Door.CloseDoor();
                                            break;
                                        case DoorStatus.Closed:
                                            elevator_door.Door.Enabled = false;
                                            break;
                                    }
                                }
                            }
                            if (are_all_doors_closed)
                            {
                                elevatorState = EElevatorState.MovingElevator;
                                is_performing_tick_again = true;
                            }
                            break;
                        case EElevatorState.MovingElevator:
                            bool are_pistons_unoccupied = true;
                            float minimal_pistons_distance = 0.0f;
                            float maximal_pistons_distance = 0.0f;
                            uint working_piston_count = 0U;
                            foreach (IMyPistonBase piston in pistons)
                            {
                                if (piston.IsWorking)
                                {
                                    if ((piston.Status == PistonStatus.Extending) || (piston.Status == PistonStatus.Retracting))
                                    {
                                        are_pistons_unoccupied = false;
                                        break;
                                    }
                                    minimal_pistons_distance += piston.LowestPosition;
                                    maximal_pistons_distance += piston.HighestPosition;
                                    ++working_piston_count;
                                }
                            }
                            if (are_pistons_unoccupied)
                            {
                                if (working_piston_count > 0U)
                                {
                                    ElevatorDoor enqueued_elevator_door = elevatorDoorQueue[0];
                                    if (enqueued_elevator_door.PistonsDistance > maximal_pistons_distance)
                                    {
                                        Echo($"[ERROR] Impossible to reach elevator door \"{enqueued_elevator_door.Door.Name}\" of pistons distance \"{enqueued_elevator_door.PistonsDistance}\". Maximal pistons distance possible: \"{maximal_pistons_distance}\"");
                                        elevatorDoorQueue.RemoveAt(0);
                                    }
                                    else
                                    {
                                        bool are_piston_movements_finished = true;
                                        float new_piston_position = enqueued_elevator_door.PistonsDistance / working_piston_count;
                                        float piston_speed = pistonsSpeed / working_piston_count;
                                        float leftover_piston_distance = 0.0f;
                                        foreach (IMyPistonBase piston in pistons)
                                        {
                                            if (piston.IsWorking)
                                            {
                                                switch (piston.Status)
                                                {
                                                    case PistonStatus.Stopped:
                                                    case PistonStatus.Extended:
                                                    case PistonStatus.Retracted:
                                                        float actual_new_piston_distance = new_piston_position + leftover_piston_distance;
                                                        leftover_piston_distance = 0.0f;
                                                        if (piston.CurrentPosition > (actual_new_piston_distance + float.Epsilon))
                                                        {
                                                            if (piston.LowestPosition > actual_new_piston_distance)
                                                            {
                                                                leftover_piston_distance = actual_new_piston_distance - piston.LowestPosition;
                                                                actual_new_piston_distance = piston.LowestPosition;
                                                            }
                                                            if (piston.MaxLimit < actual_new_piston_distance)
                                                            {
                                                                piston.MaxLimit = actual_new_piston_distance;
                                                            }
                                                            piston.MinLimit = actual_new_piston_distance;
                                                            piston.Velocity = -piston_speed;
                                                            are_piston_movements_finished = false;
                                                        }
                                                        else if (piston.CurrentPosition < (actual_new_piston_distance - float.Epsilon))
                                                        {
                                                            if (piston.HighestPosition < actual_new_piston_distance)
                                                            {
                                                                leftover_piston_distance = actual_new_piston_distance - piston.HighestPosition;
                                                                actual_new_piston_distance = piston.HighestPosition;
                                                            }
                                                            if (piston.MinLimit > actual_new_piston_distance)
                                                            {
                                                                piston.MinLimit = actual_new_piston_distance;
                                                            }
                                                            piston.MaxLimit = actual_new_piston_distance;
                                                            piston.Velocity = piston_speed;
                                                            are_piston_movements_finished = false;
                                                        }
                                                        break;
                                                    case PistonStatus.Extending:
                                                    case PistonStatus.Retracting:
                                                        are_piston_movements_finished = false;
                                                        break;
                                                }
                                            }
                                        }
                                        if (are_piston_movements_finished)
                                        {
                                            elevatorState = EElevatorState.OpeningDoors;
                                            is_performing_tick_again = true;
                                        }
                                    }
                                }
                                else
                                {
                                    Echo("[ERROR] Not a single piston is working right now.");
                                    elevatorDoorQueue.Clear();
                                    elevatorState = EElevatorState.WaitingForInput;
                                }
                            }
                            break;
                        case EElevatorState.OpeningDoors:
                            IMyDoor door = elevatorDoorQueue[0].Door;
                            if (door.IsFunctional)
                            {
                                if (door.Status == DoorStatus.Closed)
                                {
                                    door.Enabled = true;
                                    door.OpenDoor();
                                }
                                else if (door.Status == DoorStatus.Open)
                                {
                                    door.Enabled = false;
                                    elevatorDoorQueue.RemoveAt(0);
                                    elevatorState = EElevatorState.WaitingForInput;
                                }
                            }
                            else
                            {
                                elevatorDoorQueue.RemoveAt(0);
                                elevatorState = EElevatorState.WaitingForInput;
                            }
                            break;
                        default:
                            Echo($"[EXCEPTION] Elevator state \"{elevatorState}\" has not been implemented yet.");
                            break;
                    }
                    ++tick_count;
                    if (tick_count >= 10U)
                    {
                        Echo($"[WARNING] Too many ticks were performed. Current elevator state: {elevatorState}");
                        break;
                    }
                }
            }
            else
            {
                Echo("[ERROR] Please specify an elevator name to this programmable block.");
            }
        }

        private void InitializeBlocks()
        {
            elevatorDoors.Clear();
            GridTerminalSystem.GetBlocksOfType
            (
                doors,
                (door) =>
                {
                    if (door.IsFunctional)
                    {
                        string elevator_name = null;
                        string elevator_door_name = null;
                        float? pistons_distance = null;
                        ParseCustomDataKeyValuePairs
                        (
                            door,
                            (key, value) =>
                            {
                                switch (key)
                                {
                                    case "elevatorname":
                                        elevator_name = value.ToLowerInvariant();
                                        break;
                                    case "elevatordoorname":
                                        elevator_door_name = value.ToLowerInvariant();
                                        break;
                                    case "pistonsdistance":
                                        try
                                        {
                                            float parsed_pistons_distance =
                                                float.Parse
                                                (
                                                    value,
                                                    System.Globalization.NumberStyles.Any,
                                                    System.Globalization.CultureInfo.InvariantCulture
                                                );
                                            pistons_distance = (parsed_pistons_distance < 0.0f) ? pistons_distance : parsed_pistons_distance;
                                        }
                                        catch (Exception e)
                                        {
                                            Echo($"[EXCEPTION] {e}");
                                        }
                                        break;
                                }
                            }
                        );
                        if ((elevator_name != null) || (elevator_door_name != null) || (pistons_distance != null))
                        {
                            if (elevator_name == null)
                            {
                                Echo($"[ERROR] Door \"{door.CustomName}\" does not have an elevator name assigned.");
                            }
                            else if (elevator_name == elevatorName)
                            {
                                if (elevator_door_name == null)
                                {
                                    Echo($"[ERROR] Door \"{door.CustomName}\" does not have an elevator door name assigned.");
                                }
                                else if (pistons_distance == null)
                                {
                                    Echo($"[ERROR] Door \"{door.CustomName}\" does not have pistons distance assigned.");
                                }
                                else if (elevatorDoors.ContainsKey(elevator_door_name))
                                {
                                    Echo($"[WARNING] Skipping duplicate elevator door name \"{elevator_door_name}\"...");
                                }
                                else
                                {
                                    elevatorDoors.Add(elevator_door_name, new ElevatorDoor(door, pistons_distance.Value));
                                    Echo($"[INFO] Registered door \"{door.CustomName}\" as \"{elevator_door_name}\".");
                                }
                            }
                        }
                    }
                    return false;
                }
            );
            doors.Clear();
            GridTerminalSystem.GetBlocksOfType
            (
                pistons,
                (piston) =>
                {
                    bool ret = false;
                    ParseCustomDataKeyValuePairs
                    (
                        piston,
                        (key, value) =>
                        {
                            switch (key)
                            {
                                case "elevatorname":
                                    ret = ret || (elevatorName == value.ToLowerInvariant());
                                    break;
                            }
                        }
                    );
                    if (ret)
                    {
                        Echo($"[INFO] Registered piston \"{piston.CustomName}\".");
                    }
                    return ret;
                }
            );
        }
    }
}
