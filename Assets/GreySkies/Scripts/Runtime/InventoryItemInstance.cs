using System;
using Unity.Netcode;
using Unity.Collections;

namespace GreySkies
{
    public struct InventoryItemInstance : INetworkSerializable, IEquatable<InventoryItemInstance>
    {
        public FixedString32Bytes Guid;
        public FixedString32Bytes ItemID;
        public int slotX;
        public int slotY;
        public bool isRotated;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Guid);
            serializer.SerializeValue(ref ItemID);
            serializer.SerializeValue(ref slotX);
            serializer.SerializeValue(ref slotY);
            serializer.SerializeValue(ref isRotated);
        }

        public bool Equals(InventoryItemInstance other)
        {
            return Guid == other.Guid &&
                   ItemID == other.ItemID &&
                   slotX == other.slotX &&
                   slotY == other.slotY &&
                   isRotated == other.isRotated;
        }

        public override bool Equals(object obj)
        {
            return obj is InventoryItemInstance other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Guid.GetHashCode();
                hash = hash * 23 + ItemID.GetHashCode();
                hash = hash * 23 + slotX.GetHashCode();
                hash = hash * 23 + slotY.GetHashCode();
                hash = hash * 23 + isRotated.GetHashCode();
                return hash;
            }
        }
    }
}
