using System;
using RavenDevOps.Fishing.Core;
using UnityEngine;

namespace RavenDevOps.Fishing.Fishing
{
    public sealed class FishingConditionController : MonoBehaviour
    {
        [SerializeField] private FishingTimeOfDay _timeOfDay = FishingTimeOfDay.Day;
        [SerializeField] private FishingWeatherState _weather = FishingWeatherState.Clear;
        [SerializeField] private bool _autoCycleTimeOfDay;
        [SerializeField] private float _secondsPerTimeStep = 150f;

        private float _timeStepElapsed;

        public FishingTimeOfDay TimeOfDay => _timeOfDay;
        public FishingWeatherState Weather => _weather;
        public event Action<FishingTimeOfDay, FishingWeatherState> ConditionsChanged;

        private void Awake()
        {
            RuntimeServiceRegistry.Register(this);
        }

        private void OnDestroy()
        {
            RuntimeServiceRegistry.Unregister(this);
        }

        private void Update()
        {
            if (!_autoCycleTimeOfDay)
            {
                return;
            }

            _timeStepElapsed += Time.deltaTime;
            if (_timeStepElapsed < Mathf.Max(10f, _secondsPerTimeStep))
            {
                return;
            }

            _timeStepElapsed = 0f;
            SetTimeOfDay(NextTimeOfDay(_timeOfDay));
        }

        public void SetTimeOfDay(FishingTimeOfDay value)
        {
            if (_timeOfDay == value)
            {
                return;
            }

            _timeOfDay = value;
            RaiseChanged();
        }

        public void SetWeather(FishingWeatherState value)
        {
            if (_weather == value)
            {
                return;
            }

            _weather = value;
            RaiseChanged();
        }

        public void CycleWeather()
        {
            SetWeather(NextWeather(_weather));
        }

        public void CycleTimeOfDay()
        {
            SetTimeOfDay(NextTimeOfDay(_timeOfDay));
        }

        public string GetConditionLabel()
        {
            return $"{_timeOfDay} | {_weather}";
        }

        public FishConditionModifier GetCombinedModifier()
        {
            var timeModifier = GetTimeModifier(_timeOfDay);
            var weatherModifier = GetWeatherModifier(_weather);
            return new FishConditionModifier
            {
                rarityWeightMultiplier = Mathf.Max(0.1f, timeModifier.rarityWeightMultiplier * weatherModifier.rarityWeightMultiplier),
                biteDelayMultiplier = Mathf.Max(0.2f, timeModifier.biteDelayMultiplier * weatherModifier.biteDelayMultiplier),
                fightStaminaMultiplier = Mathf.Max(0.2f, timeModifier.fightStaminaMultiplier * weatherModifier.fightStaminaMultiplier),
                pullIntensityMultiplier = Mathf.Max(0.2f, timeModifier.pullIntensityMultiplier * weatherModifier.pullIntensityMultiplier),
                escapeSecondsMultiplier = Mathf.Max(0.2f, timeModifier.escapeSecondsMultiplier * weatherModifier.escapeSecondsMultiplier)
            };
        }

        private void RaiseChanged()
        {
            try
            {
                ConditionsChanged?.Invoke(_timeOfDay, _weather);
            }
            catch (Exception ex)
            {
                Debug.LogError($"FishingConditionController: ConditionsChanged listener failed ({ex.Message}).");
            }
        }

        private static FishingTimeOfDay NextTimeOfDay(FishingTimeOfDay value)
        {
            switch (value)
            {
                case FishingTimeOfDay.Dawn:
                    return FishingTimeOfDay.Day;
                case FishingTimeOfDay.Day:
                    return FishingTimeOfDay.Dusk;
                case FishingTimeOfDay.Dusk:
                    return FishingTimeOfDay.Night;
                default:
                    return FishingTimeOfDay.Dawn;
            }
        }

        private static FishingWeatherState NextWeather(FishingWeatherState value)
        {
            switch (value)
            {
                case FishingWeatherState.Clear:
                    return FishingWeatherState.Overcast;
                case FishingWeatherState.Overcast:
                    return FishingWeatherState.Rain;
                case FishingWeatherState.Rain:
                    return FishingWeatherState.Storm;
                default:
                    return FishingWeatherState.Clear;
            }
        }

        private static FishConditionModifier GetTimeModifier(FishingTimeOfDay timeOfDay)
        {
            switch (timeOfDay)
            {
                case FishingTimeOfDay.Dawn:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.15f,
                        biteDelayMultiplier = 0.88f,
                        fightStaminaMultiplier = 0.95f,
                        pullIntensityMultiplier = 1f,
                        escapeSecondsMultiplier = 1f
                    };
                case FishingTimeOfDay.Dusk:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.2f,
                        biteDelayMultiplier = 0.9f,
                        fightStaminaMultiplier = 1f,
                        pullIntensityMultiplier = 1.05f,
                        escapeSecondsMultiplier = 1f
                    };
                case FishingTimeOfDay.Night:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 0.92f,
                        biteDelayMultiplier = 1.12f,
                        fightStaminaMultiplier = 1.08f,
                        pullIntensityMultiplier = 1.08f,
                        escapeSecondsMultiplier = 0.96f
                    };
                default:
                    return FishConditionModifier.Identity;
            }
        }

        private static FishConditionModifier GetWeatherModifier(FishingWeatherState weather)
        {
            switch (weather)
            {
                case FishingWeatherState.Overcast:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.05f,
                        biteDelayMultiplier = 0.95f,
                        fightStaminaMultiplier = 1f,
                        pullIntensityMultiplier = 1f,
                        escapeSecondsMultiplier = 1.03f
                    };
                case FishingWeatherState.Rain:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.12f,
                        biteDelayMultiplier = 0.9f,
                        fightStaminaMultiplier = 1.05f,
                        pullIntensityMultiplier = 1.08f,
                        escapeSecondsMultiplier = 1f
                    };
                case FishingWeatherState.Storm:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.2f,
                        biteDelayMultiplier = 0.82f,
                        fightStaminaMultiplier = 1.14f,
                        pullIntensityMultiplier = 1.2f,
                        escapeSecondsMultiplier = 0.9f
                    };
                default:
                    return FishConditionModifier.Identity;
            }
        }
    }
}
