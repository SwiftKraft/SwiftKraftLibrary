using SwiftKraft.Gameplay.Common.FPS;
using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace SwiftKraft.Gameplay.NPCs
{
    [RequireComponent(typeof(MotorBase))]
    public class NPCNavigator : NPCModuleBase
    {
        public MotorBase Motor { get; private set; }

        public float WaypointRadius = 0.25f;
        public Timer RepathTimer;

        public int CurrentWaypointIndex
        {
            get => _currentWaypointIndex;
            private set => _currentWaypointIndex = Mathf.Clamp(value, 0, Waypoints.Length - 1);
        }
        int _currentWaypointIndex;

        public Vector3 Destination
        {
            get => _destination;
            set
            {
                if (_destination == value)
                    return;

                _destination = value;
                if (!CheckStop() && RepathTimer.Ended)
                {
                    Repath();
                    RepathTimer.Reset();
                }
            }
        }
        Vector3 _destination;

        public Vector3 CurrentWaypoint => Waypoints.Length <= 0 ? Destination : Waypoints[CurrentWaypointIndex];

        public bool Stopped { get; set; }

        protected Vector3[] Waypoints => Path?.corners;

        protected NavMeshPath Path { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponent<MotorBase>();
            Destination = transform.position;
            Path = new();
        }

        protected virtual void FixedUpdate()
        {
            if (Stopped)
            {
                Motor.WishMoveDirection = Vector3.zero;
                return;
            }

            RepathTimer.Tick(Time.fixedDeltaTime);
            Motor.WishMovePosition = CurrentWaypoint;

            if (Vector3.Distance(transform.position, CurrentWaypoint) <= WaypointRadius)
            {
                CurrentWaypointIndex++;
                CheckStop();
            }

            if (RepathTimer.Ended)
            {
                RepathTimer.Reset();
                Repath();
            }
        }

        protected virtual bool CheckStop()
        {
            Stopped = Vector3.Distance(_destination, transform.position) <= WaypointRadius;
            return Stopped;
        }

        public void Repath()
        {
            Stopped = !NavMesh.CalculatePath(transform.position, Destination, NavMesh.AllAreas, Path);
            CurrentWaypointIndex = 0;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (Waypoints == null || Waypoints.Length <= 0)
                return;

            Gizmos.color = Stopped ? Color.red : Color.green;

            for (int i = 0; i < Waypoints.Length; i++)
            {
                if (i < Waypoints.Length - 1)
                    Gizmos.DrawLine(Waypoints[i], Waypoints[i + 1]);
                Gizmos.DrawWireSphere(Waypoints[i], 0.25f);
            }
        }

#endif
    }
}
