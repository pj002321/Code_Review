using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Hunt
{
    public class ActorFxController : MonoBehaviour
    {
        private Animator _animator;
        private CharacterFxPreset _preset;
        
        // 런타임 상태 추적용
        private int _lastStateHash;
        private float _lastNormalizedTime;
        private AnimationClip _currentClip;
        private List<FxTiming> _currentTimings;
        private HashSet<int> _triggeredIndices = new HashSet<int>();

        private IsAttackPointer _attackPointer;
        private UserCombat _userCombat;

        public void Initialize(CharacterFxPreset preset)
        {
            _preset = preset;
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            
            // 상위(UserCharacter/Root)에 있는 AttackPointer / UserCombat 자동 감지
            _attackPointer = GetComponentInParent<IsAttackPointer>();
            _userCombat = GetComponentInParent<UserCombat>();
            
            // 초기화 시 필요한 리소스 프리로드 (선택 사항)
            PreloadResources().Forget();
            
            $"[ActorFxController] Initialized with preset: {_preset.name} (AttackPointer Found: {_attackPointer != null}, Combat Found: {_userCombat != null})".DLog();
        }

        private async UniTaskVoid PreloadResources()
        {
            if (_preset == null) return;
            // 필요한 경우 여기서 VFX/SFX 프리로드
            await UniTask.Yield();
        }

        private void Update()
        {
            if (_preset == null) return;
            if (_animator == null) return;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            int currentHash = stateInfo.fullPathHash;
            float normalizedTime = stateInfo.normalizedTime % 1.0f; 
            
            // 상태 변경 감지
            if (currentHash != _lastStateHash)
            {
                OnStateChanged();
                _lastStateHash = currentHash;
            }

            // 현재 클립이 유효하고 타이밍 데이터가 있다면 체크
            if (_currentTimings != null && _currentClip != null)
            {
                CheckTriggers(_lastNormalizedTime, normalizedTime, _currentClip.length);
            }

            _lastNormalizedTime = normalizedTime;
        }

        private void OnStateChanged()
        {
            _currentClip = null;
            _currentTimings = null;
            _triggeredIndices.Clear();
            _lastNormalizedTime = 0;

            var clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                var clip = clipInfo[0].clip;
                if (clip != null)
                {
                    _currentClip = clip;
                    // 프리셋에서 현재 클립에 해당하는 데이터 찾기
                    var data = _preset.clipFxDataList.Find(x => x.clipName == clip.name);
                    if (data != null)
                    {
                        _currentTimings = data.fxTimings;
                    }
                }
            }
        }

        private void CheckTriggers(float prevNormalized, float currentNormalized, float clipLength)
        {
            if (_currentTimings == null) return;

            // 루프 처리: prev > current 인 경우 (한 바퀴 돎)
            bool looped = currentNormalized < prevNormalized;

            for (int i = 0; i < _currentTimings.Count; i++)
            {
                var timing = _currentTimings[i];
                float triggerNormalized = timing.timeInSeconds / clipLength;

                bool shouldTrigger = false;

                if (looped)
                {
                    // 루프 시 처리
                    if (triggerNormalized >= prevNormalized || triggerNormalized <= currentNormalized)
                    {
                        shouldTrigger = true;
                    }
                }
                else
                {
                    if (triggerNormalized >= prevNormalized && triggerNormalized <= currentNormalized)
                    {
                        shouldTrigger = true;
                    }
                }

                if (shouldTrigger)
                {
                    if (!_triggeredIndices.Contains(i) || looped)
                    {
                        PlayEffect(timing);
                        _triggeredIndices.Add(i);
                    }
                }
            }
            
            if (looped)
            {
                _triggeredIndices.Clear();
            }
        }

        private void PlayEffect(FxTiming timing)
        {
             PlayEffectAsync(timing).Forget();
        }

        private async UniTaskVoid PlayEffectAsync(FxTiming timing)
        {
            // VFX 재생
            if (timing.vfxType != VfxType.None)
            {
                string key = VfxKeyConst.GetVfxKey(timing.vfxType);
                if (!string.IsNullOrEmpty(key))
                {
                    // 스폰 기준점 결정 (AttackPointer가 있으면 우선 사용)
                    Transform targetT = (_attackPointer != null) ? _attackPointer.GetT() : transform;
                    
                    Vector3 spawnPos = targetT.position;
                    Quaternion spawnRot = targetT.rotation;
                    Transform parentT = null;

                    // AttachHit: 캐릭터(혹은 기준점)에 부착
                    if (timing.attachHit)
                    {
                        parentT = targetT;
                    }

                    var handle = await VfxManager.Shared.PlayOneShot(key, spawnPos, spawnRot, parent: parentT);
                    
                    // HitDetector 설정 (UserCombat 등 연동)
                    if (handle != null && handle.IsVaild && _userCombat != null)
                    {
                        _userCombat.SetupHitDetectorFor(handle.vfxObject);
                    }
                }
            }

            // SFX 재생
            if (timing.audioType != AudioType.None)
            {
                string key = AudioKeyConst.GetSfxKey(timing.audioType);
                if (!string.IsNullOrEmpty(key))
                {
                     AudioManager.Shared.PlaySfx(key);
                }
            }
        }
    }
}
