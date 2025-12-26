public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float roundDuration = 30f; // 1분 라운드 시간

    public UnityEvent onTimerComplete = new UnityEvent();

    private float _currentTime = 0f;
    private bool _isRunning = false;
    private Coroutine _timerCoroutine;

    private void Start()
    {
        ResetTimer();
        StartTimer();
    }

    public void ResetTimer()
    {
        StopTimer();
        _currentTime = 0f;
        UpdateTimerDisplay();
    }

    private IEnumerator UpdateTimer()
    {
        while (_isRunning && RoundSystem.Shared?.OngameOver != true)
        {
            _currentTime += Time.deltaTime;

            if (_currentTime >= roundDuration)
            {
                StopTimer();
                onTimerComplete?.Invoke(); 
            }

            UpdateTimerDisplay();
            yield return null;
        }
    }

    private void StopTimer()
    {
        if (_isRunning)
        {
            _isRunning = false;
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }
    }

    public void StartTimer()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _timerCoroutine = StartCoroutine(UpdateTimer());
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        
        int minutes = Mathf.FloorToInt(_currentTime / 60);
        int seconds = Mathf.FloorToInt(_currentTime % 60);
        timerText.text = string.Format("{0}:{1:00}", minutes, seconds);
    }
}