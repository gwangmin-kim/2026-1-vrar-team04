using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour, IConditionReceiver
{
    [SerializeField] private string _nextSceneName;
    [SerializeField] private string _failSceneName;

    private bool _conditionMet = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[SceneTransition] Player 태그가 아닌 객체가 충돌 → 무시");
            return;
        }

        Debug.Log($"[SceneTransition] 조건 상태: {_conditionMet}");

        if (_conditionMet)
        {
            Debug.Log($"[SceneTransition] 조건 만족 → {_nextSceneName} 로드");
            SceneManager.LoadSceneAsync(_nextSceneName);
        }
        else
        {
            Debug.Log($"[SceneTransition] 조건 불만족 → {_failSceneName} 로드");
            SceneManager.LoadSceneAsync(_failSceneName);
        }
    }

    public void SetCondition(bool met)
    {
        _conditionMet = met;
        Debug.Log($"[SceneTransition] 조건 변경: {_conditionMet}");
    }
}
