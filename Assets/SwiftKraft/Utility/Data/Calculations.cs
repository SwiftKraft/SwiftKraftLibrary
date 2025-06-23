using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SwiftKraft.Utils
{
    public static class Calculations
    {
        public static float AccelerationDistance(float initialVelocity, float finalVelocity, float acceleration) => (finalVelocity * finalVelocity - initialVelocity * initialVelocity) / (2 * acceleration);
    }
}
