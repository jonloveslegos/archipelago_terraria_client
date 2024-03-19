using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SeldomArchipelago.Players;
using SeldomArchipelago.Systems;

namespace SeldomArchipelago.Globals
{
    class DeleteBannedGlobalItem : GlobalItem
    {
        public override bool CanUseItem(Item item, Player player)
        {
            var apPlayer = player.GetModPlayer<ArchipelagoPlayer>();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            if (archipelagoSystem.IsBannedItem(item.Name))
            {

                if (!apPlayer.obtainedItems.Contains(ItemID.Search.GetName(item.netID)))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
