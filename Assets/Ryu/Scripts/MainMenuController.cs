using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 메인 메뉴 컨트롤러. 게임 시작·게임 종료 후 *같은 씬*을 재활용해서 메인 화면으로 사용.
///
/// 책임:
/// - 시작 버튼 → 게임 플레이 씬 로드
/// - 설정 버튼 → 설정 패널 열기/닫기
/// - 종료 버튼 → 애플리케이션 종료 (에디터에선 Play Mode 종료)
///
/// 셋업:
/// 1) MainMenu.unity 씬 만들기. XR Origin 1 개만 두고 나머진 비움.
/// 2) World Space Canvas 1개에 *메인 패널*, *설정 패널* 두 자식 GameObject 로 분리.
/// 3) 빈 GameObject "MenuController" 만들고 이 스크립트 추가.
/// 4) 인스펙터에서 _mainPanel, _settingsPanel 각각 연결.
/// 5) 각 버튼의 OnClick 이벤트에 이 컨트롤러의 메서드 연결:
///    - 시작 버튼   → StartGame()
///    - 설정 버튼   → OpenSettings()
///    - 종료 버튼   → QuitGame()
///    - 설정 패널의 닫기 버튼 → CloseSettings()
/// </summary>
[AddComponentMenu("Horror/Main Menu Controller (VR&AR Team 04)")]
public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField, Tooltip("시작 버튼을 누르면 로드할 게임 플레이 씬 이름. Build Settings 에 등록돼있어야 함.")]
    private string _gameplaySceneName = "Map";

    [Header("Panels")]
    [SerializeField, Tooltip("메인 메뉴 패널 (시작/설정/종료 버튼들). 비워두면 토글 X.")]
    private GameObject _mainPanel;

    [SerializeField, Tooltip("설정 패널 (Comfort 옵션 등). 시작 시 비활성. 비워두면 OpenSettings 동작 안 함.")]
    private GameObject _settingsPanel;

    [Header("Audio (Optional)")]
    [SerializeField, Tooltip("버튼 선택 시 클릭 사운드.")]
    private OneShotPlayer _selectSound;

    [SerializeField, Tooltip("설정 패널 등에서 뒤로 가기 시 사운드.")]
    private OneShotPlayer _backSound;

    [Header("Events")]
    [Tooltip("StartGame 호출 시 — 씬 로드 *직전*. 페이드 아웃 등 연결 가능.")]
    public UnityEvent OnBeforeStartGame;

    [Tooltip("QuitGame 호출 시 — 종료 직전. 저장 등 연결 가능.")]
    public UnityEvent OnBeforeQuit;

    private void Start()
    {
        EnsureSettingsController();

        // 초기 상태: 메인 패널 활성, 설정 패널 비활성
        if (_mainPanel != null) _mainPanel.SetActive(true);
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    // ─── 버튼 핸들러 (Button.OnClick 에 연결) ─────────────

    /// <summary>시작 버튼. 게임 플레이 씬으로 이동.</summary>
    public void StartGame()
    {
        _selectSound?.Play();
        OnBeforeStartGame?.Invoke();

        if (string.IsNullOrEmpty(_gameplaySceneName))
        {
            Debug.LogWarning("[MainMenuController] _gameplaySceneName 이 비어있음. 씬을 로드할 수 없음.", this);
            return;
        }

        SceneManager.LoadScene(_gameplaySceneName);
    }

    /// <summary>설정 버튼. 설정 패널 열기.</summary>
    public void OpenSettings()
    {
        _selectSound?.Play();
        if (_settingsPanel == null) return;

        if (_mainPanel != null) _mainPanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    /// <summary>설정 패널의 뒤로/닫기 버튼.</summary>
    public void CloseSettings()
    {
        _backSound?.Play();
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
        if (_mainPanel != null) _mainPanel.SetActive(true);
    }

    /// <summary>종료 버튼. 빌드에선 애플리케이션 종료, 에디터에선 Play Mode 종료.</summary>
    public void QuitGame()
    {
        _selectSound?.Play();
        OnBeforeQuit?.Invoke();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── 외부에서 호출 가능 (게임 종료 후 메뉴 복귀 등) ────

    /// <summary>게임 중 어디서든 메인 메뉴 씬으로 복귀할 때 호출. (예: 게임 클리어/실패 후)</summary>
    public static void ReturnToMainMenu(string mainMenuSceneName = "MainMenu")
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void EnsureSettingsController()
    {
        if (_settingsPanel == null) return;

        var settingsController = _settingsPanel.GetComponent<MainMenuSettingsController>();
        if (settingsController == null)
            settingsController = _settingsPanel.AddComponent<MainMenuSettingsController>();

        settingsController.Initialize(this);
    }
}
