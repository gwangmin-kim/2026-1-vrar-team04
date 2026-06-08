using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    public static SceneLoadManager Instance { get; private set; }

    // 다음 씬에서 LobbyModeController가 참조할 데이터 백업 저장소
    public LobbyModeController.LobbyMode? NextMode { get; set; }
    public bool HasQueuedPose { get; set; }
    public Vector3 QueuedLocalPosition { get; set; }
    public Quaternion QueuedLocalRotation { get; set; }
    public Quaternion QueuedLocalCameraRotation { get; set; }

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 백그라운드에서 씬을 미리 90% 로드해두고 대기하다가 전환하여 VR 버벅임을 최소화합니다.
    /// </summary>
    public void LoadSceneSeamless(string sceneName)
    {
        if (IsLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        IsLoading = true;

        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneName);
        // 90% 완료 시 메인 스레드가 씬 활성화를 위해 순간 멈추는 현상을 백그라운드 단계에서 대기하도록 차단
        ao.allowSceneActivation = false;

        while (ao.progress < 0.9f)
        {
            yield return null;
        }

        // 💡 [참고] 엘리베이터 문이 완전히 닫히거나 카메라 앞의 3D 암전 가리개(Fade Out) 처리가
        // 완벽히 끝날 때까지 대기하고 싶다면 이 부분에 추가 대기 시간 조절용 코드를 넣을 수 있습니다.
        // yield return new WaitForSeconds(0.3f);

        // 대기가 끝나면 활성화 플래그를 켜서 순간 전환 유도
        ao.allowSceneActivation = true;

        while (!ao.isDone)
        {
            yield return null;
        }

        IsLoading = false;
    }

}
