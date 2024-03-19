using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using SeldomArchipelago.Players;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Social;

namespace SeldomArchipelago.Systems
{
    // TODO Use a separate class for data and logic
    public class ArchipelagoSystem : ModSystem
    {
        List<string> locationBacklog = new List<string>();
        List<Task<LocationInfoPacket>> locationQueue;
        ArchipelagoSession session;
        DeathLinkService deathlink;
        bool randomize_crafting_stations = false;
        bool randomize_pickaxes = false;
        bool randomize_hammers = false;
        bool randomize_crystals = false;
        bool enabled;
        int collectedItems;
        int currentItem;
        List<string> collectedLocations = new List<string>();
        List<string> goals = new List<string>();
        bool victory = false;
        int slot = 0;

        public override void LoadWorldData(TagCompound tag)
        {
            collectedItems = tag.ContainsKey("ApCollectedItems") ? tag.Get<int>("ApCollectedItems") : 0;
        }

        public override void OnWorldLoad()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.None);

            locationQueue = new List<Task<LocationInfoPacket>>();

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            var config = ModContent.GetInstance<Config.Config>();
            session = ArchipelagoSessionFactory.CreateSession(config.address, config.port);

            LoginResult result;
            try
            {
                result = session.TryConnectAndLogin("Terraria", config.name, ItemsHandlingFlags.AllItems, null, null, null, config.password == "" ? null : config.password);
                if (result is LoginFailure)
                {
                    session = null;
                    return;
                }
            }
            catch
            {
                session = null;
                return;
            }

            var locations = session.DataStorage[Scope.Slot, "CollectedLocations"].To<String[]>();
            if (locations != null)
            {
                collectedLocations = new List<string>(locations);
            }

            var success = (LoginSuccessful)result;
            goals = new List<string>(((JArray)success.SlotData["goal"]).ToObject<string[]>());

            victory = false;

            session.MessageLog.OnMessageReceived += (message) =>
            {
                var text = "";
                foreach (var part in message.Parts)
                {
                    text += part.Text;
                }
                Chat(text);
            };

            if ((bool)success.SlotData["deathlink"])
            {
                deathlink = session.CreateDeathLinkService();
                deathlink.EnableDeathLink();

                deathlink.OnDeathLinkReceived += ReceiveDeathlink;
            }

            if ((bool)success.SlotData["randomize_crafting_stations"])
            {
                randomize_crafting_stations = true;
            }

            if ((bool)success.SlotData["randomize_pickaxes"])
            {
                randomize_pickaxes = true;
            }

            if ((bool)success.SlotData["randomize_hammers"])
            {
                randomize_hammers = true;
            }

            if ((bool)success.SlotData["randomize_crystals"])
            {
                randomize_crystals = true;
            }

            slot = success.Slot;
        }
        public string GetBannedItemLocation(string item)
        {
            switch (item)
            {
                case "Tungsten Pickaxe": return "Obtain Silver Pickaxe";
                case "Gold Pickaxe": return "Obtain Gold Pickaxe";
                case "Fossil Pickaxe": return "Obtain Fossil Pickaxe";
                case "Nightmare Pickaxe": return "Obtain Nightmare Pickaxe";
                case "Deathbringer Pickaxe": return "Obtain Nightmare Pickaxe";
                case "Molten Pickaxe": return "Obtain Molten Pickaxe";
                case "Cobalt Pickaxe": return "Obtain Cobalt Pickaxe";
                case "Palladium Pickaxe": return "Obtain Cobalt Pickaxe";
                case "Mythril Pickaxe": return "Obtain Mythril Pickaxe";
                case "Orichalcum Pickaxe": return "Obtain Mythril Pickaxe";
                case "Adamantite Pickaxe": return "Obtain Adamantite Pickaxe";
                case "Titanium Pickaxe": return "Obtain Adamantite Pickaxe";
                case "Spectre Pickaxe": return "Obtain Spectre Pickaxe";
                case "Spectre Hamaxe": return "Obtain Spectre Hamaxe";
                case "Chlorophyte Pickaxe": return "Obtain Chlorophyte Pickaxe";
                case "Pickaxe Axe": return "Obtain Pickaxe Axe";
                case "Shroomite Digging Claw": return "Obtain Shroomite Digging Claw";
                case "Picksaw": return "Obtain Picksaw";
                case "Vortex Pickaxe": return "Obtain Luminite Pickaxe";
                case "Nebula Pickaxe": return "Obtain Luminite Pickaxe";
                case "Solar Flare Pickaxe": return "Obtain Luminite Pickaxe";
                case "Stardust Pickaxe": return "Obtain Luminite Pickaxe";
                case "Pwnhammer": return "Obtain Pwnhammer";
                case "Drax": return "Obtain Drax";
                case "Vortex Hamaxe": return "Obtain Luminite Hamaxe";
                case "Nebula Hamaxe": return "Obtain Luminite Hamaxe";
                case "Solar Flare Hamaxe": return "Obtain Luminite Hamaxe";
                case "Stardust Hamaxe": return "Obtain Luminite Hamaxe";
                case "Work Bench": return "Obtain Work Bench";
                case "Life Crystal": return "Obtain Life Crystal";
                case "Mana Crystal": return "Obtain Mana Crystal";
                case "Furnace": return "Obtain Furnace";
                case "Iron Anvil": return "Obtain Anvil";
                case "Lead Anvil": return "Obtain Anvil";
                case "Loom": return "Obtain Loom";
                case "Sawmill": return "Obtain Sawmill";
                case "Solidifier": return "Obtain Solidifier";
                case "Hellforge": return "Obtain Hellforge";
                case "Tinkerer's Workshop": return "Obtain Tinkerer's Workshop";
                case "Mythril Anvil": return "Obtain Hardmode Anvil";
                case "Orichalcum Anvil": return "Obtain Hardmode Anvil";
                case "Adamantite Forge": return "Obtain Hardmode Forge";
                case "Titanium Forge": return "Obtain Hardmode Forge";
                case "Life Fruit": return "Obtain Life Fruit";
                case "Autohammer": return "Obtain Autohammer";
                case "Ancient Manipulator": return "Obtain Ancient Manipulator";
                case "Chlorophyte Warhammer": return "Obtain Chlorophyte Warhammer";
                case "The Axe": return "Obtain The Axe";
                default: return null;
            }
        }
        public bool IsBannedItem(string item)
        {
            switch (item)
            {
                case "Silver Pickaxe": return randomize_pickaxes;
                case "Tungsten Pickaxe": return randomize_pickaxes;
                case "Gold Pickaxe": return randomize_pickaxes;
                case "Candy Cane Pickaxe": return randomize_pickaxes;
                case "Fossil Pickaxe": return randomize_pickaxes;
                case "Bone Pickaxe": return randomize_pickaxes;
                case "Platinum Pickaxe": return randomize_pickaxes;
                case "Reaver Shark": return randomize_pickaxes;
                case "Nightmare Pickaxe": return randomize_pickaxes;
                case "Deathbringer Pickaxe": return randomize_pickaxes;
                case "Molten Pickaxe": return randomize_pickaxes;
                case "Cobalt Pickaxe": return randomize_pickaxes;
                case "Palladium Pickaxe": return randomize_pickaxes;
                case "Mythril Pickaxe": return randomize_pickaxes;
                case "Orichalcum Pickaxe": return randomize_pickaxes;
                case "Adamantite Pickaxe": return randomize_pickaxes;
                case "Titanium Pickaxe": return randomize_pickaxes;
                case "Spectre Pickaxe": return randomize_pickaxes;
                case "Spectre Hamaxe": return randomize_hammers;
                case "Chlorophyte Pickaxe": return randomize_pickaxes;
                case "Pickaxe Axe": return randomize_pickaxes;
                case "Shroomite Digging Claw": return randomize_pickaxes;
                case "Picksaw": return randomize_pickaxes;
                case "Vortex Pickaxe": return randomize_pickaxes;
                case "Nebula Pickaxe": return randomize_pickaxes;
                case "Solar Flare Pickaxe": return randomize_pickaxes;
                case "Stardust Pickaxe": return randomize_pickaxes;
                case "Pwnhammer": return randomize_hammers;
                case "Cobalt Drill": return randomize_pickaxes;
                case "Palladium Drill": return randomize_pickaxes;
                case "Mythril Drill": return randomize_pickaxes;
                case "Orichalcum Drill": return randomize_pickaxes;
                case "Adamantite Drill": return randomize_pickaxes;
                case "Titanium Drill": return randomize_pickaxes;
                case "Chlorophyte Drill": return randomize_pickaxes;
                case "Drax": return randomize_pickaxes;
                case "Vortex Drill": return randomize_pickaxes;
                case "Nebula Drill": return randomize_pickaxes;
                case "Solar Flare Drill": return randomize_pickaxes;
                case "Stardust Drill": return randomize_pickaxes;
                case "Vortex Hamaxe": return randomize_hammers;
                case "Nebula Hamaxe": return randomize_hammers;
                case "Solar Flare Hamaxe": return randomize_hammers;
                case "Stardust Hamaxe": return randomize_hammers;
                case "Laser Drill": return randomize_pickaxes;
                case "Work Bench": return randomize_crafting_stations;
                case "Life Crystal": return randomize_crystals;
                case "Mana Crystal": return randomize_crystals;
                case "Furnace": return randomize_crafting_stations;
                case "Iron Anvil": return randomize_crafting_stations;
                case "Lead Anvil": return randomize_crafting_stations;
                case "Loom": return randomize_crafting_stations;
                case "Sawmill": return randomize_crafting_stations;
                case "Solidifier": return randomize_crafting_stations;
                case "Hellforge": return randomize_crafting_stations;
                case "Tinkerer's Workshop": return randomize_crafting_stations;
                case "Mythril Anvil": return randomize_crafting_stations;
                case "Orichalcum Anvil": return randomize_crafting_stations;
                case "Adamantite Forge": return randomize_crafting_stations;
                case "Titanium Forge": return randomize_crafting_stations;
                case "Life Fruit": return randomize_crystals;
                case "Autohammer": return randomize_crafting_stations;
                case "Ancient Manipulator": return randomize_crafting_stations;
                case "Chlorophyte Warhammer": return randomize_hammers;
                case "The Axe": return randomize_hammers;
                default: return false;
            }
        }

        static readonly Func<bool>[] flags = new[] { () => NPC.downedSlimeKing, () => NPC.downedBoss1, () => NPC.downedBoss2, () => DD2Event.DownedInvasionT1, () => NPC.downedGoblins, () => NPC.downedQueenBee, () => NPC.downedBoss3, () => NPC.downedDeerclops, () => Main.hardMode, () => NPC.downedPirates, () => NPC.downedQueenSlime, () => NPC.downedMechBoss1, () => NPC.downedMechBoss2, () => NPC.downedMechBoss3, () => NPC.downedPlantBoss, () => NPC.downedGolemBoss, () => DD2Event.DownedInvasionT2, () => NPC.downedMartians, () => NPC.downedFishron, () => NPC.downedHalloweenTree, () => NPC.downedHalloweenKing, () => NPC.downedChristmasTree, () => NPC.downedChristmasSantank, () => NPC.downedChristmasIceQueen, () => NPC.downedFrost, () => NPC.downedEmpressOfLight, () => NPC.downedAncientCultist, () => NPC.downedTowerNebula, () => NPC.downedMoonlord };

        public void Collect(string item)
        {
            switch (item)
            {
                case "Reward: Torch God's Favor": GiveItem(ItemID.TorchGodsFavor); break;
                case "Post-King Slime": NPC.downedSlimeKing = true; break;
                case "Post-Eye of Cthulhu": NPC.downedBoss1 = true; break;
                case "Post-Evil Boss": NPC.downedBoss2 = true; break;
                case "Post-Old One's Army Tier 1": DD2Event.DownedInvasionT1 = true; break;
                case "Post-Goblin Army": NPC.downedGoblins = true; break;
                case "Post-Queen Bee": NPC.downedQueenBee = true; break;
                case "Post-Skeletron": NPC.downedBoss3 = true; break;
                case "Post-Deerclops": NPC.downedDeerclops = true; break;
                case "Hardmode": WorldGen.StartHardmode(); break;
                case "Post-Pirate Invasion": NPC.downedPirates = true; break;
                case "Post-Queen Slime": NPC.downedQueenSlime = true; break;
                case "Post-The Twins": NPC.downedMechBoss2 = NPC.downedMechBossAny = true; break;
                case "Post-Old One's Army Tier 2": DD2Event.DownedInvasionT2 = true; break;
                case "Post-The Destroyer": NPC.downedMechBoss1 = NPC.downedMechBossAny = true; break;
                case "Post-Skeletron Prime": NPC.downedMechBoss3 = NPC.downedMechBossAny = true; break;
                case "Post-Plantera": NPC.downedPlantBoss = true; break;
                case "Post-Golem": NPC.downedGolemBoss = true; break;
                case "Post-Old One's Army Tier 3": DD2Event.DownedInvasionT3 = true; break;
                case "Post-Martian Madness": NPC.downedMartians = true; break;
                case "Post-Duke Fishron": NPC.downedFishron = true; break;
                case "Post-Mourning Wood": NPC.downedHalloweenTree = true; break;
                case "Post-Pumpking": NPC.downedHalloweenKing = true; break;
                case "Post-Everscream": NPC.downedChristmasTree = true; break;
                case "Post-Santa-NK1": NPC.downedChristmasSantank = true; break;
                case "Post-Ice Queen": NPC.downedChristmasIceQueen = true; break;
                case "Post-Frost Legion": NPC.downedFrost = true; break;
                case "Post-Empress of Light": NPC.downedEmpressOfLight = true; break;
                case "Post-Lunatic Cultist": NPC.downedAncientCultist = true; break;
                case "Post-Lunar Events": NPC.downedTowerNebula = NPC.downedTowerSolar = NPC.downedTowerStardust = NPC.downedTowerVortex = true; break;
                case "Post-Moon Lord": NPC.downedMoonlord = true; break;
                case "Work Bench Unlock": 
                    GiveUnlock(ItemID.WorkBench); 
                    break;
                case "Life Crystal Unlock":
                    GiveUnlock(ItemID.LifeCrystal);
                    break;
                case "Mana Crystal Unlock":
                    GiveUnlock(ItemID.ManaCrystal);
                    break;
                case "Furnace Unlock":
                    GiveUnlock(ItemID.Furnace);
                    break;
                case "Anvil Unlock":
                    GiveUnlock(ItemID.IronAnvil);
                    GiveUnlock(ItemID.LeadAnvil);
                    break;
                case "Silver Pickaxe Unlock":
                    GiveUnlock(ItemID.SilverPickaxe);
                    GiveUnlock(ItemID.TungstenPickaxe);
                    break;
                case "Gold Pickaxe Unlock":
                    GiveUnlock(ItemID.GoldPickaxe);
                    GiveUnlock(ItemID.PlatinumPickaxe);
                    GiveUnlock(ItemID.CnadyCanePickaxe);
                    GiveUnlock(ItemID.BonePickaxe);
                    break;
                case "Fossil Pickaxe Unlock":
                    GiveUnlock(ItemID.FossilPickaxe);
                    break;
                case "Loom Unlock":
                    GiveUnlock(ItemID.Loom);
                    break;
                case "Sawmill Unlock":
                    GiveUnlock(ItemID.Sawmill);
                    break;
                case "Solidifier Unlock":
                    GiveUnlock(ItemID.Solidifier);
                    break;
                case "Nightmare Pickaxe Unlock":
                    GiveUnlock(ItemID.NightmarePickaxe);
                    GiveUnlock(ItemID.DeathbringerPickaxe);
                    GiveUnlock(ItemID.ReaverShark);
                    break;
                case "Hellforge Unlock":
                    GiveUnlock(ItemID.Hellforge);
                    break;
                case "Molten Pickaxe Unlock":
                    GiveUnlock(ItemID.MoltenPickaxe);
                    break;
                case "Tinkerer's Workshop Unlock":
                    GiveUnlock(ItemID.TinkerersWorkshop);
                    break;
                case "Pwnhammer Unlock":
                    GiveUnlock(ItemID.Pwnhammer);
                    break;
                case "Cobalt Pickaxe Unlock":
                    GiveUnlock(ItemID.CobaltPickaxe);
                    GiveUnlock(ItemID.PalladiumPickaxe);
                    GiveUnlock(ItemID.CobaltDrill);
                    GiveUnlock(ItemID.PalladiumDrill);
                    break;
                case "Hardmode Anvil Unlock":
                    GiveUnlock(ItemID.MythrilAnvil);
                    GiveUnlock(ItemID.OrichalcumAnvil);
                    break;
                case "Mythril Pickaxe Unlock":
                    GiveUnlock(ItemID.MythrilPickaxe);
                    GiveUnlock(ItemID.OrichalcumPickaxe);
                    GiveUnlock(ItemID.MythrilDrill);
                    GiveUnlock(ItemID.OrichalcumDrill);
                    break;
                case "Hardmode Forge Unlock":
                    GiveUnlock(ItemID.AdamantiteForge);
                    GiveUnlock(ItemID.TitaniumForge);
                    break;
                case "Adamantite Pickaxe Unlock":
                    GiveUnlock(ItemID.AdamantitePickaxe);
                    GiveUnlock(ItemID.TitaniumPickaxe);
                    GiveUnlock(ItemID.AdamantiteDrill);
                    GiveUnlock(ItemID.TitaniumDrill);
                    break;
                case "Life Fruit Unlock":
                    GiveUnlock(ItemID.LifeFruit);
                    break;
                case "Pickaxe Axe Unlock":
                    GiveUnlock(ItemID.PickaxeAxe);
                    break;
                case "Chlorophyte Pickaxe Unlock":
                    GiveUnlock(ItemID.ChlorophytePickaxe);
                    GiveUnlock(ItemID.ChlorophyteDrill);
                    break;
                case "Chlorophyte Warhammer Unlock":
                    GiveUnlock(ItemID.ChlorophyteWarhammer);
                    break;
                case "Shroomite Digging Claw Unlock":
                    GiveUnlock(ItemID.ShroomiteDiggingClaw);
                    break;
                case "Spectre Pickaxe Unlock":
                    GiveUnlock(ItemID.SpectrePickaxe);
                    break;
                case "Drax Unlock":
                    GiveUnlock(ItemID.Drax);
                    break;
                case "The Axe Unlock":
                    GiveUnlock(ItemID.TheAxe);
                    break;
                case "Autohammer Unlock":
                    GiveUnlock(ItemID.Autohammer);
                    break;
                case "Spectre Hamaxe Unlock":
                    GiveUnlock(ItemID.SpectreHamaxe);
                    break;
                case "Picksaw Unlock":
                    GiveUnlock(ItemID.Picksaw);
                    GiveUnlock(ItemID.LaserDrill);
                    break;
                case "Ancient Manipulator Unlock":
                    GiveUnlock(ItemID.LunarCraftingStation);
                    break;
                case "Luminite Pickaxe Unlock":
                    GiveUnlock(ItemID.VortexPickaxe);
                    GiveUnlock(ItemID.NebulaPickaxe);
                    GiveUnlock(ItemID.SolarFlarePickaxe);
                    GiveUnlock(ItemID.StardustPickaxe);
                    GiveUnlock(ItemID.VortexDrill);
                    GiveUnlock(ItemID.NebulaDrill);
                    GiveUnlock(ItemID.SolarFlareDrill);
                    GiveUnlock(ItemID.StardustDrill);
                    break;
                case "Luminite Hamaxe Unlock":
                    GiveUnlock(ItemID.LunarHamaxeNebula);
                    GiveUnlock(ItemID.LunarHamaxeSolar);
                    GiveUnlock(ItemID.LunarHamaxeStardust);
                    GiveUnlock(ItemID.LunarHamaxeVortex);
                    break;
                case "Reward: Coins":
                    var flagCount = 0;
                    foreach (var flag in flags) if (flag()) flagCount++;
                    var silver = flagCount switch
                    {
                        0 => 15,
                        1 => 20,
                        2 => 25,
                        3 => 30,
                        4 => 40,
                        5 => 50,
                        6 => 70,
                        7 => 100,
                        8 => 150,
                        9 => 200,
                        10 => 250,
                        11 => 300,
                        12 => 400,
                        13 => 500,
                        14 => 700,
                        15 => 1000,
                        16 => 1500,
                        17 => 2000,
                        18 => 2500,
                        19 => 3000,
                        20 => 4000,
                        21 => 5000,
                        22 => 7000,
                        23 => 10000,
                        24 => 15000,
                        25 => 20000,
                        26 => 25000,
                        27 => 30000,
                        28 => 40000,
                        _ => 50000,
                    };
                    GiveItem(ItemID.SilverCoin, silver);
                    break;
                default: Chat($"Received unknown item: {item}"); break;
            }
        }

        public override void PostUpdateWorld()
        {
            if (session == null) return;

            if (!session.Socket.Connected)
            {
                Chat("Disconnected from Archipelago. Reload the world to reconnect.");
                session = null;
                deathlink = null;
                enabled = false;
                return;
            }

            if (!enabled) return;

            var unqueue = new List<int>();
            for (var i = 0; i < locationQueue.Count; i++)
            {
                var status = locationQueue[i].Status;

                if (status switch
                {
                    TaskStatus.RanToCompletion or TaskStatus.Canceled or TaskStatus.Faulted => true,
                    _ => false,
                })
                {
                    if (status == TaskStatus.RanToCompletion) foreach (var item in locationQueue[i].Result.Locations) Chat($"Sent {session.Items.GetItemName(item.Item)} to {session.Players.GetPlayerAlias(item.Player)}!");
                    else Chat("Sent an item to a player...but failed to get info about it!");

                    unqueue.Add(i);
                }
            }

            unqueue.Reverse();
            foreach (var i in unqueue) locationQueue.RemoveAt(i);

            while (session.Items.Any())
            {
                var item = session.Items.DequeueItem();
                var itemName = session.Items.GetItemName(item.Item);

                if (currentItem++ < collectedItems) continue;

                Collect(itemName);

                collectedItems++;
            }

            if (victory) return;

            foreach (var goal in goals) if (!collectedLocations.Contains(goal)) return;

            var victoryPacket = new StatusUpdatePacket()
            {
                Status = ArchipelagoClientState.ClientGoal,
            };
            session.Socket.SendPacket(victoryPacket);

            victory = true;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            tag["ApCollectedItems"] = collectedItems;
            if (enabled) session.DataStorage[Scope.Slot, "CollectedLocations"] = collectedLocations.ToArray();
        }

        public void Reset()
        {
            typeof(SocialAPI).GetField("_mode", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, SocialMode.Steam);

            locationBacklog.Clear();
            locationQueue = null;
            deathlink = null;
            enabled = false;
            currentItem = 0;
            collectedLocations = new List<string>();
            goals = new List<string>();
            victory = false;

            if (session != null) session.Socket.DisconnectAsync();
            session = null;
        }

        public override void OnWorldUnload()
        {
            collectedItems = 0;
            Reset();
        }

        public string[] Status() => Tuple.Create(session != null, enabled) switch
        {
            (false, _) => new[] {
                @"The world is not connected to Archipelago! Reload the world or run ""/apconnect"".",
                "If you are the host, check your config in the main menu at Workshop > Manage Mods > Config",
                "Or in-game at Settings > Mod Configuration",
            },
            (true, false) => new[] { @"Archipelago is connected but not enabled. Once everyone's joined, run ""/apstart"" to start it." },
            (true, true) => new[] { "Archipelago is active!" },
        };

        public bool Enable()
        {
            if (session == null) return false;

            enabled = true;

            foreach (var location in locationBacklog) QueueLocation(location);
            locationBacklog.Clear();

            return true;
        }

        public bool SendCommand(string command)
        {
            if (session == null) return false;

            var packet = new SayPacket()
            {
                Text = command,
            };
            session.Socket.SendPacket(packet);

            return true;
        }

        public string[] DebugInfo()
        {
            var info = new List<string>();

            if (locationBacklog.Count > 0)
            {
                info.Add("You have locations in the backlog, which should only be the case if Archipelago is inactive");
                info.Add($"Location backlog: [{string.Join("; ", locationBacklog)}]");
            }
            else
            {
                info.Add("No locations in the backlog, which is usually normal");
            }

            if (locationQueue.Count > 0)
            {
                info.Add($"You have locations queued for sending. In normal circumstances, these locations will be sent ASAP.");

                var statuses = new List<string>();
                foreach (var location in locationQueue) statuses.Add(location.Status switch
                {
                    TaskStatus.Created => "Created",
                    TaskStatus.WaitingForActivation => "Waiting for activation",
                    TaskStatus.WaitingToRun => "Waiting to run",
                    TaskStatus.Running => "Running",
                    TaskStatus.WaitingForChildrenToComplete => "Waiting for children to complete",
                    TaskStatus.RanToCompletion => "Completed",
                    TaskStatus.Canceled => "Canceled",
                    TaskStatus.Faulted => "Faulted",
                    _ => "Has a status that was added to C# after this code was written",
                });

                info.Add($"Location queue statuses: [{string.Join("; ", statuses)}]");
            }
            else
            {
                info.Add("No locations in the queue, which is usually normal");
            }

            info.Add($"You're {(session == null ? "not " : "")}connected to Archipelago");
            if (session != null && !session.Socket.Connected)
            {
                info.Add("Actually, the socket is disconnected and mod is in a weird state");
            }

            info.Add($"DeathLink is {(deathlink == null ? "dis" : "en")}abled");
            info.Add($"Archipelago is {(enabled ? "en" : "dis")}abled");
            info.Add($"You've collected {collectedItems} items, of which {currentItem} have been applied");
            info.Add($"Collected locations: [{string.Join("; ", collectedLocations)}]");
            info.Add($"Goals: [{string.Join("; ", goals)}]");
            info.Add($"Victory has {(victory ? "been achieved! Hooray!" : "not been achieved. Alas.")}");
            info.Add($"You are slot {slot}");

            return info.ToArray();
        }

        public void Chat(string message, int player = -1)
        {
            if (player == -1)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), Color.White);
                    Console.WriteLine(message);
                }
                else Main.NewText(message);
            }
            else ChatHelper.SendChatMessageToClient(NetworkText.FromLiteral(message), Color.White, player);
        }

        public void Chat(string[] messages, int player = -1)
        {
            foreach (var message in messages) Chat(message, player);
        }

        public void QueueLocation(string locationName)
        {
            if (!enabled)
            {
                locationBacklog.Add(locationName);
                return;
            }

            var location = session.Locations.GetLocationIdFromName("Terraria", locationName);
            if (location == -1 || !session.Locations.AllMissingLocations.Contains(location)) return;

            if (!collectedLocations.Contains(locationName))
            {
                locationQueue.Add(session.Locations.ScoutLocationsAsync(new[] { location }));
                collectedLocations.Add(locationName);
            }

            session.Locations.CompleteLocationChecks(new[] { location });
        }

        public void QueueLocationClient(string locationName)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                QueueLocation(locationName);
                return;
            }

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(locationName);
            packet.Send();
        }

        public void GiveUnlock(int item, int count = 1)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active)
                {
                    player.GetModPlayer<ArchipelagoPlayer>().obtainedItems.Add(ItemID.Search.GetName(item));
                    Console.WriteLine(ItemID.Search.GetName(item));
                }
            }
        }

        public void GiveItem(int item, int count = 1)
        {
            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active)
                {
                    if (item == ItemID.SilverCoin)
                    {
                        var platinum = count / 10000;
                        var gold = count % 10000 / 100;
                        var silver = count % 100;
                        if (platinum > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.PlatinumCoin, platinum);
                        if (gold > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.GoldCoin, gold);
                        if (silver > 0) player.QuickSpawnItem(player.GetSource_GiftOrReward(), ItemID.SilverCoin, silver);
                    }
                    else
                    {
                        player.QuickSpawnItem(player.GetSource_GiftOrReward(), item, count);
                    }
                }
            }
        }

        public void TriggerDeathlink(string message, int player)
        {
            if (deathlink == null) return;

            var death = new DeathLink(session.Players.GetPlayerAlias(slot), message);
            deathlink.SendDeathLink(death);
            ReceiveDeathlink(death);
        }

        public void ReceiveDeathlink(DeathLink death)
        {
            var message = $"[DeathLink] {(death.Source == null ? "" : $"{death.Source} died")}{(death.Source != null && death.Cause != null ? ": " : "")}{(death.Cause == null ? "" : $"{death.Cause}")}";

            for (var i = 0; i < Main.maxPlayers; i++)
            {
                var player = Main.player[i];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }

            if (Main.netMode == NetmodeID.SinglePlayer) return;

            var packet = ModContent.GetInstance<SeldomArchipelago>().GetPacket();
            packet.Write(message);
            packet.Send();
        }
    }
}
