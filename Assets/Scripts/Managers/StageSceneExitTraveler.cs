using UnityEngine;

[AddComponentMenu("Horror/Stage Scene Exit Traveler (VR&AR Team 04)")]
public class StageSceneExitTraveler : MonoBehaviour
{
    [Header("Target Configurations")]
    [SerializeField] private string _lobbySceneName = "LobbyMap";

    /// <summary>
    /// 플레이어의 엘리베이터 내 오프셋 좌표를 전역 매니저에 백업(큐잉)합니다.
    /// </summary>
    public void QueuePlayerPose(Transform sourceElevator, Transform playerRoot)
    {
        if (sourceElevator == null || playerRoot == null || SceneLoadManager.Instance == null)
        {
            Debug.LogWarning("[StageSceneExitTraveler] 필수 컴포넌트 참조가 유실되어 포즈 백업을 건너뜁니다.");
            return;
        }

        var slm = SceneLoadManager.Instance;

        // 플레이어 카메라 가 존재하면 카메라 회전 기준으로, 없으면 플레이어 루트 기준으로 상대 좌표 연산
        Transform cameraTransform = GetPlayerCameraTransform(playerRoot);
        Quaternion cameraRotation = cameraTransform != null ? cameraTransform.rotation : playerRoot.rotation;

        // 로컬 좌표 및 회전 오프셋 연산 후 전역 매니저에 저장
        slm.QueuedLocalPosition = sourceElevator.InverseTransformPoint(playerRoot.position);
        slm.QueuedLocalRotation = Quaternion.Inverse(sourceElevator.rotation) * playerRoot.rotation;
        slm.QueuedLocalCameraRotation = Quaternion.Inverse(sourceElevator.rotation) * cameraRotation;
        slm.HasQueuedPose = true;
    }

    /// <summary>
    /// 로비를 게임플레이 모드로 설정하여 비동기 심리스 로드를 시작합니다.
    /// </summary>
    public void LoadLobbyAsGameplayFloor()
    {
        if (SceneLoadManager.Instance == null) return;

        SceneLoadManager.Instance.NextMode = LobbyModeController.LobbyMode.GameplayFloor;
        SceneLoadManager.Instance.LoadSceneSeamless(_lobbySceneName);
    }

    /// <summary>
    /// 로비를 메인 메뉴 모드로 설정하여 비동기 심리스 로드를 시작합니다. (재시작 등)
    /// </summary>
    public void LoadLobbyAsMainMenu()
    {
        if (SceneLoadManager.Instance == null) return;

        SceneLoadManager.Instance.NextMode = LobbyModeController.LobbyMode.MainMenu;
        SceneLoadManager.Instance.LoadSceneSeamless(_lobbySceneName);
    }

    // 내부 편의용 카메라 탐색 헬퍼 함수
    private Transform GetPlayerCameraTransform(Transform playerRoot)
    {
        if (playerRoot == null) return null;
        Camera playerCamera = playerRoot.GetComponentInChildren<Camera>(true);
        return playerCamera != null ? playerCamera.transform : null;
    }
}
