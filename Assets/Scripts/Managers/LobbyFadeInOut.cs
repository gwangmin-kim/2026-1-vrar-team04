using System;
using DG.Tweening;
using UnityEngine;

public class LobbyFadeInOut : MonoBehaviour
{
    [Header("VR Fade Setup")]
    [SerializeField] private MeshRenderer _fadeRenderer; // 카메라 앞 반구형 가리개의 MeshRenderer
    private Material _fadeMaterial;
    private int _baseColorPropId;

    private void Awake()
    {
        if (_fadeRenderer != null)
        {
            _fadeMaterial = _fadeRenderer.material;
            // URP 기본 셰이더의 컬러 및 알파 제어용 프로퍼티 ID
            _baseColorPropId = Shader.PropertyToID("_BaseColor");
        }
    }

    public void SetDark()
    {
        // 혹시 비활성화되어 있다면 켜기
        _fadeRenderer.gameObject.SetActive(true);

        // 현재 머티리얼 컬러 가져오기
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // 알파값 1f로 변경
        currentColor.a = 1f;
        _fadeMaterial.SetColor(_baseColorPropId, currentColor);
    }

    public void SetBright()
    {
        // 현재 머티리얼 컬러 가져오기
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // 알파값 0f로 변경
        currentColor.a = 0f;
        _fadeMaterial.SetColor(_baseColorPropId, currentColor);

        // 드로우콜 낭비를 방지하기 위해 가리개 끄기
        _fadeRenderer.gameObject.SetActive(false);
    }

    /// <summary>
    /// 화면을 검게 암전시키는 함수 (Fade Out)
    /// </summary>
    /// <param name="duration">걸리는 시간</param>
    /// <param name="onComplete">완료 후 실행할 함수</param>
    public void FadeOut(float duration, Action onComplete = null)
    {
        if (_fadeMaterial == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 혹시 비활성화되어 있다면 켜기
        _fadeRenderer.gameObject.SetActive(true);

        // 현재 머티리얼 컬러 가져오기
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // DOTween으로 알파값만 1f(검은색 100%)로 보간
        DOTween.To(() => currentColor.a, x =>
        {
            currentColor.a = x;
            _fadeMaterial.SetColor(_baseColorPropId, currentColor);
        }, 1f, duration)
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            // 완료 시 수행할 동작 실행
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 투명해지면서 화면을 다시 보여주는 함수 (Fade In)
    /// </summary>
    /// <param name="duration">걸리는 시간</param>
    /// <param name="onComplete">완료 후 실행할 함수</param>
    public void FadeIn(float duration, Action onComplete = null)
    {
        if (_fadeMaterial == null)
        {
            onComplete?.Invoke();
            return;
        }

        _fadeRenderer.gameObject.SetActive(true);
        Color currentColor = _fadeMaterial.GetColor(_baseColorPropId);

        // DOTween으로 알파값만 0f(완전 투명)로 보간
        DOTween.To(() => currentColor.a, x =>
        {
            currentColor.a = x;
            _fadeMaterial.SetColor(_baseColorPropId, currentColor);
        }, 0f, duration)
        .SetEase(Ease.InQuad)
        .OnComplete(() =>
        {
            // 완전 투명해지면 드로우콜 낭비를 방지하기 위해 가리개 끄기
            _fadeRenderer.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }
}
