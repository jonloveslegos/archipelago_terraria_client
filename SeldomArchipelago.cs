using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using SeldomArchipelago.Systems;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

namespace SeldomArchipelago
{
    // TODO
    // Make a new class that holds all of `ArchipelagoSystem`'s state to avoid having to manually reset
    // Use a data-oriented approach to get rid of all this repetition
    public class SeldomArchipelago : Mod
    {
        // Terraria is single-threaded so this *should* be fine
        public bool temp;

        public static MethodInfo desertScourgeHeadOnKill = null;
        public static MethodInfo giantClamOnKill = null; // downedCLAM and downedCLAMHardMode
        public static MethodInfo cragmawMireOnKill = null;
        public static MethodInfo acidRainEventUpdateInvasion = null; // downedEoCAcidRain and downedAquaticScourgeAcidRain
        public static MethodInfo crabulonOnKill = null;
        public static MethodInfo hiveMindOnKill = null;
        public static MethodInfo perforatorHiveOnKill = null;
        public static MethodInfo slimeGodCoreOnKill = null;
        public static MethodInfo calamityGlobalNPCOnKill = null;
        public static MethodInfo aquaticScourgeHeadOnKill = null;
        public static MethodInfo maulerOnKill = null;
        public static MethodInfo brimstoneElementalOnKill = null;
        public static MethodInfo cryogenOnKill = null;
        public static MethodInfo calamitasCloneOnKill = null;
        public static MethodInfo greatSandSharkOnKill = null;
        public static MethodInfo leviathanRealOnKill = null;
        public static MethodInfo astrumAureusOnKill = null;
        public static MethodInfo plaguebringerGoliathOnKill = null;
        public static MethodInfo ravagerBodyOnKill = null;
        public static MethodInfo astrumDeusHeadOnKill = null;
        public static MethodInfo profanedGuardianCommanderOnKill = null;
        public static MethodInfo bumblefuckOnKill = null;
        public static MethodInfo providenceOnKill = null;
        public static MethodInfo stormWeaverHeadOnKill = null;
        public static MethodInfo ceaselessVoidOnKill = null;
        public static MethodInfo signusOnKill = null;
        public static MethodInfo polterghastOnKill = null;
        public static MethodInfo nuclearTerrorOnKill = null;
        public static MethodInfo oldDukeOnKill = null;
        public static MethodInfo devourerofGodsHeadOnKill = null;
        public static MethodInfo yharonOnKill = null;
        public static MethodInfo aresBodyDoMiscDeathEffects = null;
        public static MethodInfo supremeCalamitasOnKill = null;

        public override void Load()
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            // Begin cursed IL editing

            // Torch God reward Terraria.Player:13794
            IL.Terraria.Player.TorchAttack += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.netMode))));
                cursor.EmitDelegate<Action>(() => archipelagoSystem.QueueLocationClient("Torch God"));
                cursor.Emit(OpCodes.Ret);
            };

            // Allow Torch God even if you have `unlockedBiomeTorches`
            IL.Terraria.Player.UpdateTorchLuck_ConsumeCountersAndCalculate += il =>
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(i => i.MatchLdfld(typeof(Player).GetField(nameof(Player.unlockedBiomeTorches))));
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);

                cursor.GotoNext(i => i.MatchLdcI4(ItemID.TorchGodsFavor));
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldc_I4_0);
            };

            // General event clear locations
            IL.Terraria.NPC.SetEventFlagCleared += il =>
            {
                var cursor = new ILCursor(il);

                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate<Action<int>>((int id) =>
                {
                    var location = id switch
                    {
                        GameEventClearedID.DefeatedSlimeKing => "King Slime",
                        GameEventClearedID.DefeatedEyeOfCthulu => "Eye of Cthulhu",
                        GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu => "Evil Boss",
                        GameEventClearedID.DefeatedGoblinArmy => "Goblin Army",
                        GameEventClearedID.DefeatedQueenBee => "Queen Bee",
                        GameEventClearedID.DefeatedSkeletron => "Skeletron",
                        GameEventClearedID.DefeatedDeerclops => "Deerclops",
                        GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode => "Wall of Flesh",
                        GameEventClearedID.DefeatedPirates => "Pirate Invasion",
                        GameEventClearedID.DefeatedQueenSlime => "Queen Slime",
                        GameEventClearedID.DefeatedTheTwins => "The Twins",
                        GameEventClearedID.DefeatedDestroyer => "The Destroyer",
                        GameEventClearedID.DefeatedSkeletronPrime => "Skeletron Prime",
                        GameEventClearedID.DefeatedPlantera => "Plantera",
                        GameEventClearedID.DefeatedGolem => "Golem",
                        GameEventClearedID.DefeatedMartians => "Martian Invasion",
                        GameEventClearedID.DefeatedFishron => "Duke Fishron",
                        GameEventClearedID.DefeatedHalloweenTree => "Mourning Wood",
                        GameEventClearedID.DefeatedHalloweenKing => "Pumpking",
                        GameEventClearedID.DefeatedChristmassTree => "Everscream",
                        GameEventClearedID.DefeatedSantank => "Santa-NK1",
                        GameEventClearedID.DefeatedIceQueen => "Ice Queen",
                        GameEventClearedID.DefeatedFrostArmy => "Frost Legion",
                        GameEventClearedID.DefeatedEmpressOfLight => "Empress of Light",
                        GameEventClearedID.DefeatedAncientCultist => "Lunatic Cultist",
                        GameEventClearedID.DefeatedMoonlord => "Moon Lord",
                        _ => null,
                    };

                    if (location != null) archipelagoSystem.QueueLocation(location);
                });
                cursor.Emit(OpCodes.Ret);
            };

            // Old One's Army locations
            IL.Terraria.GameContent.Events.DD2Event.WinInvasionInternal += il =>
            {
                var cursor = new ILCursor(il);

                foreach (var (flagName, tier) in new Tuple<string, int>[] {
                    Tuple.Create(nameof(DD2Event.DownedInvasionT1), 1),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT2), 2),
                    Tuple.Create(nameof(DD2Event.DownedInvasionT3), 3),
                })
                {
                    var flag = typeof(DD2Event).GetField(flagName);
                    cursor.GotoNext(i => i.MatchStsfld(flag));
                    cursor.EmitDelegate<Action>(() => temp = (bool)flag.GetValue(null));
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() =>
                    {
                        flag.SetValue(null, temp);
                        archipelagoSystem.QueueLocation($"Old One's Army Tier {tier}");
                    });
                }
            };

            IL.Terraria.NPC.DoDeathEvents += il =>
            {
                var cursor = new ILCursor(il);

                // Prevent NPC.downedMechBossAny from being set
                while (cursor.TryGotoNext(i => i.MatchStsfld(typeof(NPC).GetField(nameof(NPC.downedMechBossAny)))))
                {
                    cursor.EmitDelegate<Action>(() => temp = NPC.downedMechBossAny);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() => NPC.downedMechBossAny = temp);
                }

                // Prevent Hardmode generation Terraria.NPC:69104
                cursor.GotoNext(i => i.MatchCall(typeof(WorldGen).GetMethod(nameof(WorldGen.StartHardmode))));
                cursor.EmitDelegate<Action>(() =>
                {
                    temp = Main.hardMode;
                    Main.hardMode = true;
                });
                cursor.Index++;
                cursor.EmitDelegate<Action>(() => Main.hardMode = temp);
            };

            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted += OnAchievementCompleted;

            // Unmaintainable reflection

            var calamity = ModLoader.GetMod("CalamityMod");
            if (calamity == null) return;

            var calamityAssembly = calamity.GetType().Assembly;
            foreach (var type in calamityAssembly.GetTypes()) switch (type.Name)
                {
                    case "DesertScourgeHead": desertScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GiantClam": giantClamOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CragmawMire": cragmawMireOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AcidRainEvent": acidRainEventUpdateInvasion = type.GetMethod("UpdateInvasion", BindingFlags.Static | BindingFlags.Public); break;
                    case "Crabulon": crabulonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "HiveMind": hiveMindOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PerforatorHive": perforatorHiveOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "SlimeGodCore": slimeGodCoreOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamityGlobalNPC": calamityGlobalNPCOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AquaticScourgeHead": aquaticScourgeHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Mauler": maulerOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "BrimstoneElemental": brimstoneElementalOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Cryogen": cryogenOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CalamitasClone": calamitasCloneOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "GreatSandShark": greatSandSharkOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Leviathan": leviathanRealOnKill = type.GetMethod("RealOnKill", BindingFlags.Static | BindingFlags.Public); break;
                    case "AstrumAureus": astrumAureusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "PlaguebringerGoliath": plaguebringerGoliathOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "RavagerBody": ravagerBodyOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AstrumDeusHead": astrumDeusHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "ProfanedGuardianCommander": profanedGuardianCommanderOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Bumblefuck": bumblefuckOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Providence": providenceOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "StormWeaverHead": stormWeaverHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "CeaselessVoid": ceaselessVoidOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Signus": signusOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Polterghast": polterghastOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "NuclearTerror": nuclearTerrorOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "OldDuke": oldDukeOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "DevourerofGodsHead": devourerofGodsHeadOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "Yharon": yharonOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                    case "AresBody": aresBodyDoMiscDeathEffects = type.GetMethod("DoMiscDeathEffects", BindingFlags.Static | BindingFlags.Public); break;
                    case "SupremeCalamitas": supremeCalamitasOnKill = type.GetMethod("OnKill", BindingFlags.Instance | BindingFlags.Public); break;
                }

            onDesertScourgeHeadOnKill += OnDesertScourgeHeadOnKill;
            onGiantClamOnKill += OnGiantClamOnKill;
            onCragmawMireOnKill += OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion += EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill += OnCrabulonOnKill;
            onHiveMindOnKill += OnHiveMindOnKill;
            onPerforatorHiveOnKill += OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill += OnSlimeGodCoreOnKill;
            editCalamityGlobalNPCOnKill += EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill += OnAquaticScourgeHeadOnKill;
            onMaulerOnKill += OnMaulerOnKill;
            onBrimstoneElementalOnKill += OnBrimstoneElementalOnKill;
            onCryogenOnKill += OnCryogenOnKill;
            onCalamitasCloneOnKill += OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill += OnGreatSandSharkOnKill;
            onLeviathanRealOnKill += OnLeviathanRealOnKill;
            onAstrumAureusOnKill += OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill += OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill += OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill += OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill += OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill += OnBumblefuckOnKill;
            onProvidenceOnKill += OnProvidenceOnKill;
            onStormWeaverHeadOnKill += OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill += OnCeaselessVoidOnKill;
            onSignusOnKill += OnSignusOnKill;
            onPolterghastOnKill += OnPolterghastOnKill;
            onNuclearTerrorOnKill += OnNuclearTerrorOnKill;
            onOldDukeOnKill += OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill += OnDevourerofGodsHeadOnKill;
            onYharonOnKill += OnYharonOnKill;
            onAresBodyDoMiscDeathEffects += OnAresBodyDoMiscDeathEffects;
            onSupremeCalamitasOnKill += OnSupremeCalamitasOnKill;
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            var message = reader.ReadString();
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();

            if (message == "") archipelagoSystem.Chat(archipelagoSystem.Status(), whoAmI);
            else if (message.StartsWith("deathlink")) archipelagoSystem.TriggerDeathlink(message.Substring(9), whoAmI);
            else if (message.StartsWith("[DeathLink]"))
            {
                var player = Main.player[Main.myPlayer];
                if (player.active && !player.dead) player.Hurt(PlayerDeathReason.ByCustomReason(message), 999999, 1);
            }
            else archipelagoSystem.QueueLocation(message);
        }

        public override void Unload()
        {
            if (Main.netMode != NetmodeID.Server) Main.Achievements.OnAchievementCompleted -= OnAchievementCompleted;

            if (ModLoader.GetMod("CalamityMod") == null) return;

            onDesertScourgeHeadOnKill -= OnDesertScourgeHeadOnKill;
            onGiantClamOnKill -= OnGiantClamOnKill;
            onCragmawMireOnKill -= OnCragmawMireOnKill;
            editAcidRainEventUpdateInvasion -= EditAcidRainEventUpdateInvasion;
            onCrabulonOnKill -= OnCrabulonOnKill;
            onHiveMindOnKill -= OnHiveMindOnKill;
            onPerforatorHiveOnKill -= OnPerforatorHiveOnKill;
            onSlimeGodCoreOnKill -= OnSlimeGodCoreOnKill;
            editCalamityGlobalNPCOnKill -= EditCalamityGlobalNPCOnKill;
            onAquaticScourgeHeadOnKill -= OnAquaticScourgeHeadOnKill;
            onMaulerOnKill -= OnMaulerOnKill;
            onBrimstoneElementalOnKill -= OnBrimstoneElementalOnKill;
            onCryogenOnKill -= OnCryogenOnKill;
            onCalamitasCloneOnKill -= OnCalamitasCloneOnKill;
            onGreatSandSharkOnKill -= OnGreatSandSharkOnKill;
            onLeviathanRealOnKill -= OnLeviathanRealOnKill;
            onAstrumAureusOnKill -= OnAstrumAureusOnKill;
            onPlaguebringerGoliathOnKill -= OnPlaguebringerGoliathOnKill;
            onRavagerBodyOnKill -= OnRavagerBodyOnKill;
            onAstrumDeusHeadOnKill -= OnAstrumDeusHeadOnKill;
            onProfanedGuardianCommanderOnKill -= OnProfanedGuardianCommanderOnKill;
            onBumblefuckOnKill -= OnBumblefuckOnKill;
            onProvidenceOnKill -= OnProvidenceOnKill;
            onStormWeaverHeadOnKill -= OnStormWeaverHeadOnKill;
            onCeaselessVoidOnKill -= OnCeaselessVoidOnKill;
            onSignusOnKill -= OnSignusOnKill;
            onPolterghastOnKill -= OnPolterghastOnKill;
            onNuclearTerrorOnKill -= OnNuclearTerrorOnKill;
            onOldDukeOnKill -= OnOldDukeOnKill;
            onDevourerofGodsHeadOnKill -= OnDevourerofGodsHeadOnKill;
            onYharonOnKill -= OnYharonOnKill;
            onAresBodyDoMiscDeathEffects -= OnAresBodyDoMiscDeathEffects;
            onSupremeCalamitasOnKill -= OnSupremeCalamitasOnKill;
        }

        void OnAchievementCompleted(Achievement achievement)
        {
            var name = achievement.Name switch
            {
                "TIMBER" => "Timber!!",
                "BENCHED" => "Benched",
                "OBTAIN_HAMMER" => "Stop! Hammer Time!",
                "MATCHING_ATTIRE" => "Matching Attire",
                "FASHION_STATEMENT" => "Fashion Statement",
                "OOO_SHINY" => "Ooo! Shiny!",
                "NO_HOBO" => "No Hobo",
                "HEAVY_METAL" => "Heavy Metal",
                "FREQUENT_FLYER" => "The Frequent Flyer",
                "DYE_HARD" => "Dye Hard",
                "LUCKY_BREAK" => "Lucky Break",
                "STAR_POWER" => "Star Power",
                "YOU_CAN_DO_IT" => "You Can Do It!",
                "TURN_GNOME_TO_STATUE" => "Heliophobia",
                "ARCHAEOLOGIST" => "Archaeologist",
                "PET_THE_PET" => "Feeling Petty",
                "FLY_A_KITE_ON_A_WINDY_DAY" => "A Rather Blustery Day",
                "PRETTY_IN_PINK" => "Pretty in Pink",
                "MARATHON_MEDALIST" => "Marathon Medalist",
                "SERVANT_IN_TRAINING" => "Servant-in-Training",
                "GOOD_LITTLE_SLAVE" => "10 Fishing Quests",
                "TROUT_MONKEY" => "Trout Monkey",
                "GLORIOUS_GOLDEN_POLE" => "Glorious Golden Pole",
                "FAST_AND_FISHIOUS" => "Fast and Fishious",
                "SUPREME_HELPER_MINION" => "Supreme Helper Minion!",
                "INTO_ORBIT" => "Into Orbit",
                "WATCH_YOUR_STEP" => "Watch Your Step!",
                "THROWING_LINES" => "Throwing Lines",
                "VEHICULAR_MANSLAUGHTER" => "Vehicular Manslaughter",
                "FIND_A_FAIRY" => "Hey! Listen!",
                "I_AM_LOOT" => "I Am Loot!",
                "HEART_BREAKER" => "Heart Breaker",
                "HOLD_ON_TIGHT" => "Hold on Tight!",
                "LIKE_A_BOSS" => "Like a Boss",
                "JEEPERS_CREEPERS" => "Jeepers Creepers",
                "DECEIVER_OF_FOOLS" => "Deceiver of Fools",
                "DIE_TO_DEAD_MANS_CHEST" => "Dead Men Tell No Tales",
                "BULLDOZER" => "Bulldozer",
                "THERE_ARE_SOME_WHO_CALL_HIM" => "There are Some Who Call Him...",
                "ITS_GETTING_HOT_IN_HERE" => "It's Getting Hot in Here",
                "ROCK_BOTTOM" => "Rock Bottom",
                "SMASHING_POPPET" => "Smashing, Poppet!",
                "TALK_TO_NPC_AT_MAX_HAPPINESS" => "Leading Landlord",
                "COMPLETELY_AWESOME" => "Completely Awesome",
                "STICKY_SITUATION" => "Sticky Situation",
                "THE_CAVALRY" => "The Cavalry",
                "BLOODBATH" => "Bloodbath",
                "TIL_DEATH" => "Til Death...",
                "FOUND_GRAVEYARD" => "Quiet Neighborhood",
                "THROW_A_PARTY" => "Jolly Jamboree",
                "MINER_FOR_FIRE" => "Miner for Fire",
                "GO_LAVA_FISHING" => "Hot Reels!",
                "GET_TERRASPARK_BOOTS" => "Boots of the Hero",
                "WHERES_MY_HONEY" => "Where's My Honey?",
                "NOT_THE_BEES" => "Not the Bees!",
                "DUNGEON_HEIST" => "Dungeon Heist",
                "BEGONE_EVIL" => "Begone, Evil!",
                "EXTRA_SHINY" => "Extra Shiny!",
                "HEAD_IN_THE_CLOUDS" => "Head in the Clouds",
                "GELATIN_WORLD_TOUR" => "Gelatin World Tour",
                "DEFEAT_DREADNAUTILUS" => "Don't Dread on Me",
                "PRISMANCER" => "Prismancer",
                "GET_A_LIFE" => "Get a Life",
                "TOPPED_OFF" => "Topped Off",
                "BUCKETS_OF_BOLTS" => "Buckets of Bolts",
                "MECHA_MAYHEM" => "Mecha Mayhem",
                "DRAX_ATTAX" => "Drax Attax",
                "PHOTOSYNTHESIS" => "Photosynthesis",
                "FUNKYTOWN" => "Funkytown",
                "IT_CAN_TALK" => "It Can Talk?!",
                "REAL_ESTATE_AGENT" => "Real Estate Agent",
                "ROBBING_THE_GRAVE" => "Robbing the Grave",
                "BIG_BOOTY" => "Big Booty",
                "RAINBOWS_AND_UNICORNS" => "Rainbows and Unicorns",
                "TEMPLE_RAIDER" => "Temple Raider",
                "SWORD_OF_THE_HERO" => "Sword of the Hero",
                "KILL_THE_SUN" => "Kill the Sun",
                "BALEFUL_HARVEST" => "Baleful Harvest",
                "ICE_SCREAM" => "Ice Scream",
                "SLAYER_OF_WORLDS" => "Slayer of Worlds",
                "SICK_THROW" => "Sick Throw",
                "YOU_AND_WHAT_ARMY" => "You and What Army?",
                _ => null,
            };

            if (name != null) ModContent.GetInstance<ArchipelagoSystem>().QueueLocationClient(name);
        }

        delegate void OnKill(ModNPC self);

        void OnDesertScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Desert Scourge");
        }

        void OnGiantClamOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Giant Clam");
        }

        void OnCragmawMireOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cragmaw Mire");
        }

        void EditAcidRainEventUpdateInvasion(ILContext il)
        {
            var archipelagoSystem = ModContent.GetInstance<ArchipelagoSystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdarg(0));
            cursor.Index++;
            cursor.EmitDelegate<Action<bool>>(won =>
            {
                if (won)
                {
                    archipelagoSystem.QueueLocation("Post-Acid Rain Tier 1");
                    if (CalamityMod.DownedBossSystem.downedAquaticScourge) archipelagoSystem.QueueLocation("Post-Acid Rain Tier 2");
                }
            });
            cursor.Emit(OpCodes.Ldc_I4_0);
        }

        void OnCrabulonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Crabulon");
        }

        void OnHiveMindOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Hive Mind");
        }

        void OnPerforatorHiveOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Perforator Hive");
        }

        void OnSlimeGodCoreOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Slime God");
        }

        void EditCalamityGlobalNPCOnKill(ILContext il)
        {
            var seldomArchipelago = ModContent.GetInstance<ArchipelagoSystem>();
            var cursor = new ILCursor(il);

            cursor.GotoNext(i => i.MatchLdcI4(NPCID.BloodNautilus));
            cursor.GotoNext(i => i.MatchLdcI4(NPCID.BloodNautilus));
            cursor.EmitDelegate<Action<int>>(npc =>
            {
                if (npc == NPCID.BloodNautilus) seldomArchipelago.QueueLocation("Dreadnautilus");
            });
            cursor.Emit(OpCodes.Ldc_I4_M1);
        }

        void OnAquaticScourgeHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Aquatic Scourge");
        }

        void OnMaulerOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Mauler");
        }

        void OnBrimstoneElementalOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Brimstone Elemental");
        }

        void OnCryogenOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Cryogen");
        }

        void OnCalamitasCloneOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Calamitas Clone");
        }

        void OnGreatSandSharkOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Great Sand Shark");
        }

        delegate void RealOnKill(NPC npc);
        void OnLeviathanRealOnKill(RealOnKill orig, NPC npc)
        {
            if (temp) orig(npc);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Leviathan and Anahita");
        }

        void OnAstrumAureusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Aureus");
        }

        void OnPlaguebringerGoliathOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Plaguebringer Goliath");
        }

        void OnRavagerBodyOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ravager");
        }

        void OnAstrumDeusHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Astrum Deus");
        }

        void OnProfanedGuardianCommanderOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Profaned Guardians");
        }

        void OnBumblefuckOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Dragonfolly");
        }

        void OnProvidenceOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Providence, the Profaned Goddess");
        }

        void OnStormWeaverHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Storm Weaver");
        }

        void OnCeaselessVoidOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Ceaseless Void");
        }

        void OnSignusOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Signus, Envoy of the Devourer");
        }

        void OnPolterghastOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Polterghast");
        }

        void OnNuclearTerrorOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Nuclear Terror");
        }

        void OnOldDukeOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Old Duke");
        }

        void OnDevourerofGodsHeadOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("The Devourer of Gods");
        }

        void OnYharonOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Yharon, Dragon of Rebirth");
        }

        delegate void DoMiscDeathEffects(NPC npc, int mechType);
        void OnAresBodyDoMiscDeathEffects(DoMiscDeathEffects orig, NPC npc, int mechType)
        {
            if (temp) orig(npc, mechType);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Exo Mechs");
        }

        void OnSupremeCalamitasOnKill(OnKill orig, ModNPC self)
        {
            if (temp) orig(self);
            else ModContent.GetInstance<ArchipelagoSystem>().QueueLocation("Supreme Witch, Calamitas");
        }

        delegate void OnOnKill(OnKill orig, ModNPC self);

        static event OnOnKill onDesertScourgeHeadOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(desertScourgeHeadOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(desertScourgeHeadOnKill, value);
        }

        static event OnOnKill onGiantClamOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(giantClamOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(giantClamOnKill, value);
        }

        static event OnOnKill onCragmawMireOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(cragmawMireOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(cragmawMireOnKill, value);
        }

        static event ILContext.Manipulator editAcidRainEventUpdateInvasion
        {
            add => HookEndpointManager.Modify(acidRainEventUpdateInvasion, value);
            remove => HookEndpointManager.Unmodify(acidRainEventUpdateInvasion, value);
        }

        static event OnOnKill onCrabulonOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(crabulonOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(crabulonOnKill, value);
        }

        static event OnOnKill onHiveMindOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(hiveMindOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(hiveMindOnKill, value);
        }

        static event OnOnKill onPerforatorHiveOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(perforatorHiveOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(perforatorHiveOnKill, value);
        }

        static event OnOnKill onSlimeGodCoreOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(slimeGodCoreOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(slimeGodCoreOnKill, value);
        }

        static event ILContext.Manipulator editCalamityGlobalNPCOnKill
        {
            add => HookEndpointManager.Modify(calamityGlobalNPCOnKill, value);
            remove => HookEndpointManager.Unmodify(calamityGlobalNPCOnKill, value);
        }

        static event OnOnKill onAquaticScourgeHeadOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(aquaticScourgeHeadOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(aquaticScourgeHeadOnKill, value);
        }

        static event OnOnKill onMaulerOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(maulerOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(maulerOnKill, value);
        }

        static event OnOnKill onBrimstoneElementalOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(brimstoneElementalOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(brimstoneElementalOnKill, value);
        }

        static event OnOnKill onCryogenOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(cryogenOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(cryogenOnKill, value);
        }

        static event OnOnKill onCalamitasCloneOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(calamitasCloneOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(calamitasCloneOnKill, value);
        }

        static event OnOnKill onGreatSandSharkOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(greatSandSharkOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(greatSandSharkOnKill, value);
        }

        delegate void OnRealOnKill(RealOnKill orig, NPC npc);
        static event OnRealOnKill onLeviathanRealOnKill
        {
            add => HookEndpointManager.Add<OnRealOnKill>(leviathanRealOnKill, value);
            remove => HookEndpointManager.Remove<OnRealOnKill>(leviathanRealOnKill, value);
        }

        static event OnOnKill onAstrumAureusOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(astrumAureusOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(astrumAureusOnKill, value);
        }

        static event OnOnKill onPlaguebringerGoliathOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(plaguebringerGoliathOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(plaguebringerGoliathOnKill, value);
        }

        static event OnOnKill onRavagerBodyOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(ravagerBodyOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(ravagerBodyOnKill, value);
        }

        static event OnOnKill onAstrumDeusHeadOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(astrumDeusHeadOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(astrumDeusHeadOnKill, value);
        }

        static event OnOnKill onProfanedGuardianCommanderOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(profanedGuardianCommanderOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(profanedGuardianCommanderOnKill, value);
        }

        static event OnOnKill onBumblefuckOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(bumblefuckOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(bumblefuckOnKill, value);
        }

        static event OnOnKill onProvidenceOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(providenceOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(providenceOnKill, value);
        }

        static event OnOnKill onStormWeaverHeadOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(stormWeaverHeadOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(stormWeaverHeadOnKill, value);
        }

        static event OnOnKill onCeaselessVoidOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(ceaselessVoidOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(ceaselessVoidOnKill, value);
        }

        static event OnOnKill onSignusOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(signusOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(signusOnKill, value);
        }

        static event OnOnKill onPolterghastOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(polterghastOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(polterghastOnKill, value);
        }

        static event OnOnKill onNuclearTerrorOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(nuclearTerrorOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(nuclearTerrorOnKill, value);
        }

        static event OnOnKill onOldDukeOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(oldDukeOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(oldDukeOnKill, value);
        }

        static event OnOnKill onDevourerofGodsHeadOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(devourerofGodsHeadOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(devourerofGodsHeadOnKill, value);
        }

        static event OnOnKill onYharonOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(yharonOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(yharonOnKill, value);
        }

        delegate void OnDoMiscDeathEffects(DoMiscDeathEffects orig, NPC npc, int mechType);
        static event OnDoMiscDeathEffects onAresBodyDoMiscDeathEffects
        {
            add => HookEndpointManager.Add<OnDoMiscDeathEffects>(aresBodyDoMiscDeathEffects, value);
            remove => HookEndpointManager.Remove<OnDoMiscDeathEffects>(aresBodyDoMiscDeathEffects, value);
        }

        static event OnOnKill onSupremeCalamitasOnKill
        {
            add => HookEndpointManager.Add<OnOnKill>(supremeCalamitasOnKill, value);
            remove => HookEndpointManager.Remove<OnOnKill>(supremeCalamitasOnKill, value);
        }
    }
}
