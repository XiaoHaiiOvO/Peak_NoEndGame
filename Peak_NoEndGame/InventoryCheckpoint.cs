using System.Collections.Generic;

namespace Peak_NoEndGame
{
    internal sealed class RecordedInventoryItem
    {
        internal RecordedInventoryItem(ushort itemId, string itemName, ItemInstanceData data)
        {
            ItemId = itemId;
            ItemName = itemName;
            Data = data;
        }

        internal ushort ItemId { get; private set; }
        internal string ItemName { get; private set; }
        internal ItemInstanceData Data { get; private set; }
    }

    internal sealed class InventoryCheckpoint
    {
        private readonly Dictionary<int, List<RecordedInventoryItem>> _itemsByActor =
            new Dictionary<int, List<RecordedInventoryItem>>();

        internal bool IsCaptured { get; private set; }

        internal void Clear()
        {
            _itemsByActor.Clear();
            IsCaptured = false;
        }

        internal int Capture(IEnumerable<Character> characters)
        {
            _itemsByActor.Clear();
            int recordedCount = 0;

            foreach (Character character in characters)
            {
                int actorNumber;
                if (!TryGetActorNumber(character, out actorNumber) || character.player == null)
                {
                    continue;
                }

                List<RecordedInventoryItem> items = new List<RecordedInventoryItem>();
                ItemSlot[] mainSlots = character.player.itemSlots;
                if (mainSlots != null)
                {
                    foreach (ItemSlot slot in mainSlots)
                    {
                        recordedCount += TryCaptureSlot(slot, items);
                    }
                }

                // Backpacks moved out of itemSlots in newer PEAK versions. Record slot 3
                // explicitly so the original "all carried items" behavior is preserved.
                recordedCount += TryCaptureSlot(character.player.GetItemSlot(3), items);
                _itemsByActor[actorNumber] = items;
            }

            IsCaptured = true;
            return recordedCount;
        }

        internal IList<RecordedInventoryItem> GetItems(int actorNumber)
        {
            List<RecordedInventoryItem> items;
            if (_itemsByActor.TryGetValue(actorNumber, out items))
            {
                return items;
            }

            return new RecordedInventoryItem[0];
        }

        internal static bool TryGetActorNumber(Character character, out int actorNumber)
        {
            actorNumber = 0;
            if (character == null || character.photonView == null || character.photonView.Owner == null)
            {
                return false;
            }

            actorNumber = character.photonView.Owner.ActorNumber;
            return true;
        }

        private static int TryCaptureSlot(ItemSlot slot, ICollection<RecordedInventoryItem> destination)
        {
            if (slot == null || slot.IsEmpty() || slot.prefab == null)
            {
                return 0;
            }

            ItemInstanceData copiedData = slot.data == null ? null : slot.data.Copy();
            destination.Add(new RecordedInventoryItem(slot.prefab.itemID, slot.prefab.name, copiedData));
            return 1;
        }
    }
}
