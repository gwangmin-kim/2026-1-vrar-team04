using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DeathCutsceneManager : MonoBehaviour
{
    [Header("Player & Camera")]
    [SerializeField] private Transform _xrOrigin;
    [SerializeField] private Transform _cameraOffset;

    [Header("Ghost Reference")]
    [SerializeField] private Transform _ghostTransform; // 그림자 유령 Transform
    [SerializeField] private Animator _ghostAnimator; // 유령 애니메이터
    [SerializeField] private string _attackStartTriggerName = "AttackStart";
    [SerializeField] private string _attackTriggerName = "Attack";

    [Header("Cutscene Settings")]
    [SerializeField] private float _turnDuration = 0.15f; // 유령 쪽으로 시선이 휙 돌아가는 시간
    [SerializeField] private float _attackDelay = 0.2f; // 유령 공격까지의 대기 시간
    [SerializeField] private float _fadeOutDuration = 0.5f; // 암전 완료까지 걸리는 시간
    [SerializeField] private float _fadeInDuration = 1.2f; // 다시 밝아지기까지 걸리는 시간

    private bool _isDying = false;

    private void Start()
    {
        // 자동 할당
        if (_xrOrigin == null)
        {
            _xrOrigin = GameManager.Instance.player;
        }
        if (_cameraOffset == null)
        {
            _cameraOffset = _xrOrigin.Find("CameraOffset");
        }
    }

    public void StartDeathCutscene()
    {
        if (_isDying) return;
        _isDying = true;

        // TODO: 플레이어 입력 및 조작 물리 시스템 완전히 차단
        GameManager.Instance.SetPlayerControllable(false);

        // 유령의 걷기 애니메이션 중지, 공격 전 동작 상태로 변경
        if (_ghostAnimator != null)
        {
            _ghostAnimator.SetTrigger(_attackStartTriggerName);
        }

        Sequence deathSequence = DOTween.Sequence();

        // 강제로 유령 쪽을 바라보게 함
        Vector3 directionToGhost = (_ghostTransform.position - _xrOrigin.position).normalized;
        directionToGhost.y = 0; // 평면상의 회전만 계산
        Quaternion targetRotation = Quaternion.LookRotation(directionToGhost);

        deathSequence.Append(_xrOrigin.DORotateQuaternion(targetRotation, _turnDuration).SetEase(Ease.OutBounce));

        // 유령 공격 애니메이션 재생
        deathSequence.AppendInterval(_attackDelay);
        deathSequence.AppendCallback(() =>
        {
            if (_ghostAnimator != null)
            {
                _ghostAnimator.SetTrigger(_attackTriggerName);
            }
        });

        // 공격 타이밍에 맞춰 화면 암전
        deathSequence.AppendInterval(1f); // 공격 모션이 시작되고 타격감 직전에 암전 시작
        deathSequence.AppendCallback(() =>
        {
            // FadeOut 함수에 완료 후 처리할 Action()으로 Scene 리로드를 넘겨줍니다.
            GameManager.Instance.FadeOut(_fadeOutDuration, () =>
            {
                // 스테이지 재시작
                GameManager.Instance.RestartStage();

                // 화면이 다시 밝아진 뒤 조작 허용
                GameManager.Instance.FadeIn(_fadeInDuration, () =>
                {
                    GameManager.Instance.SetPlayerControllable(true);
                });
            });
        });
    }
}
