using System;
using System.Text;
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
            return $"{FormatLabel(_timeOfDay.ToString())} | {FormatLabel(_weather.ToString())}";
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
                    return FishingWeatherState.Sunny;
                case FishingWeatherState.Sunny:
                    return FishingWeatherState.PartlyCloudy;
                case FishingWeatherState.PartlyCloudy:
                    return FishingWeatherState.Clouds;
                case FishingWeatherState.Clouds:
                    return FishingWeatherState.Overcast;
                case FishingWeatherState.Overcast:
                    return FishingWeatherState.Foggy;
                case FishingWeatherState.Foggy:
                    return FishingWeatherState.Rain;
                case FishingWeatherState.Rain:
                    return FishingWeatherState.Thunderstorm;
                case FishingWeatherState.Thunderstorm:
                    return FishingWeatherState.Storm;
                case FishingWeatherState.Storm:
                    return FishingWeatherState.QuarterMoon;
                case FishingWeatherState.QuarterMoon:
                    return FishingWeatherState.HalfMoon;
                case FishingWeatherState.HalfMoon:
                    return FishingWeatherState.FullMoon;
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
                case FishingWeatherState.Sunny:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 0.98f,
                        biteDelayMultiplier = 1.02f,
                        fightStaminaMultiplier = 0.98f,
                        pullIntensityMultiplier = 0.98f,
                        escapeSecondsMultiplier = 1.02f
                    };
                case FishingWeatherState.PartlyCloudy:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.03f,
                        biteDelayMultiplier = 0.97f,
                        fightStaminaMultiplier = 1f,
                        pullIntensityMultiplier = 1f,
                        escapeSecondsMultiplier = 1.02f
                    };
                case FishingWeatherState.Clouds:
                case FishingWeatherState.Overcast:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.05f,
                        biteDelayMultiplier = 0.95f,
                        fightStaminaMultiplier = 1f,
                        pullIntensityMultiplier = 1f,
                        escapeSecondsMultiplier = 1.03f
                    };
                case FishingWeatherState.Foggy:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.06f,
                        biteDelayMultiplier = 0.94f,
                        fightStaminaMultiplier = 1.04f,
                        pullIntensityMultiplier = 1.02f,
                        escapeSecondsMultiplier = 1f
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
                case FishingWeatherState.Thunderstorm:
                case FishingWeatherState.Storm:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.2f,
                        biteDelayMultiplier = 0.82f,
                        fightStaminaMultiplier = 1.14f,
                        pullIntensityMultiplier = 1.2f,
                        escapeSecondsMultiplier = 0.9f
                    };
                case FishingWeatherState.QuarterMoon:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.06f,
                        biteDelayMultiplier = 0.95f,
                        fightStaminaMultiplier = 1.02f,
                        pullIntensityMultiplier = 1.02f,
                        escapeSecondsMultiplier = 1f
                    };
                case FishingWeatherState.HalfMoon:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.1f,
                        biteDelayMultiplier = 0.92f,
                        fightStaminaMultiplier = 1.05f,
                        pullIntensityMultiplier = 1.05f,
                        escapeSecondsMultiplier = 0.98f
                    };
                case FishingWeatherState.FullMoon:
                    return new FishConditionModifier
                    {
                        rarityWeightMultiplier = 1.14f,
                        biteDelayMultiplier = 0.89f,
                        fightStaminaMultiplier = 1.08f,
                        pullIntensityMultiplier = 1.08f,
                        escapeSecondsMultiplier = 0.96f
                    };
                default:
                    return FishConditionModifier.Identity;
            }
        }

        private static string FormatLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 4);
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(value[i - 1]))
                {
                    builder.Append(' ');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
