using MooHudBars.HudBars;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace MooHudBars
{
    public class MooHudBarsSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            if (api.ModLoader.IsModEnabled("sleepneed"))
                SleepNeedBars.Init(api);

//            if (api.ModLoader.IsModEnabled("expandedstomach"))
//                ExpandedStomachBars.Init(api);
        }

        public override void Dispose()
        {
            SleepNeedBars.Dispose();
//            ExpandedStomachBars.Dispose();
            base.Dispose();
        }
    }
}