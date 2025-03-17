
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Movement
{
    [CreateAssetMenu(menuName = "Movement State/Air")]
    public class PlayerMovementAir : PlayerMovementBasic
    {
        public static event Action onLand;
        public static event Action onWallKick;
        public static event Action onDoubleJump;

        public PlayerMovementGround GroundState;
        public PlayerMovementDash Dash;
        public PlayerMovementWallrun WallrunState;

        public int WallKickRaycastIterations = 32;
        public int WallKickMax = 3;

        public float RequiredAirTimeForLandingSound = 1f;
        public float WallKickVerticalSpeed = 16f;
        public float WallKickHorizontalSpeed = 9f;
        public float WallKickDistance = 1f;
        public float DoubleJumpSpeedHorizontal = 5f;
        public float CoyoteTime = 0.3f;

        float currentCoyote;
        float coyoteJumpSpeed;
        float doubleJumpBuffer;
        float airTime;
        int wallKickCount;
        bool doubleJump;

        public override void StateStarted(PlayerMotor parent)
        {
            if (parent.IsGrounded)
            {
                wallKickCount = 0;
                doubleJump = false;
                parent.CurrentState = GroundState;
                return;
            }

            airTime = 0f;
            currentCoyote = 0f;
            doubleJumpBuffer = 1.0f;

            

            if (parent.RecentJumpTimer.Ended)
                ApplyCoyote();

            base.StateStarted(parent);
        }

        public override void StateEnded(PlayerMotor parent)
        {
          

            base.StateEnded(parent);
        }

      

        public override void GroundedChanged(PlayerMotor parent, bool value, bool prev)
        {
            base.GroundedChanged(parent, value, prev);

            if (value)
            {
                wallKickCount = 0;
                doubleJump = false;

                if (airTime > RequiredAirTimeForLandingSound)
                    parent.PlayMotorSound(0);

                onLand?.Invoke();
                parent.CurrentState = GroundState;
            }
        }

        public override void InputUpdate(PlayerMotor parent)
        {
            base.InputUpdate(parent);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                doubleJumpBuffer = JumpBuffer;
                
            }

        }

        public override void TickUpdate(PlayerMotor parent)
        {
            parent.Rigidbody.AddForce(Vector3.up * Gravity);

            Movement(parent, Speed, ControlThreshold);

            airTime += Time.fixedDeltaTime;

            if (currentCoyote > 0f)
                currentCoyote -= Time.fixedDeltaTime;
            else if (currentCoyote < 0f)
                currentCoyote = 0f;

            if (CurrentJumpBuffer > 0f)
            {
                if (currentCoyote > 0f)
                {
                    TryJump(parent, coyoteJumpSpeed);
                    currentCoyote = 0f;
                    doubleJumpBuffer = 0f;
                }
                else
                    TryWallKick(parent);

                CurrentJumpBuffer -= Time.fixedDeltaTime;
            }
            else if (CurrentJumpBuffer < 0f)
                CurrentJumpBuffer = 0f;

            if (doubleJumpBuffer > 0f)
            {
                doubleJumpBuffer -= Time.fixedDeltaTime;
                if (currentCoyote <= 0f)
                    TryDoubleJump(parent);
            }
            else if (doubleJumpBuffer < 0f)
                doubleJumpBuffer = 0f;

            if (WallrunState.GetWall(parent, out _, out _))
            {
                wallKickCount = 0;
                doubleJump = false;
                parent.CurrentState = WallrunState;
            }
        }


        public override void ReceiveData<T>(T obj)
        {
            if (obj is float f)
                coyoteJumpSpeed = f;
        }

        public void ApplyCoyote() => currentCoyote = CoyoteTime;

        public override void TryJump(PlayerMotor parent, float speed = -1f)
        {
            Debug.Log("Jumped");
            base.TryJump(parent);
            parent.PlayMotorSound(1);
        }


        public void TryDoubleJump(PlayerMotor parent)
        {
            if (doubleJump)
                return;

            parent.PlayMotorSound(2);

            onDoubleJump?.Invoke();

            doubleJumpBuffer = 0f;
            doubleJump = true;
            SetGravity(parent, JumpSpeed);
            parent.Rigidbody.AddForce(DoubleJumpSpeedHorizontal * parent.GetWishDir(), ForceMode.VelocityChange);
        }


        public bool TryWallKick(PlayerMotor parent)
        {
            if (wallKickCount >= WallKickMax)
                return false;

            CurrentJumpBuffer = 0f;

            Dictionary<Collider, RaycastHit> blacklist = new();
            for (int i = 0; i < WallKickRaycastIterations; i++)
                if (Physics.Raycast(parent.transform.position + Vector3.up * 1.4f, GetWallKickDirectionRaycast(parent, 360f / WallKickRaycastIterations * i), out RaycastHit hit, WallKickDistance, parent.GroundLayers, QueryTriggerInteraction.Ignore) && !blacklist.ContainsKey(hit.collider))
                    blacklist.Add(hit.collider, hit);

            if (blacklist.Count == 0)
                return false;

            doubleJumpBuffer = 0f;
            doubleJump = false;

            wallKickCount++;

            parent.PlayMotorSound(1);

            onWallKick?.Invoke();

            foreach (RaycastHit hit in blacklist.Values)
                parent.Rigidbody.AddForce(hit.normal * WallKickHorizontalSpeed, ForceMode.VelocityChange);

            SetGravity(parent, WallKickVerticalSpeed);

            return true;
        }

        private Vector3 GetWallKickDirectionRaycast(PlayerMotor parent, float degree)
        {
            Quaternion rot = Quaternion.Euler(0f, degree, 0f);
            return rot * parent.transform.forward;
        }
    }
}
