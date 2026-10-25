using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MysteriousMapMod.Rooms;

/// <summary>
/// A save-only transport model. It is removed from RunState.Modifiers immediately
/// after loading and never participates in gameplay hooks or modifier UI.
/// </summary>
public sealed class MysteriousMapShopSnapshotModifier : ModifierModel
{
    [SavedProperty]
    public string ShopSnapshot { get; set; } = string.Empty;

    public override bool ShouldReceiveCombatHooks => false;
}

internal sealed record ShopEntrySnapshot(string? ModelId, int Cost, bool Stocked);
internal sealed record ShopCardEntrySnapshot(SerializableCard? Card, int Cost, bool Stocked, bool OnSale);
internal sealed record ShopInventorySnapshot(
    ulong PlayerId,
    IReadOnlyList<ShopCardEntrySnapshot> CharacterCards,
    IReadOnlyList<ShopCardEntrySnapshot> ColorlessCards,
    IReadOnlyList<ShopEntrySnapshot> Relics,
    IReadOnlyList<ShopEntrySnapshot> Potions,
    int CardRemovalCost,
    bool CardRemovalUsed);
internal sealed record ShopRoomSnapshot(
    int ActIndex,
    int Col,
    int Row,
    IReadOnlyList<ShopInventorySnapshot> Inventories);
internal sealed record ShopSnapshotEnvelope(int Schema, IReadOnlyList<ShopRoomSnapshot> Shops);
internal sealed record ShopInventorySyncEnvelope(int Schema, ShopInventorySnapshot Inventory);

internal static class ShopInventoryPersistence
{
    private const int SchemaVersion = 1;
    private const string PayloadProperty = nameof(MysteriousMapShopSnapshotModifier.ShopSnapshot);

    private static readonly FieldInfo CostField = AccessTools.Field(typeof(MerchantEntry), "_cost");
    private static readonly FieldInfo CardCreationField = AccessTools.Field(typeof(MerchantCardEntry), "<CreationResult>k__BackingField");
    private static readonly FieldInfo CardSaleField = AccessTools.Field(typeof(MerchantCardEntry), "<IsOnSale>k__BackingField");
    private static readonly FieldInfo CharacterCardsField = AccessTools.Field(typeof(MerchantInventory), "_characterCardEntries");
    private static readonly FieldInfo ColorlessCardsField = AccessTools.Field(typeof(MerchantInventory), "_colorlessCardEntries");
    private static readonly FieldInfo RelicsField = AccessTools.Field(typeof(MerchantInventory), "_relicEntries");
    private static readonly FieldInfo PotionsField = AccessTools.Field(typeof(MerchantInventory), "_potionEntries");
    private static readonly FieldInfo CardRemovalField = AccessTools.Field(typeof(MerchantInventory), "<CardRemovalEntry>k__BackingField");
    private static readonly FieldInfo RelicModelField = AccessTools.Field(typeof(MerchantRelicEntry), "<Model>k__BackingField");
    private static readonly FieldInfo PotionModelField = AccessTools.Field(typeof(MerchantPotionEntry), "<Model>k__BackingField");
    private static readonly MethodInfo UpdateEntriesMethod = AccessTools.Method(typeof(MerchantInventory), "UpdateEntries");

    private static readonly CardType[] CharacterCardTypes =
    [
        CardType.Attack, CardType.Attack, CardType.Skill, CardType.Skill, CardType.Power
    ];

    private static readonly CardRarity[] ColorlessCardRarities =
    [
        CardRarity.Uncommon, CardRarity.Rare
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        IncludeFields = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static ShopRoomSnapshot Capture(int actIndex, MapCoord coord, ReusableMerchantRoom room)
    {
        var inventories = room.Inventories.Select(CaptureInventory).ToArray();
        return new ShopRoomSnapshot(actIndex, coord.col, coord.row, inventories);
    }

    internal static ShopInventorySnapshot CaptureInventoryForPlayer(
        ReusableMerchantRoom room,
        ulong playerId)
    {
        var inventory = room.Inventories.SingleOrDefault(item => item.Player.NetId == playerId)
            ?? throw new InvalidOperationException($"The reusable shop has no inventory for player {playerId}.");
        return CaptureInventory(inventory);
    }

    internal static void ReleaseTransientCards(RunState runState, ReusableMerchantRoom room)
    {
        // A staged revisit reloads the exact serialized card objects. Remove the
        // unbought originals first so RunState does not accumulate duplicate
        // floating cards (bought cards have already left their merchant entry).
        foreach (var card in room.Inventories
                     .SelectMany(inventory => inventory.CardEntries)
                     .Select(entry => entry.CreationResult?.Card)
                     .OfType<CardModel>())
        {
            if (runState.ContainsCard(card))
                runState.RemoveCard(card);
        }
    }

    internal static ShopRoomSnapshot MergeInventories(
        ShopRoomSnapshot room,
        IEnumerable<ShopInventorySnapshot> replacements)
    {
        var byPlayer = replacements.ToDictionary(item => item.PlayerId);
        if (byPlayer.Count == 0)
            return room;

        var merged = room.Inventories
            .Select(item => byPlayer.GetValueOrDefault(item.PlayerId) ?? item)
            .ToArray();
        return room with { Inventories = merged };
    }

    internal static string EncodeInventoryForSync(ShopInventorySnapshot inventory)
    {
        return JsonSerializer.Serialize(
            new ShopInventorySyncEnvelope(SchemaVersion, inventory), JsonOptions);
    }

    internal static ShopInventorySnapshot DecodeInventoryFromSync(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidDataException("The synchronized shop inventory payload is empty.");

        var envelope = JsonSerializer.Deserialize<ShopInventorySyncEnvelope>(payload, JsonOptions)
            ?? throw new InvalidDataException("The synchronized shop inventory payload is empty.");
        if (envelope.Schema != SchemaVersion)
            throw new InvalidDataException($"Unsupported synchronized shop inventory schema {envelope.Schema}.");
        return envelope.Inventory
            ?? throw new InvalidDataException("The synchronized shop inventory is missing.");
    }

    internal static string Encode(IEnumerable<ShopRoomSnapshot> shops)
    {
        var ordered = shops.OrderBy(shop => shop.ActIndex)
            .ThenBy(shop => shop.Col)
            .ThenBy(shop => shop.Row)
            .ToArray();
        return JsonSerializer.Serialize(new ShopSnapshotEnvelope(SchemaVersion, ordered), JsonOptions);
    }

    internal static IReadOnlyList<ShopRoomSnapshot> Decode(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return Array.Empty<ShopRoomSnapshot>();

        var envelope = JsonSerializer.Deserialize<ShopSnapshotEnvelope>(payload, JsonOptions)
            ?? throw new InvalidDataException("The shop snapshot payload is empty.");
        if (envelope.Schema != SchemaVersion)
            throw new InvalidDataException($"Unsupported shop snapshot schema {envelope.Schema}.");
        return envelope.Shops ?? Array.Empty<ShopRoomSnapshot>();
    }

    internal static SerializableModifier ToSerializableModifier(string payload)
    {
        return new SerializableModifier
        {
            Id = ModelDb.Modifier<MysteriousMapShopSnapshotModifier>().Id,
            Props = new SavedProperties
            {
                strings = [new SavedProperties.SavedProperty<string>(PayloadProperty, payload)]
            }
        };
    }

    internal static string? ReadFrom(SerializableRun save, bool remove)
    {
        var id = ModelDb.Modifier<MysteriousMapShopSnapshotModifier>().Id;
        string? payload = null;
        for (var index = save.Modifiers.Count - 1; index >= 0; index--)
        {
            var modifier = save.Modifiers[index];
            if (modifier.Id != id)
                continue;

            payload ??= modifier.Props?.strings?
                .FirstOrDefault(property => property.name == PayloadProperty).value;
            if (remove)
                save.Modifiers.RemoveAt(index);
        }
        return payload;
    }

    internal static bool TryRestoreInventories(
        RunState runState,
        ShopRoomSnapshot snapshot,
        out List<MerchantInventory> inventories,
        out string? failure)
    {
        inventories = [];
        failure = null;
        var loadedCards = new List<CardModel>();

        try
        {
            var byPlayer = snapshot.Inventories.ToDictionary(item => item.PlayerId);
            if (byPlayer.Count != runState.Players.Count)
                throw new InvalidDataException("The shop inventory player count does not match the run.");

            foreach (var player in runState.Players)
            {
                if (!byPlayer.TryGetValue(player.NetId, out var saved))
                    throw new InvalidDataException($"The shop has no inventory for player {player.NetId}.");
                inventories.Add(RestoreInventory(runState, player, saved, loadedCards));
            }
            return true;
        }
        catch (Exception exception)
        {
            foreach (var card in loadedCards)
            {
                if (runState.ContainsCard(card))
                    runState.RemoveCard(card);
            }
            inventories.Clear();
            failure = exception.Message;
            return false;
        }
    }

    private static ShopInventorySnapshot CaptureInventory(MerchantInventory inventory)
    {
        return new ShopInventorySnapshot(
            inventory.Player.NetId,
            inventory.CharacterCardEntries.Select(CaptureCard).ToArray(),
            inventory.ColorlessCardEntries.Select(CaptureCard).ToArray(),
            inventory.RelicEntries.Select(CaptureRelic).ToArray(),
            inventory.PotionEntries.Select(CapturePotion).ToArray(),
            GetRawCost(inventory.CardRemovalEntry!),
            inventory.CardRemovalEntry?.Used ?? true);
    }

    private static ShopCardEntrySnapshot CaptureCard(MerchantCardEntry entry)
    {
        return new ShopCardEntrySnapshot(
            entry.CreationResult?.Card.ToSerializable(),
            GetRawCost(entry),
            entry.IsStocked,
            entry.IsOnSale);
    }

    private static ShopEntrySnapshot CaptureRelic(MerchantRelicEntry entry)
    {
        return new ShopEntrySnapshot(entry.Model?.Id.ToString(), GetRawCost(entry), entry.IsStocked);
    }

    private static ShopEntrySnapshot CapturePotion(MerchantPotionEntry entry)
    {
        return new ShopEntrySnapshot(entry.Model?.Id.ToString(), GetRawCost(entry), entry.IsStocked);
    }

    private static MerchantInventory RestoreInventory(
        RunState runState,
        Player player,
        ShopInventorySnapshot saved,
        ICollection<CardModel> loadedCards)
    {
        EnsureCount(saved.CharacterCards, CharacterCardTypes.Length, "character cards");
        EnsureCount(saved.ColorlessCards, ColorlessCardRarities.Length, "colorless cards");
        EnsureCount(saved.Relics, 3, "relics");
        EnsureCount(saved.Potions, 3, "potions");

        var inventory = new MerchantInventory(player);
        var characterPool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).ToList();
        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>()
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint).ToList();

        var characterCards = (List<MerchantCardEntry>)CharacterCardsField.GetValue(inventory)!;
        for (var index = 0; index < CharacterCardTypes.Length; index++)
        {
            var entry = new MerchantCardEntry(player, inventory, characterPool, CharacterCardTypes[index]);
            RestoreCard(runState, player, entry, saved.CharacterCards[index], loadedCards);
            characterCards.Add(entry);
        }

        var colorlessCards = (List<MerchantCardEntry>)ColorlessCardsField.GetValue(inventory)!;
        for (var index = 0; index < ColorlessCardRarities.Length; index++)
        {
            var entry = new MerchantCardEntry(player, inventory, colorlessPool, ColorlessCardRarities[index]);
            RestoreCard(runState, player, entry, saved.ColorlessCards[index], loadedCards);
            colorlessCards.Add(entry);
        }

        // Direct constructors calculate a random display cost. Preserve and restore
        // every affected RNG/grab-bag before replacing that cost with the saved one.
        var playerRng = player.PlayerRng.ToSerializable();
        var playerGrabBag = player.RelicGrabBag.ToSerializable();
        var sharedGrabBag = runState.SharedRelicGrabBag.ToSerializable();
        try
        {
            var relics = (List<MerchantRelicEntry>)RelicsField.GetValue(inventory)!;
            foreach (var item in saved.Relics)
            {
                MerchantRelicEntry entry;
                if (item.Stocked && item.ModelId != null)
                {
                    var model = ModelDb.GetById<RelicModel>(ModelId.Deserialize(item.ModelId)).ToMutable();
                    entry = new MerchantRelicEntry(model, player);
                }
                else
                {
                    entry = new MerchantRelicEntry(RelicRarity.Shop, player);
                    RelicModelField.SetValue(entry, null);
                }
                SetRawCost(entry, item.Cost);
                relics.Add(entry);
            }

            var potions = (List<MerchantPotionEntry>)PotionsField.GetValue(inventory)!;
            foreach (var item in saved.Potions)
            {
                MerchantPotionEntry entry;
                if (item.Stocked && item.ModelId != null)
                {
                    var model = ModelDb.GetById<PotionModel>(ModelId.Deserialize(item.ModelId)).ToMutable();
                    entry = new MerchantPotionEntry(model, player);
                }
                else
                {
                    entry = new MerchantPotionEntry(player);
                    PotionModelField.SetValue(entry, null);
                }
                SetRawCost(entry, item.Cost);
                potions.Add(entry);
            }
        }
        finally
        {
            player.PlayerRng.LoadFromSerializable(playerRng);
            player.RelicGrabBag.LoadFromSerializable(playerGrabBag);
            runState.SharedRelicGrabBag.LoadFromSerializable(sharedGrabBag);
        }

        var removal = new MerchantCardRemovalEntry(player);
        SetRawCost(removal, saved.CardRemovalCost);
        if (saved.CardRemovalUsed)
            removal.SetUsed();
        CardRemovalField.SetValue(inventory, removal);

        var updateEntries = (Action<PurchaseStatus, MerchantEntry>)UpdateEntriesMethod
            .CreateDelegate(typeof(Action<PurchaseStatus, MerchantEntry>), inventory);
        foreach (var entry in inventory.AllEntries)
            entry.PurchaseCompleted += updateEntries;

        return inventory;
    }

    private static void RestoreCard(
        RunState runState,
        Player player,
        MerchantCardEntry entry,
        ShopCardEntrySnapshot saved,
        ICollection<CardModel> loadedCards)
    {
        if (saved.Stocked)
        {
            if (saved.Card?.Id == null)
                throw new InvalidDataException("A stocked merchant card has no saved model.");
            var card = runState.LoadCard(saved.Card, player);
            loadedCards.Add(card);
            CardCreationField.SetValue(entry, new CardCreationResult(card));
        }
        CardSaleField.SetValue(entry, saved.OnSale);
        SetRawCost(entry, saved.Cost);
    }

    private static void EnsureCount<T>(IReadOnlyList<T> items, int expected, string name)
    {
        if (items.Count != expected)
            throw new InvalidDataException($"Expected {expected} {name}, found {items.Count}.");
    }

    private static int GetRawCost(MerchantEntry entry) => (int)CostField.GetValue(entry)!;
    private static void SetRawCost(MerchantEntry entry, int cost) => CostField.SetValue(entry, cost);
}
