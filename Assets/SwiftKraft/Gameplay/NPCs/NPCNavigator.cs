using SwiftKraft.Gameplay.Motors;
using SwiftKraft.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace SwiftKraft.Gameplay.NPCs
{
    [RequireComponent(typeof(MotorBase))]
    [DisallowMultipleComponent]
    public class NPCNavigator : NPCModuleBase
    {
        public override string ID => "Essentials.Navigator";
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

        [field: SerializeField]
        public bool LookAtWaypoint { get; set; }
        [field: SerializeField]
        public float MinLookWaypointDistance { get; set; } = 1f;

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

        public override void Tick()
        {
            base.Tick();

            if (Stopped)
            {
                Motor.WishMoveDirection = Vector3.zero;
                return;
            }

            Motor.WishMovePosition = CurrentWaypoint;
            RepathTimer.Tick(Time.fixedDeltaTime);

            if (LookAtWaypoint)
            {
                Vector3 lookPos = CurrentWaypoint + Motor.LookPointHeight * Vector3.up;
                if ((lookPos - Motor.LookPoint.position).sqrMagnitude > MinLookWaypointDistance * MinLookWaypointDistance)
                    Motor.WishLookPosition = lookPos;
            }

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
            Stopped = !NavMesh.CalculatePath(NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas) ? hit.position : transform.position, Destination, NavMesh.AllAreas, Path);
            CurrentWaypointIndex = 0;
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
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
