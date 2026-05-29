using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace VRARTeam04.Player
{
    /// <summary>
    /// XRI 3.x 의 <see cref="CharacterControllerBodyManipulator"/> 를 상속해
    /// CharacterController.height 를 [minHeight, maxHeight] 범위로 클램프한다.
    ///
    /// 기본 매니퓰레이터는 매 이동마다 height 를 HMD(카메라) 의 y 로 덮어쓰기 때문에
    /// 사용자의 키나 헤드셋 트래킹 상태에 따라 캡슐이 인스펙터 값보다 커져
    /// 엘리베이터 천장/문틀 같은 좁은 공간에서 끼이는 문제가 생긴다.
    ///
    /// 사용법:
    /// 1) 이 파일을 빌드한 뒤 Project 창에서 우클릭 > Create > XR > Locomotion >
    ///    "Clamped Character Controller Body Manipulator (VR&AR Team 04)" 선택해 에셋 생성.
    /// 2) XR Origin 의 <see cref="XRBodyTransformer"/> 컴포넌트에서
    ///    "Constrained Body Manipulator Object" 슬롯에 1) 에서 만든 에셋을 끌어다 놓는다.
    ///    (Use Character Controller If Exists 체크는 그대로 둬도 됨 — 슬롯에 값이 있으면
    ///     XRBodyTransformer 가 기본 매니퓰레이터 대신 이쪽을 사용한다.)
    /// </summary>
    [CreateAssetMenu(
        fileName = "ClampedCharacterControllerBodyManipulator",
        menuName = "XR/Locomotion/Clamped Character Controller Body Manipulator (VR&AR Team 04)")]
    public class ClampedCharacterControllerBodyManipulator : CharacterControllerBodyManipulator
    {
        [SerializeField, Tooltip("CharacterController.height 의 최소값(m). 키 작은 사용자가 너무 낮아져 바닥에 묻히는 것을 방지.")]
        private float _minHeight = 1.2f;

        [SerializeField, Tooltip("CharacterController.height 의 최대값(m). 엘리베이터 천장 등 낮은 공간에 캡슐이 박히지 않도록 제한.")]
        private float _maxHeight = 1.8f;

        [SerializeField, Tooltip("켜면 캡슐의 XZ 중심을 HMD 위치가 아니라 XR Origin 의 (0,0) 로 고정. 머리를 숙여도 캡슐이 문틀 밖으로 튀어나가지 않게 된다.")]
        private bool _lockCenterToOrigin = false;

        [SerializeField, Tooltip("캡슐 바닥을 origin.local.y=0 에 강제로 붙임. " +
            "(원본 매니퓰레이터는 center.y 가 height/2 + skinWidth 가 되어 origin.y 가 0이 아니면 발이 떠 보임. " +
            "켜두면 bodyGroundPosition.y 와 무관하게 항상 origin 의 발 위치에 캡슐 바닥이 붙는다.)")]
        private bool _snapBottomToOrigin = true;

        [SerializeField, Tooltip("캡슐 바닥의 y 추가 보정값(m). 양수면 위로, 음수면 아래로. " +
            "씬에서 발이 살짝 떠 있거나 묻혀 있으면 ±0.05 정도 조정.")]
        private float _bottomYOffset = 0f;

        [SerializeField, Tooltip("켜면 매 MoveBody 마다 height/center/origin world Y 등을 콘솔에 찍어 디버깅. " +
            "정상 동작 확인 후엔 꺼둘 것.")]
        private bool _debugLog = false;

        /// <inheritdoc/>
        public override CollisionFlags MoveBody(Vector3 motion)
        {
            if (linkedBody == null || characterController == null)
                return CollisionFlags.None;

            var bodyGroundPosition = linkedBody.GetBodyGroundLocalPosition();

            // 기본 동작: height = 카메라 y - 바닥 y
            float rawHeight = linkedBody.xrOrigin.CameraInOriginSpaceHeight - bodyGroundPosition.y;
            float capsuleHeight = Mathf.Clamp(rawHeight, _minHeight, _maxHeight);

            characterController.height = capsuleHeight;

            float centerX = _lockCenterToOrigin ? 0f : bodyGroundPosition.x;
            float centerZ = _lockCenterToOrigin ? 0f : bodyGroundPosition.z;

            // 캡슐 바닥 y 결정
            //  - snap = true  : bodyGround.y 를 무시하고 origin local y=0 + bottomYOffset 에 바닥
            //  - snap = false : 원래 공식(bodyGround.y + skinWidth)
            float bottomLocalY = _snapBottomToOrigin
                ? _bottomYOffset
                : bodyGroundPosition.y + characterController.skinWidth + _bottomYOffset;

            characterController.center = new Vector3(
                centerX,
                bottomLocalY + capsuleHeight * 0.5f,
                centerZ);

            if (_debugLog)
            {
                Debug.Log(
                    $"[Clamped CC] rawH={rawHeight:F3} clampedH={capsuleHeight:F3} " +
                    $"bodyGroundLocalY={bodyGroundPosition.y:F3} bottomLocalY={bottomLocalY:F3} " +
                    $"originWorldY={linkedBody.originTransform.position.y:F3} " +
                    $"capsuleBottomWorldY={(linkedBody.originTransform.TransformPoint(new Vector3(0, bottomLocalY, 0)).y):F3}",
                    this);
            }

            // 비활성 컨트롤러에 Move 호출하면 에러가 나므로 가드
            if (characterController.enabled)
                return characterController.Move(motion);

            // 베이스 클래스와 동일한 fallback: 그냥 Origin transform 을 직접 옮긴다.
            linkedBody.originTransform.position += motion;
            return CollisionFlags.None;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_minHeight < 0.1f) _minHeight = 0.1f;
            if (_maxHeight < _minHeight) _maxHeight = _minHeight;
        }
#endif
    }
}
