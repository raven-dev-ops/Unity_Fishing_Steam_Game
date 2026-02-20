using RavenDevOps.Fishing.Audio;
using RavenDevOps.Fishing.Core;
using RavenDevOps.Fishing.Save;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class CatchResolver : MonoBehaviour
    {
        [SerializeField] private FishingActionStateMachine _stateMachine;
        [SerializeField] private FishSpawner _spawner;
        [SerializeField] private HookMovementController _hook;
        [SerializeField] private SaveManager _saveManager;
        [SerializeField] private AudioManager _audioManager;

        [SerializeField] private int _currentDistanceTier = 1;
        [SerializeField] private AudioClip _castSfx;
        [SerializeField] private AudioClip _hookSfx;
        [SerializeField] private AudioClip _catchSfx;

        private FishDefinition _hookedFish;

        private void Awake()
        {
            RuntimeServiceRegistry.Resolve(ref _saveManager, this, warnIfMissing: false);
            RuntimeServiceRegistry.Resolve(ref _audioManager, this, warnIfMissing: false);
        }

        private void OnEnable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged += OnStateChanged;
            }
        }

        private void OnDisable()
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(FishingActionState previous, FishingActionState next)
        {
            if (next == FishingActionState.InWater)
            {
                _audioManager?.PlaySfx(_castSfx);
                TryHookFish();
            }
            else if (next == FishingActionState.Hooked)
            {
                _audioManager?.PlaySfx(_hookSfx);
            }
            else if (next == FishingActionState.Resolve)
            {
                ResolveCatch();
            }
        }

        private void TryHookFish()
        {
            if (_spawner == null || _hook == null)
            {
                return;
            }

            _hookedFish = _spawner.RollFish(_currentDistanceTier, _hook.CurrentDepth);
            if (_hookedFish != null)
            {
                _stateMachine?.SetHooked();
            }
        }

        private void ResolveCatch()
        {
            if (_hookedFish == null || _saveManager == null)
            {
                _stateMachine?.ResetToCast();
                return;
            }

            var save = _saveManager.Current;
            var existing = save.fishInventory.Find(x => x.fishId == _hookedFish.id && x.distanceTier == _currentDistanceTier);
            if (existing == null)
            {
                save.fishInventory.Add(new FishInventoryEntry
                {
                    fishId = _hookedFish.id,
                    distanceTier = _currentDistanceTier,
                    count = 1
                });
            }
            else
            {
                existing.count += 1;
            }

            save.stats.totalFishCaught += 1;
            save.stats.farthestDistanceTier = Mathf.Max(save.stats.farthestDistanceTier, _currentDistanceTier);

            _saveManager.Save();
            _audioManager?.PlaySfx(_catchSfx);

            _hookedFish = null;
            _stateMachine?.ResetToCast();
        }
    }
}
