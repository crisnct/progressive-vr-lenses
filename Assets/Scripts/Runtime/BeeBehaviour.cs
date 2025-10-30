using UnityEngine;

namespace ProgressiveVrLenses.Runtime
{
    /// <summary>
    /// Simple behaviour that animates a bee around a point with gentle swarming motion.
    /// </summary>
    public class BeeBehaviour : MonoBehaviour
    {
        private Vector3 _center;
        private float _radius;
        private float _heightOffset;
        private float _speed;
        private Vector2 _phase;

        /// <summary>
        /// Configure the bee orbit parameters. Called automatically by the scene builder.
        /// </summary>
        public void Initialize(Vector3 center, float radius, float heightOffset, float speed, Vector2 phase)
        {
            _center = center;
            _radius = radius;
            _heightOffset = heightOffset;
            _speed = speed;
            _phase = phase;
        }

        private void Update()
        {
            var time = Time.time * _speed + _phase.x;
            var orbit = new Vector3(Mathf.Cos(time), 0f, Mathf.Sin(time)) * _radius;
            var vertical = Mathf.Sin(Time.time * (_speed * 0.75f) + _phase.y) * 0.35f;

            transform.position = _center + orbit + new Vector3(0f, _heightOffset + vertical, 0f);
            transform.Rotate(Vector3.up, _speed * 120f * Time.deltaTime, Space.World);

            var wobble = Mathf.Sin(Time.time * 5.5f + _phase.y) * 8f;
            transform.localRotation = Quaternion.Euler(0f, wobble, 0f);
        }
    }
}
