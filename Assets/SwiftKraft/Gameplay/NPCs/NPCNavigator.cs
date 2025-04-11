using SwiftKraft.Gameplay.Motors;
using UnityEngine;

namespace SwiftKraft.Gameplay.NPCs
{
    [RequireComponent(typeof(MotorBase))]
    public class NPCNavigator : NPCModuleBase
    {
        public MotorBase Motor { get; private set; }

        public float WaypointRadius = 0.25f;

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
                Stopped = Vector3.Distance(_destination, transform.position) <= WaypointRadius;
            }
        }
        Vector3 _destination;

        public Vector3 CurrentWaypoint => Waypoints[CurrentWaypointIndex];

        public bool Stopped { get; set; }

        protected Vector3[] Waypoints;

        protected override void Awake()
        {
            base.Awake();
            Motor = GetComponent<MotorBase>();
        }

        protected virtual void FixedUpdate()
        {
            if (Stopped)
            {
                Motor.WishMoveDirection = Vector3.zero;
                return;
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (Waypoints.Length <= 0)
                return;

            Gizmos.color = Color.green;

            foreach (Vector3 position in Waypoints)
                Gizmos.DrawWireSphere(position, 0.25f);

            Gizmos.DrawLineList(Waypoints);
        }

#endif
    }
}
