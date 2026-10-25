using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using MysteriousMapMod;
using MysteriousMapMod.Generation;
using MysteriousMapMod.Map;
using MysteriousMapMod.Patches;
using MysteriousMapMod.Rooms;
using MysteriousMapMod.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

var assembly = typeof(MysteriousMapBootstrap).Assembly;
var harmony = new Harmony("local.mysterious_map.compatibility_test");
var patched = new List<string>();
IsaacRelicRegistration.Register();

try
{
    foreach (var type in assembly.GetTypes().Where(type =>
                 type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length > 0))
    {
        var methods = new PatchClassProcessor(harmony, type).Patch();
        if (methods == null || methods.Count == 0)
            throw new InvalidOperationException($"Patch class {type.FullName} did not resolve a target method.");
        patched.Add($"{type.Name}: {string.Join(", ", methods.Select(FormatMethod))}");
    }

    var registeredRelics = new[]
    {
        typeof(MysteriousDataminer),
        typeof(MysteriousGlitchedRelic)
    };
    var poolContent = (IDictionary)AccessTools.Field(typeof(ModHelper), "_moddedContentForPools")
        .GetValue(null)!;
    var sharedContent = poolContent[typeof(SharedRelicPool)]
        ?? throw new InvalidOperationException("The shared relic pool has no mod registrations.");
    var addedTypes = (IEnumerable<Type>)AccessTools.Field(sharedContent.GetType(), "modelsToAdd")
        .GetValue(sharedContent)!;
    if (registeredRelics.Except(addedTypes).Any())
        throw new InvalidOperationException("The Dataminer relics were not registered for the shared relic pool.");
    if (new[] { typeof(MysteriousBlueMap), typeof(MysteriousCompass),
            typeof(MysteriousBogoBombs), typeof(MysteriousPyromaniac),
            typeof(MysteriousIpecac), typeof(MysteriousSacredHeart),
            typeof(MysteriousWafer), typeof(MysteriousHolyMantle),
            typeof(MysteriousNineVolt), typeof(MysteriousHalo) }
        .Intersect(addedTypes).Any())
        throw new InvalidOperationException("Legacy Isaac relics must not appear in new reward pools.");
    foreach (var relicType in registeredRelics)
    {
        var relic = (RelicModel)Activator.CreateInstance(relicType)!;
        const string prefix = "res://MysteriousMap/relics/";
        if (!relic.IconPath.StartsWith(prefix, StringComparison.Ordinal) ||
            !File.Exists(Path.Combine("assets", "relics", Path.GetFileName(relic.IconPath))))
            throw new InvalidOperationException($"{relicType.Name} has no project-owned icon asset.");
    }

    foreach (var (type, property) in new[]
             {
                 (typeof(MysteriousDataminer), nameof(MysteriousDataminer.RoomsCharged)),
                 (typeof(MysteriousDataminer), nameof(MysteriousDataminer.Armed)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.ComboCodes)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.ExternalComboCodes)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.LastMaxHpRoomCount)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.LastInjuryRoomCount)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.LastGrantedRelicRoomCount)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.FlowerTurnCounter)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.FakeFlowerTurnCounter)),
                 (typeof(MysteriousGlitchedRelic), nameof(MysteriousGlitchedRelic.TenAttackCounter))
             })
        if (type.GetProperty(property)?.GetCustomAttribute<SavedPropertyAttribute>() == null)
            throw new InvalidOperationException($"Dataminer save property {type.Name}.{property} is missing.");

    var dataminer = new MysteriousDataminer();
    if (!dataminer.CanArm || !dataminer.TryArm() || dataminer.TryArm())
        throw new InvalidOperationException("Dataminer arming must be single-use until the pickup.");
    dataminer.Consume();
    if (!dataminer.CanArm || dataminer.Armed || dataminer.ShowCounter)
        throw new InvalidOperationException("Dataminer should be reusable without charging or a counter.");
    dataminer.RoomsCharged = 0; // Old saves may contain an empty legacy value.
    if (!dataminer.TryArm())
        throw new InvalidOperationException("Legacy charge must not block a later treasure use.");

    if (!MysteriousMapActMap.ShouldGuaranteeFirstTreasure(0, 1) ||
        !MysteriousMapActMap.ShouldGuaranteeFirstTreasure(0, 2) ||
        MysteriousMapActMap.ShouldGuaranteeFirstTreasure(1, 1) ||
        MysteriousMapActMap.ShouldGuaranteeFirstTreasure(0, 0))
        throw new InvalidOperationException("The opening chest must be guaranteed in the first act for single-player and multiplayer, regardless of the Dataminer setting.");

    for (var previousFights = 0; previousFights < 5; previousFights++)
        if (RoomFactory.ShouldUseWeakEncounter(true, previousFights, 100) != (previousFights < 3) ||
            RoomFactory.ShouldUseWeakEncounter(false, previousFights, 100) ||
            !RoomFactory.ShouldUseWeakEncounter(false, previousFights, 1))
            throw new InvalidOperationException("The first three single-player normal encounters must use the weak pool.");

    for (ulong seed = 1; seed <= 100; seed++)
    {
        var standardStart = new MapPoint(3, 0) { PointType = MapPointType.Ancient };
        var standardBoss = new MapPoint(3, 15) { PointType = MapPointType.Boss };
        var lanes = new List<List<MapPoint>>();
        for (var col = 1; col <= 4; col++)
        {
            var first = new MapPoint(col, 1) { PointType = MapPointType.Monster };
            standardStart.AddChildPoint(first);
            var lane = new List<MapPoint> { first };
            var previous = first;
            for (var row = 2; row < 15; row++)
            {
                var point = new MapPoint(col, row)
                {
                    PointType = col == 1 && row == 7
                        ? MapPointType.Treasure : MapPointType.Monster
                };
                previous.AddChildPoint(point);
                lane.Add(point);
                previous = point;
            }
            previous.AddChildPoint(standardBoss);
            lanes.Add(lane);
        }
        var count = StandardMapTreasures.ApplyToPoints(
            standardStart, standardBoss, 15, seed, 0);
        if (count is < 3 or > 4 ||
            standardStart.Children.Any(point => point.PointType != MapPointType.Treasure) ||
            lanes.Any(lane => lane.Count(point => point.PointType == MapPointType.Treasure) != count))
            throw new InvalidOperationException("Every standard solo route needs the same 3–4 chest rows.");
    }

    for (ulong seed = 1; seed <= 100; seed++)
    {
        var standardMap = new StandardActMap(new Rng(seed, "mysterious_map_test"),
            new Overgrowth(), isMultiplayer: false,
            shouldReplaceTreasureWithElites: false);
        var pathCount = StandardMapTreasures.Apply(standardMap, seed, 0);
        if (pathCount is < 3 or > 4 ||
            standardMap.StartingMapPoint.Children.Any(point =>
                point.PointType != MapPointType.Treasure))
            throw new InvalidOperationException($"Standard map seed {seed} has no complete opening chest row.");
        var reachable = new HashSet<MapPoint>();
        var queue = new Queue<MapPoint>();
        queue.Enqueue(standardMap.StartingMapPoint);
        while (queue.Count > 0)
        {
            var point = queue.Dequeue();
            if (!reachable.Add(point)) continue;
            foreach (var child in point.Children) queue.Enqueue(child);
        }
        var chestRows = reachable.Where(point => point.PointType == MapPointType.Treasure)
            .GroupBy(point => point.coord.row).ToArray();
        if (chestRows.Length != pathCount || chestRows.Any(row =>
                reachable.Where(point => point.coord.row == row.Key)
                    .Any(point => point.PointType != MapPointType.Treasure)))
            throw new InvalidOperationException($"Standard map seed {seed} has a partial chest row.");
    }

    var testStartRoute = MysteriousMapActMap.BuildStandardRoute(
        new GridCell(2, 2), OuterRoomKind.Start, 123UL,
        guaranteedFirstTreasure: true, startingMapPoint: new MapPoint(0, 0));
    var testStart = testStartRoute[InnerNodeRole.Entry];
    var firstRoom = testStartRoute[InnerNodeRole.ChoiceLeft];
    if (testStart.Children.Count != 1 || testStart.Children.Single() != firstRoom ||
        firstRoom.PointType != MapPointType.Treasure ||
        !firstRoom.Children.Contains(testStartRoute[InnerNodeRole.ChoiceRight]))
        throw new InvalidOperationException("Dataminer test runs must reach a treasure room first.");
    var normalStartRoute = MysteriousMapActMap.BuildStandardRoute(
        new GridCell(2, 2), OuterRoomKind.Start, 123UL,
        guaranteedFirstTreasure: false, startingMapPoint: new MapPoint(0, 0));
    if (normalStartRoute[InnerNodeRole.Entry].Children.Count != 2)
        throw new InvalidOperationException("Normal start routes must retain both initial branches.");

    var glitch = new MysteriousGlitchedRelic
    {
        ComboCodes = "2:11:3|3:15:100|14:9:25|invalid"
    };
    var parsed = (IEnumerable<GlitchCombination>)AccessTools.Method(
        typeof(MysteriousGlitchedRelic), "ParseCombinations").Invoke(glitch, null)!;
    if (parsed.Count() != 3 ||
        parsed.All(combo => combo.Trigger != GlitchTrigger.Exhaust ||
                            combo.Outcome != GlitchOutcome.Victory))
        throw new InvalidOperationException("Glitch combinations did not survive encoding.");
    if (parsed.First().ToString() != "2:11")
        throw new InvalidOperationException("Legacy chance must be discarded when a combo is saved again.");
    var initialCounts = Enumerable.Range(0, 35)
        .Select(MysteriousGlitchedRelic.RollInitialCombinationCount).ToArray();
    if (initialCounts.Min() != 3 || initialCounts.Max() != 8 ||
        Enumerable.Range(3, 6).Any(count => !initialCounts.Contains(count)) ||
        Enumerable.Range(3, 5).Any(count =>
            initialCounts.Count(item => item == count) <
            initialCounts.Count(item => item == count + 1)))
        throw new InvalidOperationException("Each Dataminer pickup needs 3–8 increasingly rare links.");
    glitch.ComboCodes = string.Join('|', Enumerable.Repeat("1:1", 3)
        .Concat(Enumerable.Range(1, 12).Select(i => $"1:{i}")));
    var limited = (IEnumerable<GlitchCombination>)AccessTools.Method(
        typeof(MysteriousGlitchedRelic), "ParseCombinations").Invoke(glitch, null)!;
    if (limited.Count() != 5)
        throw new InvalidOperationException("Repeated glitch links must stack while preserving the two-result condition cap.");
    glitch.ComboCodes = string.Join('|', Enumerable.Repeat("1:1", 20));
    var stacked = (IEnumerable<GlitchCombination>)AccessTools.Method(
        typeof(MysteriousGlitchedRelic), "ParseCombinations").Invoke(glitch, null)!;
    if (stacked.Count() != 20)
        throw new InvalidOperationException("Repeated Dataminer pickups must not have a total link cap.");
    glitch.ComboCodes = "1:1|1:2|2:1";
    var noMesh = (IEnumerable<GlitchCombination>)AccessTools.Method(
        typeof(MysteriousGlitchedRelic), "ParseCombinations").Invoke(glitch, null)!;
    if (noMesh.Count() != 2)
        throw new InvalidOperationException("Glitch links must be 1:1, 1:2 or 2:1, never a larger mesh.");
    if (!glitch.PackedIconPath.StartsWith("res://MysteriousMap/relics/", StringComparison.Ordinal))
        throw new InvalidOperationException("The glitched relic must use original mod artwork.");
    if (GlitchInteractionRegistry.AllTriggers.Count(item => item.Id.StartsWith("vanilla.")) != 180 ||
        GlitchInteractionRegistry.AllOutcomes.Count(item => item.Id.StartsWith("vanilla.")) != 180)
        throw new InvalidOperationException("The 180 vanilla relic fragments were not registered.");
    foreach (var name in new[] { "jewelry_box", "sere_talon" })
    {
        var result = GlitchInteractionRegistry.AllOutcomes.First(item =>
            item.Id == $"vanilla.{name}.result");
        if (!result.RequiresCombat)
            throw new InvalidOperationException($"{name} must generate a temporary combat card, not alter the permanent deck.");
    }
    if (GlitchInteractionRegistry.AllOutcomes.Count(item =>
            item.Id.StartsWith("curse.") && item.IsDrawback) != 12)
        throw new InvalidOperationException("The 12 original curse drawbacks were not registered.");
    if (GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.divine_destiny.result") ||
        !GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Regent(),
            "vanilla.divine_destiny.result"))
        throw new InvalidOperationException("Regent-only Star fragments leaked into other characters.");
    if (GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.ninja_scroll.result") ||
        GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Silent(),
            "vanilla.radiant_pearl.result"))
        throw new InvalidOperationException("Character-specific card fragments leaked into other pools.");
    if (GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.paels_horn.result") ||
        GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.electric_shrymp.result") ||
        GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.paels_growth.result") ||
        GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.funerary_mask.result") ||
        GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Ironclad(),
            "vanilla.mini_regent.result") ||
        !GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Necrobinder(),
            "vanilla.paels_tooth.result") ||
        !GlitchCharacterPool.Allows(new MegaCrit.Sts2.Core.Models.Characters.Regent(),
            "vanilla.mini_regent.result"))
        throw new InvalidOperationException("Character-specific fragments leaked into another pool.");

    var compatibility = AccessTools.Method(typeof(MysteriousGlitchedRelic), "IsCompatible");
    if (Enum.GetValues<GlitchTrigger>().Length != 88 ||
        Enum.GetValues<GlitchOutcome>().Length != 80)
        throw new InvalidOperationException("Dataminer should expose 88 triggers and 80 built-in outcomes.");
    if (GlitchInteractionRegistry.AllTriggers.First(item =>
            item.Id == "vanilla.ring_of_the_drake.condition").BuiltInSource !=
        GlitchTrigger.FirstThreeTurnsCombat ||
        GlitchInteractionRegistry.AllTriggers.First(item =>
            item.Id == "vanilla.fake_happy_flower.condition").BuiltInSource !=
        GlitchTrigger.EveryFifthTurn ||
        GlitchInteractionRegistry.AllTriggers.First(item =>
            item.Id == "vanilla.lucky_fysh.condition").BuiltInSource !=
        GlitchTrigger.CardAddedToDeck ||
        GlitchInteractionRegistry.AllTriggers.First(item =>
            item.Id == "vanilla.rainbow_ring.condition").BuiltInSource !=
        GlitchTrigger.AllThreeTypesTurn)
        throw new InvalidOperationException("New multi-turn vanilla conditions were not mapped.");
    if (!(bool)compatibility.Invoke(null,
            [GlitchTrigger.CardGenerated, GlitchOutcome.CharacterCard])!)
        throw new InvalidOperationException("Generated-card feedback must be a valid combination.");
    if (GlitchCardGeneration.HandPosition != CardPilePosition.Bottom)
        throw new InvalidOperationException("Generated hand cards need the game's valid Bottom position.");

    var ironclad = new Ironclad();
    var silent = new Silent();
    var defect = new Defect();
    var regent = new Regent();
    if (GlitchCharacterPool.Allows(ironclad, GlitchOutcome.Poison) ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.snecko_skull.condition") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.data_disk.result") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.cracked_core.result") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.runic_capacitor.result") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.symbiotic_virus.result") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.lunar_pastry.result") ||
        GlitchCharacterPool.Allows(ironclad, "vanilla.ring_of_the_snake.result") ||
        !GlitchCharacterPool.Allows(silent, GlitchOutcome.Poison) ||
        !GlitchCharacterPool.Allows(silent, "vanilla.snecko_skull.condition") ||
        !GlitchCharacterPool.Allows(silent, "vanilla.ring_of_the_snake.result") ||
        !GlitchCharacterPool.Allows(defect, "vanilla.data_disk.result") ||
        !GlitchCharacterPool.Allows(defect, "vanilla.cracked_core.result") ||
        !GlitchCharacterPool.Allows(defect, "vanilla.symbiotic_virus.result") ||
        !GlitchCharacterPool.Allows(regent, "vanilla.lunar_pastry.result") ||
        GlitchCharacterPool.Allows(defect, GlitchOutcome.Poison))
        throw new InvalidOperationException("Character-specific glitch pools leaked poison or Focus.");
    if (GlitchRandomBuffs.Pool.Length < 25 ||
        GlitchRandomBuffs.Pool.Select(item => item.PowerType).Distinct().Count() !=
            GlitchRandomBuffs.Pool.Length ||
        GlitchRandomBuffs.Pool.Any(item => item.Amount <= 0 ||
            Activator.CreateInstance(item.PowerType) is not PowerModel power ||
            power.Type != PowerType.Buff) ||
        !GlitchRandomBuffs.Pool.Any(item => item.PowerType.Name == "DemonFormPower") ||
        !GlitchRandomBuffs.Pool.Any(item => item.PowerType.Name == "EchoFormPower") ||
        !GlitchRandomBuffs.Pool.Any(item => item.PowerType.Name == "EnvenomPower"))
        throw new InvalidOperationException("Cross-character card buff pool is incomplete or unsafe.");
    if (GlitchRandomDebuffs.EnemyPool.Length < 8 ||
        GlitchRandomDebuffs.SelfPool.Length < 14 ||
        GlitchRandomDebuffs.EnemyPool.Concat(GlitchRandomDebuffs.SelfPool)
            .Any(item => item.Amount <= 0 ||
                Activator.CreateInstance(item.PowerType) is not PowerModel power ||
                power.Type != PowerType.Debuff) ||
        !(bool)compatibility.Invoke(null,
            [GlitchTrigger.CardPlayed, GlitchOutcome.RandomEnemyDebuff])! ||
        (bool)compatibility.Invoke(null,
            [GlitchTrigger.RestHealed, GlitchOutcome.RandomSelfDebuff])!)
        throw new InvalidOperationException("Random debuffs must be typed and combat-only.");
    if (GlitchRandomEnemyBuffs.Pool.Length < 10 ||
        GlitchRandomEnemyBuffs.Pool.Select(item => item.PowerType).Distinct().Count() !=
            GlitchRandomEnemyBuffs.Pool.Length ||
        GlitchRandomEnemyBuffs.Pool.Any(item => item.Amount <= 0 ||
            Activator.CreateInstance(item.PowerType) is not PowerModel power ||
            power.Type != PowerType.Buff) ||
        !GlitchRandomEnemyBuffs.Pool.Any(item => item.PowerType == typeof(SoarPower)) ||
        !GlitchRandomEnemyBuffs.Pool.Any(item => item.PowerType == typeof(IntangiblePower)) ||
        GlitchRandomEnemyBuffs.Pool.Any(item => item.PowerType == typeof(FlutterPower)) ||
        !(bool)compatibility.Invoke(null,
            [GlitchTrigger.CardPlayed, GlitchOutcome.RandomEnemyBuff])! ||
        (bool)compatibility.Invoke(null,
            [GlitchTrigger.RestHealed, GlitchOutcome.RandomEnemyBuff])!)
        throw new InvalidOperationException("Enemy buff pool must be generic and combat-only.");

    var limiter = new GlitchCascadeLimiter();
    if (!limiter.TryEnter(true))
        throw new InvalidOperationException("A fresh cascade must start.");
    var generated = 0;
    while (limiter.TrySpend(GlitchOutcome.CharacterCard, true)) generated++;
    if (generated != GlitchCascadeLimiter.MaxGeneratedCardsPerCascade)
        throw new InvalidOperationException("Generated-card feedback was not bounded.");
    limiter.Exit();
    for (var depth = 0; depth < GlitchCascadeLimiter.MaxDepth; depth++)
        if (!limiter.TryEnter(true))
            throw new InvalidOperationException("Feedback stopped before five nested triggers.");
    if (limiter.TryEnter(true))
        throw new InvalidOperationException("Feedback exceeded five nested triggers.");
    for (var depth = 0; depth < GlitchCascadeLimiter.MaxDepth; depth++)
        limiter.Exit();
    if (!limiter.TryEnter(true) || !limiter.TrySpend(GlitchOutcome.CharacterCard, true))
        throw new InvalidOperationException("A later action must start a fresh cascade.");
    limiter.Exit();

    var teamTriggers = new[] { GlitchTrigger.TeammateHurt, GlitchTrigger.TeammateDamage,
        GlitchTrigger.TeammateBlockGained, GlitchTrigger.TeammateBuffGained };
    var teamOutcomes = new[] { GlitchOutcome.TeammateDamage, GlitchOutcome.TeammateInjury,
        GlitchOutcome.TeammateCard, GlitchOutcome.TeammateStrength,
        GlitchOutcome.TeammateBlock, GlitchOutcome.TeammateHeal,
        GlitchOutcome.TeammateEndTurn, GlitchOutcome.TeammateRandomBuff };
    if (teamTriggers.Any(trigger => GlitchTeamEffects.Allows(1, trigger) ||
            !GlitchTeamEffects.Allows(2, trigger)) ||
        teamOutcomes.Any(outcome => GlitchTeamEffects.Allows(1, outcome) ||
            !GlitchTeamEffects.Allows(2, outcome)))
        throw new InvalidOperationException("Teammate effects must be multiplayer-only.");
    if (!(bool)compatibility.Invoke(null,
            [GlitchTrigger.TeammateHurt, GlitchOutcome.TeammateDamage])! ||
        (bool)compatibility.Invoke(null,
            [GlitchTrigger.ShopEntered, GlitchOutcome.TeammateDamage])! ||
        (bool)compatibility.Invoke(null,
            [GlitchTrigger.TeammateBlockGained, GlitchOutcome.TeammateStrength])!)
        throw new InvalidOperationException("Teammate effects must respect combat/context requirements.");
    var teamLimiter = new GlitchCascadeLimiter();
    teamLimiter.TryEnter(true);
    var teamCards = 0;
    while (teamLimiter.TrySpend(GlitchOutcome.TeammateCard, true)) teamCards++;
    if (teamCards != GlitchCascadeLimiter.MaxGeneratedCardsPerCascade)
        throw new InvalidOperationException("Teammate card feedback exceeded its cap.");
    teamLimiter.Exit();

    GlitchInteractionRegistry.RegisterTrigger("compat_test.generated", () => "Generated", true, false);
    GlitchInteractionRegistry.RegisterOutcome("compat_test.block", () => "Block", true, false,
        _ => Task.CompletedTask);
    glitch.ExternalComboCodes = "compat_test.generated>compat_test.block>35|core.t30>compat_test.block>10";
    var externalParsed = (IEnumerable<ExternalGlitchCombination>)AccessTools.Method(
        typeof(MysteriousGlitchedRelic), "ParseExternalCombinations").Invoke(glitch, null)!;
    if (externalParsed.Count() != 2)
        throw new InvalidOperationException("Registered mod combinations did not survive encoding.");
    if (externalParsed.First().ToString() != "compat_test.generated>compat_test.block")
        throw new InvalidOperationException("Legacy external chance must be discarded on save.");
    foreach (var trigger in Enum.GetValues<GlitchTrigger>())
        if (!Enum.GetValues<GlitchOutcome>().Any(outcome =>
                (bool)compatibility.Invoke(null, [trigger, outcome])!))
            throw new InvalidOperationException($"Glitch trigger {trigger} has no valid outcomes.");
}
finally
{
    harmony.UnpatchAll(harmony.Id);
}

Console.WriteLine($"Resolved and temporarily applied {patched.Count} Harmony patch classes against the installed sts2.dll:");
foreach (var item in patched)
    Console.WriteLine($"  {item}");

var requiredMultiplayerPatches = new[]
{
    "SyncMysteriousMapModeIntoLobbyPatch",
    "MysteriousMapMultiplayerMarkerTransportPatch",
    "RestoreMysteriousMapMultiplayerMarkerAtRunCreationPatch",
    "RecognizeNewMultiplayerModePatch",
    "SaveMultiplayerMapModePatch",
    "RestoreMultiplayerMapModePatch",
    "PreserveMultiplayerMapModeDuringCanonicalizationPatch",
    "ValidateMysteriousMapVotePatch",
    "ValidateMysteriousMapTravelPatch",
    "InitializeShopInventoryMultiplayerSyncPatch",
    "DisposeShopInventoryMultiplayerSyncPatch"
};
foreach (var required in requiredMultiplayerPatches)
{
    if (!patched.Any(item => item.StartsWith(required + ":", StringComparison.Ordinal)))
        throw new InvalidOperationException($"Required multiplayer patch {required} was not applied.");
}
foreach (var required in new[]
         {
             "SaveHiddenRoomRevealsPatch",
             "RestoreHiddenRoomRevealsPatch",
             "PreserveHiddenRoomRevealsDuringCanonicalizationPatch",
             "MysteriousMapUnknownRoomTypePatch"
         })
{
    if (!patched.Any(item => item.StartsWith(required + ":", StringComparison.Ordinal)))
        throw new InvalidOperationException($"Required hidden-room patch {required} was not applied.");
}

AssertMember(AccessTools.Method(typeof(StartRunLobby), "BeginRunForAllPlayers"),
    "StartRunLobby.BeginRunForAllPlayers");
AssertMember(AccessTools.Method(typeof(StartRunLobby), "BeginRunLocally"),
    "StartRunLobby.BeginRunLocally");
AssertMember(AccessTools.Method(typeof(NGame), nameof(NGame.StartNewMultiplayerRun)),
    "NGame.StartNewMultiplayerRun");
AssertMember(AccessTools.Field(typeof(MapSelectionSynchronizer), "_runState"),
    "MapSelectionSynchronizer._runState");
AssertMember(typeof(NetVoteForMapCoordAction).GetField(nameof(NetVoteForMapCoordAction.source)),
    "NetVoteForMapCoordAction.source");
AssertMember(typeof(NetVoteForMapCoordAction).GetField(nameof(NetVoteForMapCoordAction.destination)),
    "NetVoteForMapCoordAction.destination");
AssertMember(typeof(NetMoveToMapCoordAction).GetField(nameof(NetMoveToMapCoordAction.destination)),
    "NetMoveToMapCoordAction.destination");
var spoilsCoordSetter = AccessTools.PropertySetter(typeof(SpoilsMap), nameof(SpoilsMap.SpoilsCoord));
AssertMember(spoilsCoordSetter, "SpoilsMap.SpoilsCoord setter");
var spoilsMap = new SpoilsMap();
var spoilsCoord = new MegaCrit.Sts2.Core.Map.MapCoord(4, 5);
spoilsCoordSetter!.Invoke(spoilsMap, [spoilsCoord]);
if (spoilsMap.SpoilsCoord != spoilsCoord)
    throw new InvalidOperationException("Spoils Map target coordinate could not be reassigned.");
AssertMember(AccessTools.Field(typeof(NPotionPopup), "_useButton"), "NPotionPopup._useButton");
AssertMember(AccessTools.Field(typeof(NPotionPopup), "_holder"), "NPotionPopup._holder");
AssertMember(AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary"), "NMapScreen._mapPointDictionary");
AssertMember(AccessTools.Field(typeof(NMapPoint), "_controllerSelectionReticle"),
    "NMapPoint._controllerSelectionReticle");
AssertMember(AccessTools.PropertyGetter(typeof(Player), "CanUseOrRemovePotions"),
    "Player.CanUseOrRemovePotions getter");
AssertMember(AccessTools.PropertyGetter(typeof(NClickableControl), "IsFocused"), "NClickableControl.IsFocused getter");
AssertMember(AccessTools.PropertyGetter(typeof(NControllerManager), "InputType"),
    "NControllerManager.InputType getter");
if (!MapPotionTargeting.IsSupportedTargetType(TargetType.AnyEnemy) ||
    !MapPotionTargeting.IsSupportedTargetType(TargetType.AnyPlayer) ||
    !MapPotionTargeting.IsSupportedTargetType(TargetType.AnyAlly) ||
    !MapPotionTargeting.IsSupportedTargetType(TargetType.TargetedNoCreature) ||
    MapPotionTargeting.IsSupportedTargetType(TargetType.Self) ||
    MapPotionTargeting.IsSupportedTargetType(TargetType.AllEnemies))
{
    throw new InvalidOperationException("Map potion targeting accepts the wrong TargetType set.");
}
if (new MysteriousMapMultiplayerMarkerModifier().ShouldReceiveCombatHooks)
    throw new InvalidOperationException("The multiplayer marker must never receive combat hooks.");
var markerCreatedFallback = typeof(MysteriousMapMultiplayerMarkerModifier).GetMethod(
    "AfterRunCreated", BindingFlags.Instance | BindingFlags.NonPublic);
var markerLoadedFallback = typeof(MysteriousMapMultiplayerMarkerModifier).GetMethod(
    "AfterRunLoaded", BindingFlags.Instance | BindingFlags.NonPublic);
if (markerCreatedFallback?.DeclaringType != typeof(MysteriousMapMultiplayerMarkerModifier) ||
    markerLoadedFallback?.DeclaringType != typeof(MysteriousMapMultiplayerMarkerModifier))
{
    throw new InvalidOperationException(
        "The multiplayer marker has no native run-created/run-loaded activation fallback.");
}
var hostRevealSettings = new MysteriousMapRevealSettings(
    Mind: true,
    Compass: false,
    TreasureMap: true,
    BlueMap: true);
var revealMask = hostRevealSettings.ToMask();
if (MysteriousMapRevealSettings.FromMask(revealMask) != hostRevealSettings)
    throw new InvalidOperationException("Host reveal-setting mask did not round-trip.");
var decodedMarkerProperties = new SavedProperties
{
    ints =
    [
        new SavedProperties.SavedProperty<int>(
            nameof(MysteriousMapMultiplayerMarkerModifier.RevealOptionsMask), revealMask)
    ],
    bools =
    [
        new SavedProperties.SavedProperty<bool>(
            nameof(MysteriousMapMultiplayerMarkerModifier.StartWithDataminer), true)
    ]
};
var decodedMarker = new MysteriousMapMultiplayerMarkerModifier();
decodedMarkerProperties.Fill(decodedMarker);
if (decodedMarker.RevealOptionsMask != revealMask || !decodedMarker.StartWithDataminer ||
    !MysteriousMapRunMode.ReadRuntimeStartingDataminer([decodedMarker]))
    throw new InvalidOperationException("Host run settings did not survive lobby packet serialization.");
if (typeof(MysteriousMapMultiplayerMarkerModifier)
        .GetProperty(nameof(MysteriousMapMultiplayerMarkerModifier.StartWithDataminer))?
        .GetCustomAttribute<SavedPropertyAttribute>() == null)
    throw new InvalidOperationException("Starting Dataminer is not a saved lobby marker property.");
var transportLobby = (StartRunLobby)RuntimeHelpers.GetUninitializedObject(typeof(StartRunLobby));
var ordinaryModifier = new MysteriousMapHiddenRoomStateModifier();
var listenerModifiers = new List<ModifierModel> { ordinaryModifier, decodedMarker };
MysteriousMapMultiplayerMarkerTransportPatch.CaptureForLobbyListener(
    transportLobby, ref listenerModifiers, writeLog: false);
if (listenerModifiers.Count != 1 || !ReferenceEquals(listenerModifiers[0], ordinaryModifier))
    throw new InvalidOperationException(
        "The transient map-mode carrier was not removed before the standard lobby listener.");
var runCreationModifiers = MysteriousMapMultiplayerMarkerTransportPatch.ReattachForRunCreation(
    transportLobby, Array.Empty<ModifierModel>(), writeLog: false);
if (runCreationModifiers.Count != 1 || !ReferenceEquals(runCreationModifiers[0], decodedMarker))
    throw new InvalidOperationException(
        "The transient map-mode carrier was not restored at multiplayer run creation.");
if (MysteriousMapMultiplayerMarkerTransportPatch.ReattachForRunCreation(
        transportLobby, Array.Empty<ModifierModel>(), writeLog: false).Count != 0)
{
    throw new InvalidOperationException("The transient multiplayer carrier was not consumed exactly once.");
}
if (new MysteriousMapHiddenRoomStateModifier().ShouldReceiveCombatHooks)
    throw new InvalidOperationException("The hidden-room save marker must never receive combat hooks.");
var explicitlyHiddenPoint = new MegaCrit.Sts2.Core.Map.MapPoint(2, 3) { CanBeModified = false };
var pollutedOrdinaryPoint = new MegaCrit.Sts2.Core.Map.MapPoint(3, 3) { CanBeModified = true };
MysteriousMapRules.MarkHidden(explicitlyHiddenPoint);
if (!MysteriousMapRules.IsHiddenRoom(explicitlyHiddenPoint) ||
    MysteriousMapRules.IsHiddenRoom(pollutedOrdinaryPoint))
{
    throw new InvalidOperationException(
        "Hidden-room identity still depends on the vanilla CanBeModified working flag.");
}
var unknownShopCoord = new MapCoord(5, 6);
var otherCoord = new MapCoord(6, 6);
var unknownShopHistory = new MapPointHistoryEntry
{
    MapPointType = MapPointType.Unknown,
    Rooms = [new MapPointRoomHistoryEntry { RoomType = RoomType.Shop }]
};
var otherHistory = new MapPointHistoryEntry
{
    MapPointType = MapPointType.Unknown,
    Rooms = [new MapPointRoomHistoryEntry { RoomType = RoomType.Event }]
};
if (!MysteriousMapRules.HasResolvedShopHistory(
        unknownShopCoord,
        [unknownShopCoord, otherCoord, unknownShopCoord],
        [unknownShopHistory, otherHistory]) ||
    MysteriousMapRules.HasResolvedShopHistory(
        otherCoord,
        [unknownShopCoord, otherCoord],
        [unknownShopHistory, otherHistory]))
{
    throw new InvalidOperationException(
        "Question-mark shop history was not preserved across the pre-history repeat-entry window.");
}
Console.WriteLine("Multiplayer lobby marker, vote action, and host-selected movement contracts resolved.");
Console.WriteLine("Standard multiplayer character-select marker transport passed.");
Console.WriteLine("Question-mark shop repeat-entry history detection passed.");

var savedCard = new SerializableCard
{
    Id = new ModelId("cards", "mysterious_map_codec_test"),
    CurrentUpgradeLevel = 2,
    Props = new SavedProperties
    {
        ints = [new SavedProperties.SavedProperty<int>("CodecTest", 7)]
    }
};
var characterCards = Enumerable.Range(0, 5)
    .Select(index => new ShopCardEntrySnapshot(index == 0 ? savedCard : null, 50 + index, index == 0, index == 0))
    .ToArray();
var colorlessCards = Enumerable.Range(0, 2)
    .Select(index => new ShopCardEntrySnapshot(null, 75 + index, false, false))
    .ToArray();
var frozenRelicIds = Enumerable.Range(0, 3)
    .Select(index => new ModelId("relics", $"mysterious_map_frozen_{index}").ToString())
    .ToArray();
var inventory = new ShopInventorySnapshot(42, characterCards, colorlessCards,
    frozenRelicIds.Select((id, index) => new ShopEntrySnapshot(id, 100 + index, true)).ToArray(),
    Enumerable.Range(0, 3).Select(index => new ShopEntrySnapshot(null, 60 + index, false)).ToArray(),
    75, true);
var payload = ShopInventoryPersistence.Encode([new ShopRoomSnapshot(1, 4, 6, [inventory])]);
var decoded = ShopInventoryPersistence.Decode(payload).Single();
var decodedCard = decoded.Inventories.Single().CharacterCards[0].Card;
if (decoded.ActIndex != 1 || decoded.Col != 4 || decoded.Row != 6 ||
    decodedCard?.Id != savedCard.Id || decodedCard.CurrentUpgradeLevel != 2 ||
    decodedCard.Props?.ints?.Single().value != 7 ||
    !decoded.Inventories.Single().Relics.Select(item => item.ModelId).SequenceEqual(frozenRelicIds))
{
    throw new InvalidOperationException("Shop snapshot codec did not preserve its payload.");
}
var stagedRevisit = new ReusableMerchantRoom(decoded);
if (stagedRevisit.HasInventory || stagedRevisit.PendingSnapshot != decoded)
    throw new InvalidOperationException("A shop revisit did not stage its frozen inventory snapshot.");
var revisedSnapshot = decoded with { Row = 7 };
stagedRevisit.ReplacePendingSnapshot(revisedSnapshot);
if (stagedRevisit.PendingSnapshot != revisedSnapshot)
    throw new InvalidOperationException("A staged shop revisit could not accept a newer synchronized snapshot.");
Console.WriteLine("Shop snapshot codec round-trip passed.");

var remoteInventory = inventory with { PlayerId = 84, CardRemovalUsed = false };
var hostReplica = remoteInventory with { CardRemovalUsed = false, CardRemovalCost = 100 };
var remoteAuthority = remoteInventory with { CardRemovalUsed = true, CardRemovalCost = 125 };
var merged = ShopInventoryPersistence.MergeInventories(
    new ShopRoomSnapshot(1, 4, 6, [inventory, hostReplica]), [remoteAuthority]);
if (merged.Inventories.Single(item => item.PlayerId == inventory.PlayerId) != inventory ||
    merged.Inventories.Single(item => item.PlayerId == remoteAuthority.PlayerId) != remoteAuthority)
{
    throw new InvalidOperationException("Authoritative shop inventory merge replaced the wrong player.");
}

var syncPayload = ShopInventoryPersistence.EncodeInventoryForSync(remoteAuthority);
var message = new MysteriousMapShopInventorySyncMessage
{
    ActIndex = 1,
    Col = 4,
    Row = 6,
    Revision = 12,
    InventoryPayload = syncPayload
};
var writer = new PacketWriter { WarnOnGrow = false };
message.Serialize(writer);
var reader = new PacketReader();
reader.Reset(writer.Buffer);
var decodedMessage = new MysteriousMapShopInventorySyncMessage();
decodedMessage.Deserialize(reader);
var decodedInventory = ShopInventoryPersistence.DecodeInventoryFromSync(decodedMessage.InventoryPayload);
if (decodedMessage.ActIndex != message.ActIndex || decodedMessage.Col != message.Col ||
    decodedMessage.Row != message.Row || decodedMessage.Revision != message.Revision ||
    decodedInventory.PlayerId != remoteAuthority.PlayerId ||
    decodedInventory.CardRemovalCost != remoteAuthority.CardRemovalCost ||
    decodedInventory.CardRemovalUsed != remoteAuthority.CardRemovalUsed ||
    decodedInventory.CharacterCards.Count != remoteAuthority.CharacterCards.Count ||
    decodedInventory.ColorlessCards.Count != remoteAuthority.ColorlessCards.Count ||
    decodedInventory.Relics.Count != remoteAuthority.Relics.Count ||
    decodedInventory.Potions.Count != remoteAuthority.Potions.Count ||
    !decodedMessage.ShouldBroadcast)
{
    throw new InvalidOperationException("Multiplayer shop inventory message did not preserve its payload.");
}
Console.WriteLine("Per-player authoritative shop merge and multiplayer message round-trip passed.");

var potionMessage = new MysteriousMapMapPotionConsumedMessage
{
    ActIndex = 2,
    Col = 7,
    Row = 8,
    PotionSlot = 3,
    PotionId = new ModelId("potions", "mysterious_map_target_test")
};
var potionWriter = new PacketWriter { WarnOnGrow = false };
potionMessage.Serialize(potionWriter);
var potionReader = new PacketReader();
potionReader.Reset(potionWriter.Buffer);
var decodedPotionMessage = new MysteriousMapMapPotionConsumedMessage();
decodedPotionMessage.Deserialize(potionReader);
if (decodedPotionMessage.ActIndex != potionMessage.ActIndex ||
    decodedPotionMessage.Col != potionMessage.Col ||
    decodedPotionMessage.Row != potionMessage.Row ||
    decodedPotionMessage.PotionSlot != potionMessage.PotionSlot ||
    decodedPotionMessage.PotionId != potionMessage.PotionId ||
    !decodedPotionMessage.ShouldBroadcast || decodedPotionMessage.ShouldBuffer)
{
    throw new InvalidOperationException("Map-targeted potion consumption message did not preserve its payload.");
}
Console.WriteLine("Map-targeted potion consumption multiplayer message round-trip passed.");

var armedMessage = new MysteriousMapDataminerArmedMessage
    { PlayerId = 17, ActIndex = 2, RoomCol = 4, RoomRow = 5 };
var armedWriter = new PacketWriter { WarnOnGrow = false };
armedMessage.Serialize(armedWriter);
var armedReader = new PacketReader();
armedReader.Reset(armedWriter.Buffer);
var decodedArmed = new MysteriousMapDataminerArmedMessage();
decodedArmed.Deserialize(armedReader);
if (decodedArmed.PlayerId != armedMessage.PlayerId ||
    decodedArmed.ActIndex != armedMessage.ActIndex ||
    decodedArmed.RoomCol != armedMessage.RoomCol || decodedArmed.RoomRow != armedMessage.RoomRow ||
    !decodedArmed.ShouldBroadcast)
    throw new InvalidOperationException("Dataminer arming message did not preserve its owner and act.");

var comboMessage = new MysteriousMapDataminerCombinationMessage
{
    PlayerId = 17,
    ActIndex = 2,
    RoomCol = 4,
    RoomRow = 5,
    SourceRelicId = new ModelId("relics", "mysterious_map_test"),
    ComboCodes = "1:2|3:4",
    ExternalComboCodes = "other.test>core.o2"
};
var comboWriter = new PacketWriter { WarnOnGrow = false };
comboMessage.Serialize(comboWriter);
var comboReader = new PacketReader();
comboReader.Reset(comboWriter.Buffer);
var decodedCombo = new MysteriousMapDataminerCombinationMessage();
decodedCombo.Deserialize(comboReader);
if (decodedCombo.PlayerId != comboMessage.PlayerId ||
    decodedCombo.ActIndex != comboMessage.ActIndex ||
    decodedCombo.RoomCol != comboMessage.RoomCol ||
    decodedCombo.RoomRow != comboMessage.RoomRow ||
    decodedCombo.SourceRelicId != comboMessage.SourceRelicId ||
    decodedCombo.ComboCodes != comboMessage.ComboCodes ||
    decodedCombo.ExternalComboCodes != comboMessage.ExternalComboCodes)
    throw new InvalidOperationException("Dataminer combination message did not preserve its owner and effects.");

var bombMessage = new MysteriousMapBombActionMessage
{
    PlayerId = 17, Sequence = 9, ActIndex = 2,
    SourceCol = 4, SourceRow = 5, Kind = 2, DeltaCol = -1, RoundNumber = 3, Amount = 2
};
var bombWriter = new PacketWriter { WarnOnGrow = false };
bombMessage.Serialize(bombWriter);
var bombReader = new PacketReader();
bombReader.Reset(bombWriter.Buffer);
var decodedBomb = new MysteriousMapBombActionMessage();
decodedBomb.Deserialize(bombReader);
if (decodedBomb.PlayerId != bombMessage.PlayerId ||
    decodedBomb.Sequence != bombMessage.Sequence || decodedBomb.ActIndex != bombMessage.ActIndex ||
    decodedBomb.SourceCol != bombMessage.SourceCol || decodedBomb.SourceRow != bombMessage.SourceRow ||
    decodedBomb.Kind != bombMessage.Kind || decodedBomb.DeltaCol != bombMessage.DeltaCol ||
    decodedBomb.DeltaRow != bombMessage.DeltaRow || decodedBomb.RoundNumber != bombMessage.RoundNumber ||
    decodedBomb.Amount != bombMessage.Amount)
    throw new InvalidOperationException("Bomb action message did not preserve its owner and coordinates.");
Console.WriteLine("Dataminer and bomb multiplayer message round-trips passed.");

static string FormatMethod(MethodBase method) => $"{method.DeclaringType?.Name}.{method.Name}";

static void AssertMember(MemberInfo? member, string name)
{
    if (member == null)
        throw new MissingMemberException(name);
}
