// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using System;
    using UnityEngine;
    using Coherence.Common;

    public static class InterpolationUtils
    {
        public static float ClampFloat(float value)
        {
            if (float.IsPositiveInfinity(value))
            {
                return float.MaxValue;
            }

            if (float.IsNegativeInfinity(value))
            {
                return float.MinValue;
            }

            return value;
        }

        // Inspired by https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
        public static Quaternion SmoothDampQuaternion(Quaternion current, Quaternion target, ref Vector4d currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            if (smoothTime <= 0)
            {
                return target;
            }

            if (maxSpeed <= 0)
            {
                maxSpeed = Mathf.Infinity;
            }

            // Edge cases
            switch (current.IsValid(), target.IsValid())
            {
                case (false, false): return Quaternion.identity;
                case (true, false): return current;
                case (false, true): return target;
            }

            if (deltaTime == 0) // Prevents SmoothDampAngle from producing NaN
            {
                return current;
            }

            // clamp to maxSpeed
            var change = current * Quaternion.Inverse(target);
            change.ToAngleAxisShortest(out var changeAngle, out var changeAxis);

            // if change is fairly small, changeAngle will be zero but it should still be smoothed
            // but clamping the change isn't fasible or important at that point
            if (changeAngle > Mathf.Epsilon)
            {
                var maxChange = maxSpeed * smoothTime;
                changeAngle = Mathf.Clamp(changeAngle, -maxChange, maxChange);
                change = Quaternion.AngleAxis(changeAngle, changeAxis);
                target = Quaternion.Inverse(change) * current;
            }

            // account for double-cover
            var dot = Quaternion.Dot(current, target);
            var multi = dot > 0f ? 1f : -1f;
            target.x *= multi;
            target.y *= multi;
            target.z *= multi;
            target.w *= multi;

            var result = new Vector4d(
                SmoothMixDouble(current.x, target.x, ref currentVelocity.x, smoothTime, Mathf.Infinity, deltaTime),
                SmoothMixDouble(current.y, target.y, ref currentVelocity.y, smoothTime, Mathf.Infinity, deltaTime),
                SmoothMixDouble(current.z, target.z, ref currentVelocity.z, smoothTime, Mathf.Infinity, deltaTime),
                SmoothMixDouble(current.w, target.w, ref currentVelocity.w, smoothTime, Mathf.Infinity, deltaTime)
            ).normalized;

            // ensure velocity is tangent
            var velocityError = Vector4d.Project(new Vector4d(currentVelocity.x, currentVelocity.y, currentVelocity.z, currentVelocity.w), result);
            currentVelocity.x -= velocityError.x;
            currentVelocity.y -= velocityError.y;
            currentVelocity.z -= velocityError.z;
            currentVelocity.w -= velocityError.w;

            return new Quaternion((float)result.x, (float)result.y, (float)result.z, (float)result.w);
        }

        public static double SmoothMixDouble(double current, double target, ref double velocity, float smoothTime, double maxSpeed, float deltaTime)
        {
            if (smoothTime <= 0)
            {
                return target;
            }

            if (maxSpeed <= 0)
            {
                maxSpeed = Mathf.Infinity;
            }

            var next = SmoothDampDouble(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);
            if (!double.IsNaN(next) && !double.IsInfinity(next))
            {
                return next;
            }

            next = PoorMansLerpDouble(current, target, ref velocity, smoothTime, maxSpeed, deltaTime);

            return next;
        }

        public static double SmoothDampDouble(double current, double target, ref double velocity, float smoothTime, double maxSpeed, float deltaTime)
        {
            // Based on Game Programming Gems 4 Chapter 1.10

            smoothTime = Mathf.Max(0.0001F, smoothTime);
            var omega = 2F / smoothTime;

            var x = omega * deltaTime;
            var exp = 1F / (1F + x + 0.48F * x * x + 0.235F * x * x * x);
            var change = current - target;
            var originalTo = target;

            // Clamp maximum speed
            var maxChange = maxSpeed * smoothTime;
            change = ClampDouble(change, -maxChange, maxChange);
            target = current - change;
            if (double.IsPositiveInfinity(target)) target = double.MaxValue;
            if (double.IsNegativeInfinity(target)) target = double.MinValue;

            var temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            var output = target + (change + temp) * exp;

            // Prevent overshooting
            if (!double.IsNaN(output) && originalTo - current > 0.0F == output > originalTo)
            {
                output = originalTo;
                velocity = (output - originalTo) / deltaTime;
            }

            return output;
        }

        public static double PoorMansLerpDouble(double current, double target, ref double velocity, float smoothTime, double maxSpeed, float deltaTime)
        {
            if (deltaTime == 0)
            {
                return current;
            }

            var dt = deltaTime / smoothTime;

            double next;
            if (dt >= 1)
            {
                next = target;
            }
            else
            {
                next = current * (1 - dt) + target * dt;
            }

            velocity = (next - current) / deltaTime;
            if (double.IsPositiveInfinity(velocity)) velocity = double.MaxValue;
            if (double.IsNegativeInfinity(velocity)) velocity = double.MinValue;

            if (Math.Abs(velocity) > maxSpeed)
            {
                velocity = maxSpeed * Math.Sign(velocity);
                return current + maxSpeed * Math.Sign(velocity) * deltaTime;
            }

            return next;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }

        private static bool IsValid(this Quaternion q)
        {
            return !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w));
        }

        public static void ToAngleAxisShortest(this Quaternion q, out float angle, out Vector3 axis)
        {
            q.ToAngleAxis(out angle, out axis);

            if (angle >= 180)
            {
                angle = 360 - angle;
                axis *= -1;
            }
        }
    }
}
