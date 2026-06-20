using UnityEngine;

namespace GreySkies
{
    [CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Grey Skies/Inventory/Item Data")]
    public class InventoryItemData : ScriptableObject
    {
        [Header("Item Info")]
        [Tooltip("Unique string identifier for the item type")]
        [SerializeField] private string _itemId;
        [SerializeField] private string _displayName;

        [Header("Grid Layout")]
        [Tooltip("Width of the item in grid slots")]
        [SerializeField] private int _width = 1;
        [Tooltip("Height of the item in grid slots")]
        [SerializeField] private int _height = 1;

        [Header("Assets")]
        [Tooltip("Icon used in grid representation")]
        [SerializeField] private Sprite _icon;
        [Tooltip("Physical GameObject representation of this item in the game world")]
        [SerializeField] private GameObject _prefab;

        public string ItemID => _itemId;
        public string DisplayName => _displayName;
        public int Width => _width;
        public int Height => _height;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;
    }
}
