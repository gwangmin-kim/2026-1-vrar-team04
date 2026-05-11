using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace VRARTeam04.Player
{
    /// <summary>
    /// DynamicMoveProvider 를 상속해 스틱 입력을 SmoothDamp 로 보간한다.
    /// - 스틱을 누른 순간 풀-스피드까지 부드럽게 가속 (accelTime)
    /// - 스틱을 놓아도 일정 시간 동안 감속하며 멈춤 (decelTime)
    /// 기존 ContinuousMoveProvider / DynamicMoveProvider 컴포넌트를 이 컴포넌트로 교체해서 쓰면 된다.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Smooth Dynamic Move Provider (VR&AR Team 04)")]
    public class SmoothDynamicMoveProvider : DynamicMoveProvider
    {
        [Space, Header("Input Smoothing")]
        [SerializeField, Tooltip("스틱을 누른 직후 풀 속도에 도달하기까지 걸리는 시간(초).")]
        private float _accelTime = 0.18f;

        [SerializeField, Tooltip("스틱을 놓은 직후 완전히 멈출 때까지 걸리는 시간(초).")]
        private float _decelTime = 0.22f;

        [SerializeField, Tooltip("이 값보다 작은 입력 크기는 0 으로 간주 (스틱 데드존).")]
        [Range(0f, 0.5f)]
        private float _inputDeadzone = 0.08f;

        private Vector2 _smoothedInput;
        private Vector2 _smoothVelocity;

        /// <summary>
        /// ContinuousMoveProvider.ComputeDesiredMove 가 호출될 때, 원본 input 대신 보간된 input 을 base 에 넘긴다.
        /// 이렇게 하면 전진 방향 계산(DynamicMoveProvider 의 head/hand 기준)과 이동량 계산이 모두 보간된 값으로 처리된다.
        /// </summary>
        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            // 데드존: 작은 노이즈는 0 으로
            if (input.sqrMagnitude < _inputDeadzone * _inputDeadzone)
                input = Vector2.zero;

            // 가속 중이면 accelTime, 감속 중이면 decelTime 사용
            bool accelerating = input.sqrMagnitude > _smoothedInput.sqrMagnitude;
            float smoothTime = accelerating ? _accelTime : _decelTime;

            _smoothedInput = Vector2.SmoothDamp(
                _smoothedInput,
                input,
                ref _smoothVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime);

            // 완전히 작아지면 스냅 0
            if (_smoothedInput.sqrMagnitude < 1e-5f)
            {
                _smoothedInput = Vector2.zero;
                _smoothVelocity = Vector2.zero;
            }

            return base.ComputeDesiredMove(_smoothedInput);
        }

        /// <summary>
        /// 외부 (예: HeadBob, 발소리) 에서 현재 보간된 입력 크기를 읽고 싶을 때 사용.
        /// 0 = 정지, 1 = 풀 속도.
        /// </summary>
        public float CurrentInputMagnitude => Mathf.Clamp01(_smoothedInput.magnitude);
    }
}
