using System;
using UnityEngine;

namespace Primer.Graph
{
    [Serializable]
    public class LogarithmicYEquation : ParametricEquation
    {
        public float offset = 1;
        public float _base = 2;
        public Vector3 start = Vector3.zero;
        public Vector3 end = new(10, 10, 0);

        public override Vector3 Evaluate(float t) {
            var point = Vector3.Lerp(start, end, t);
            point.y = Mathf.Lerp(start.y, end.y, Mathf.Log(t + offset, _base));
            return point;
        }
    }
}