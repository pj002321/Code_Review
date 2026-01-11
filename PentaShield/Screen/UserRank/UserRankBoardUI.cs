using penta;
using Cysharp.Threading.Tasks;
using penta;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UserRankBoardUI : MonoBehaviour
{
    [SerializeField] private Image regionImage = null;
    [SerializeField] private TextMeshProUGUI scoreText = null;
    [SerializeField] private TextMeshProUGUI userNameText = null;
    [SerializeField] private TextMeshProUGUI levelText = null;
    [SerializeField] private TextMeshProUGUI rankingText = null;
    [SerializeField] private TextMeshProUGUI waveText = null;

    [SerializeField] private int score = 0;
    [SerializeField] private int level = 0;
    [SerializeField] private int rank = 0;
    [SerializeField] private int wave = 0;

    private int previousScore = 0;
    private UserRankListUI parentRankList;

    public Image RegionImage => regionImage;
    public TextMeshProUGUI ScoreText => scoreText;
    public TextMeshProUGUI UserNameText => userNameText;
    public TextMeshProUGUI LevelText => levelText;
    public TextMeshProUGUI RankingText => rankingText;
    public TextMeshProUGUI WaveText => waveText;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    public int CurrentRank { get; set; } = -1;
    public int TargetRank { get; set; } = -1;
    public bool IsAnimating { get; private set; } = false;
    
    private Vector3 originalPosition;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        
        parentRankList = GetComponentInParent<UserRankListUI>();
        
        previousScore = score;
        UpdateUI();
    }      
    
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateUI();
        }
    }
    
    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = score.ToString();
        if (levelText != null) levelText.text = level.ToString();
        if (rankingText != null) rankingText.text = rank.ToString();
        if (waveText!= null) waveText.text = $"WAVE {wave}";
    }  
 
    
    public async UniTask AnimateToNewPosition()
    {
        if (IsAnimating) return;
        
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                $"{gameObject.name}: RectTransform not found!".DWarning();
                return;
            }
        }
        
        IsAnimating = true;
        Vector3 startPosition = rectTransform.anchoredPosition;
        Vector3 targetPosition = originalPosition;
        
        $"{gameObject.name}: Animation started - {startPosition} → {targetPosition}".DLog();
        
        float elapsedTime = 0f;
        
        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            float curveValue = animationCurve.Evaluate(progress);
            
            rectTransform.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, curveValue);
            
            await UniTask.Yield();
        }
        
        rectTransform.anchoredPosition = targetPosition;
        IsAnimating = false;
    }
    
    public void SetTargetPosition(Vector3 position)
    {
        originalPosition = position;
    }
    
    public void SetPositionImmediate(Vector3 position)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                $"{gameObject.name}: RectTransform not found!".DWarning();
                return;
            }
        }
        
        rectTransform.anchoredPosition = position;
        originalPosition = position;
    }
    
    public void SetRank(int currentRank, int targetRank)
    {
        CurrentRank = currentRank;
        TargetRank = targetRank;
    }
    
    public void SetScore(int newScore)
    {
        score = newScore;
        UpdateUI();
    }

    public void SetWave(int round)
    {
        wave = round;
        UpdateUI();
    }
    
    public void SetLevel(int newLevel)
    {
        level = newLevel;
        UpdateUI();
    }
    
    [ContextMenu("Set Score 500")]
    public void SetScore500()
    {
        SetScore(500);
    }
    
    [ContextMenu("Set Score 1000")]
    public void SetScore1000()
    {
        SetScore(1000);
    }
    
    public void SetRankValue(int newRank)
    {
        rank = newRank;
        UpdateUI();
    }
    
    public int GetScore()
    {
        return score;
    }
    
    public int GetRankValue()
    {
        return rank;
    }

}
