using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Gui.Rendering;
using Gui.Widgets.Framework;
using HudUI.Widgets;
using SleepNeed.Energy;
using SleepNeed.Sleepiness;

namespace MooHudBars.HudBars
{
    public static class SleepNeedBars
    {
        private const string EnergyBarId        = "moohuduibars:sleepneed_energy";
        private const string SleepBarId         = "moohuduibars:sleepneed_sleepiness";
        private const string SleepOverloadBarId = "moohuduibars:sleepneed_sleepiness_overload";

        private const string IconDomain = "moohudui";

        private const string EnergyColour   = "#f4a523";
        private const string SleepColour    = "#1a3a6b";
        private const string OverloadColour = "#6b2fa0";

        private static ValueNotifier<float> _energyRatioNotifier;
        private static ValueNotifier<float> _energyDisplayNotifier;
        private static ValueNotifier<float> _sleepRatioNotifier;
        private static ValueNotifier<float> _sleepDisplayNotifier;
        private static ValueNotifier<float> _overloadRatioNotifier;
        private static ValueNotifier<float> _overloadDisplayNotifier;

        private static long _tickListenerId;
        private static ICoreClientAPI _api;

        public static void Init(ICoreClientAPI api)
        {
            _api = api;

            _energyRatioNotifier     = new ValueNotifier<float>(1f);
            _energyDisplayNotifier   = new ValueNotifier<float>(0f);
            _sleepRatioNotifier      = new ValueNotifier<float>(0f);
            _sleepDisplayNotifier    = new ValueNotifier<float>(0f);
            _overloadRatioNotifier   = new ValueNotifier<float>(0f);
            _overloadDisplayNotifier = new ValueNotifier<float>(0f);

            _tickListenerId = api.Event.RegisterGameTickListener(OnTick, 200);

            RegisterEnergyBar();
            RegisterSleepinessBar();
        }

        private static void RegisterEnergyBar()
        {
            EdgeBarController.Instance!.AddBar(
                EnergyBarId,
                EdgeBarSide.Left,
                new EdgeBarConfig(
                    color:              EnergyColour.FromHex(),
                    valueSource:        _energyRatioNotifier,
                    displayValueSource: _energyDisplayNotifier,
                    flashThreshold:     0.20f
                )
            );
        }

        private static void RegisterSleepinessBar()
        {
            EdgeBarController.Instance!.AddBar(
                SleepBarId,
                EdgeBarSide.Right,
                new EdgeBarConfig(
                    color:              SleepColour.FromHex(),
                    valueSource:        _sleepRatioNotifier,
                    displayValueSource: _sleepDisplayNotifier,
                    flashThreshold:     0.85f
                )
            );
        }

        private static void RegisterOverloadBar()
        {
            EdgeBarController.Instance!.AddBar(
                SleepOverloadBarId,
                EdgeBarSide.Right,
                new EdgeBarConfig(
                    color:              OverloadColour.FromHex(),
                    valueSource:        _overloadRatioNotifier,
                    displayValueSource: _overloadDisplayNotifier,
                    flashThreshold:     0.01f,
                    iconDomain: IconDomain,
                    iconPath:   "textures/sleepneed_sleepiness_overload.svg"
                )
            );
        }

        public static void Dispose()
        {
            if (_api != null && _tickListenerId != 0)
            {
                _api.Event.UnregisterGameTickListener(_tickListenerId);
                _tickListenerId = 0;
            }

            _energyRatioNotifier?.Dispose();
            _energyDisplayNotifier?.Dispose();
            _sleepRatioNotifier?.Dispose();
            _sleepDisplayNotifier?.Dispose();
            _overloadRatioNotifier?.Dispose();
            _overloadDisplayNotifier?.Dispose();

            _energyRatioNotifier     = null;
            _energyDisplayNotifier   = null;
            _sleepRatioNotifier      = null;
            _sleepDisplayNotifier    = null;
            _overloadRatioNotifier   = null;
            _overloadDisplayNotifier = null;

            _api = null;
        }
        private static void OnTick(float dt)
        {
            if (_api?.World?.Player == null)
            {
                _api?.Logger.Warning("[MooHudUIBars] Player is null in OnTick");
                return;
            }

            var eb = GetEnergyBehavior();
            var sb = GetSleepinessBehavior();
            
            _api.World.Logger.Warning($"[MooHudUIBars] Energy behavior: {eb?.CurrentEnergy ?? -1} / {eb?.MaxEnergy ?? -1}");
            _api.World.Logger.Warning($"[MooHudUIBars] Sleep behavior: {sb?.CurrentSleepinessLevel ?? -1}");

            UpdateEnergyBar();
            UpdateSleepinessBars();
        }

        private static void UpdateEnergyBar()
        {
            var eb = GetEnergyBehavior();
            if (eb == null) return;

            float max = eb.MaxEnergy;
            if (max <= 0f) return;

            _energyRatioNotifier!.Value   = eb.CurrentEnergy / max;
            _energyDisplayNotifier!.Value = eb.CurrentEnergy;
        }

        private static void UpdateSleepinessBars()
        {
            var controller = EdgeBarController.Instance;
            if (controller == null) return;

            var sb = GetSleepinessBehavior();
            if (sb == null) return;

            float capacity         = sb.SleepinessCapacity;
            float overloadCapacity = sb.SleepinessCapacityOverload;
            float current          = sb.CurrentSleepinessLevel;
            bool  isOverloaded     = current > capacity;

            bool hasBaseBar     = controller.HasBar(SleepBarId);
            bool hasOverloadBar = controller.HasBar(SleepOverloadBarId);

            if (!isOverloaded)
            {
                if (hasOverloadBar)
                {
                    controller.RemoveBar(SleepOverloadBarId);
                    hasOverloadBar = false;
                }

                if (!hasBaseBar)
                {
                    RegisterSleepinessBar();
                    hasBaseBar = true;
                }

                if (capacity > 0f)
                {
                    _sleepRatioNotifier!.Value   = current / capacity;
                    _sleepDisplayNotifier!.Value = current;
                }
            }
            else
            {
                if (hasBaseBar)
                {
                    controller.RemoveBar(SleepBarId);
                    hasBaseBar = false;
                }

                if (!hasOverloadBar)
                {
                    RegisterOverloadBar();
                    hasOverloadBar = true;
                }

                if (overloadCapacity > 0f)
                {
                    float overloadAmount = current - capacity;
                    _overloadRatioNotifier!.Value   = System.Math.Clamp(overloadAmount / overloadCapacity, 0f, 1f);
                    _overloadDisplayNotifier!.Value = current;
                }
            }
        }
    
        private static EntityBehaviorEnergy GetEnergyBehavior()
        {
            return _api?.World?.Player?.Entity?.GetBehavior<EntityBehaviorEnergy>();
        }

        private static EntityBehaviorSleepiness GetSleepinessBehavior()
        {
            return _api?.World?.Player?.Entity?.GetBehavior<EntityBehaviorSleepiness>();
        }
    }
}