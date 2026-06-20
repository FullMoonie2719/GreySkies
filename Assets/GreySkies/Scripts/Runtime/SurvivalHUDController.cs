using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;

namespace GreySkies
{
    public class SurvivalHUDController : MonoBehaviour
    {
        // Exact Hex Colors from DESIGN.md
        private static readonly Color ColorActiveMoss = HexToColor("#4A6D55");
        private static readonly Color ColorDeepCharcoal = HexToColor("#1E221F");
        private static readonly Color ColorMediumCharcoal = HexToColor("#2A2F2C");
        private static readonly Color ColorLighterCharcoal = HexToColor("#38403B");
        private static readonly Color ColorDullOchre = HexToColor("#D69E2E");
        private static readonly Color ColorCrimson = HexToColor("#B23B3B");
        private static readonly Color ColorOffWhite = HexToColor("#E3E8E5");
        private static readonly Color ColorMutedSage = HexToColor("#A0AAA4");
        private static readonly Color ColorDarkBg = HexToColor("#0E110F");

        public static SurvivalHUDController Instance { get; private set; }

        private Canvas _canvas;
        private GameObject _hudContainer;
        private GameObject _inventoryContainer;

        // HUD Elements
        private UnityEngine.UI.Image _staminaFill;
        private TMPro.TextMeshProUGUI _healthText;
        private TMPro.TextMeshProUGUI _hungerText;
        private TMPro.TextMeshProUGUI _thirstText;
        private TMPro.TextMeshProUGUI _tempText;
        private TMPro.TextMeshProUGUI _bleedingText;

        // Inventory Elements
        private GameObject _vicinityContent;
        private GameObject _gridContainer;
        private TMPro.TextMeshProUGUI _selectedItemInfoText;

        // Selected Item State
        private string _selectedItemInstanceId = "";
        private string _selectedItemId = "";
        private bool _selectedItemRotated = false;
        private int _selectedItemWidth = 1;
        private int _selectedItemHeight = 1;

        // Grid parameters
        private float _cellSize = 40f;
        private float _cellGap = 4f;

        private NetworkPlayerController _localPlayerController;
        private SurvivalStats _localStats;
        private NetworkInventory _localInventory;
        private NetworkInventory _subscribedInventory;

        private bool _isInventoryOpen = false;
        private float _lastVicinityUpdate = 0f;
        private readonly List<PickableItem> _cachedVicinityItems = new List<PickableItem>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InitializeAuto()
        {
            if (Instance == null)
            {
                GameObject go = new GameObject("SurvivalHUDController_Auto");
                go.AddComponent<SurvivalHUDController>();
                Debug.Log("[SurvivalHUDController] Automatically instantiated in active scene.");
            }
        }

        private void Start()
        {
            CreateEventSystem();
            CreateUI();
            SetInventoryOpen(false);
        }

        private void Update()
        {
            if (!FindLocalPlayer())
            {
                // Hide HUD and Inventory if local player is not found
                if (_hudContainer != null) _hudContainer.SetActive(false);
                if (_inventoryContainer != null) _inventoryContainer.SetActive(false);
                return;
            }

            if (_hudContainer != null) _hudContainer.SetActive(true);

            // Manage inventory subscription
            if (_subscribedInventory != _localInventory)
            {
                if (_subscribedInventory != null)
                {
                    _subscribedInventory.Items.OnListChanged -= OnInventoryChanged;
                }
                _subscribedInventory = _localInventory;
                if (_subscribedInventory != null)
                {
                    _subscribedInventory.Items.OnListChanged += OnInventoryChanged;
                }
            }

            // Check for Tab input to toggle full-screen inventory overlay
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                SetInventoryOpen(!_isInventoryOpen);
            }

            // Rotation shortcut
            if (_isInventoryOpen && !string.IsNullOrEmpty(_selectedItemInstanceId))
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    RotateSelectedItem();
                }
            }

            UpdateHUDValues();

            if (_isInventoryOpen)
            {
                UpdateVicinityItems();
            }
        }

        private void OnDestroy()
        {
            if (_subscribedInventory != null)
            {
                _subscribedInventory.Items.OnListChanged -= OnInventoryChanged;
            }
        }

        private void OnInventoryChanged(NetworkListEvent<InventoryItemInstance> changeEvent)
        {
            if (_isInventoryOpen)
            {
                RefreshInventoryGrid();
            }
        }

        private bool FindLocalPlayer()
        {
            if (_localPlayerController != null) return true;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                var localClient = NetworkManager.Singleton.LocalClient;
                if (localClient != null && localClient.PlayerObject != null)
                {
                    _localPlayerController = localClient.PlayerObject.GetComponent<NetworkPlayerController>();
                    if (_localPlayerController != null)
                    {
                        _localStats = _localPlayerController.GetComponent<SurvivalStats>();
                        _localInventory = _localPlayerController.GetComponent<NetworkInventory>();
                        _localPlayerController.DisableInput = _isInventoryOpen;
                        return true;
                    }
                }
            }

            // Fallback
            var players = FindObjectsByType<NetworkPlayerController>(FindObjectsInactive.Exclude);
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    _localPlayerController = player;
                    _localStats = player.GetComponent<SurvivalStats>();
                    _localInventory = player.GetComponent<NetworkInventory>();
                    _localPlayerController.DisableInput = _isInventoryOpen;
                    return true;
                }
            }

            return false;
        }

        private void CreateEventSystem()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private void CreateUI()
        {
            // Root Canvas setup
            GameObject canvasGo = new GameObject("SurvivalUICanvas");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CreateHUDPanel();
            CreateInventoryPanel();
        }

        private void CreateHUDPanel()
        {
            _hudContainer = CreatePanel(_canvas.gameObject, "HUDContainer", new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(320, 240), new Vector2(-20, 20), ColorDeepCharcoal);
            
            // Styled moss border at the top
            CreatePanel(_hudContainer, "TopBorderLine", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 4), new Vector2(0, 0), ColorActiveMoss);

            // Vital statistics displays
            CreateStatLabel(_hudContainer, "HEALTH", new Vector2(15, -20));
            _healthText = CreateText(_hudContainer, "100%", 18, ColorOffWhite, new Vector2(285, -20), TMPro.TextAlignmentOptions.Right);

            CreateStatLabel(_hudContainer, "HUNGER", new Vector2(15, -55));
            _hungerText = CreateText(_hudContainer, "100%", 18, ColorOffWhite, new Vector2(285, -55), TMPro.TextAlignmentOptions.Right);

            CreateStatLabel(_hudContainer, "THIRST", new Vector2(15, -90));
            _thirstText = CreateText(_hudContainer, "100%", 18, ColorOffWhite, new Vector2(285, -90), TMPro.TextAlignmentOptions.Right);

            CreateStatLabel(_hudContainer, "TEMPERATURE", new Vector2(15, -125));
            _tempText = CreateText(_hudContainer, "36.6°C", 18, ColorOffWhite, new Vector2(285, -125), TMPro.TextAlignmentOptions.Right);

            // Bleeding stacks (starts disabled)
            _bleedingText = CreateText(_hudContainer, "BLEEDING x0", 18, ColorCrimson, new Vector2(15, -165), TMPro.TextAlignmentOptions.Left);
            _bleedingText.fontStyle = TMPro.FontStyles.Bold;
            _bleedingText.gameObject.SetActive(false);

            // Stamina Bar: thin horizontal bar running below other statistics
            GameObject staminaBg = CreatePanel(_hudContainer, "StaminaBarBackground", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(-30, 10), new Vector2(0, 15), ColorMediumCharcoal);
            GameObject staminaFillGo = CreatePanel(staminaBg, "StaminaBarFill", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, ColorActiveMoss);
            _staminaFill = staminaFillGo.GetComponent<UnityEngine.UI.Image>();
            _staminaFill.type = UnityEngine.UI.Image.Type.Filled;
            _staminaFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            _staminaFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            _staminaFill.fillAmount = 1.0f;
        }

        private void CreateInventoryPanel()
        {
            Color backgroundTint = new Color(ColorDarkBg.r, ColorDarkBg.g, ColorDarkBg.b, 0.9f);
            _inventoryContainer = CreatePanel(_canvas.gameObject, "InventoryContainerOverlay", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, backgroundTint);

            // Overlay Titles
            var title = CreateText(_inventoryContainer, "SURVIVAL INTERFACE", 28, ColorOffWhite, new Vector2(50, -35));
            title.fontStyle = TMPro.FontStyles.Bold;
            title.rectTransform.sizeDelta = new Vector2(600, 40);

            var subtitle = CreateText(_inventoryContainer, "PRESS TAB TO CLOSE INTERFACE | PRESS R TO ROTATE SELECTED ITEM", 13, ColorMutedSage, new Vector2(50, -75));
            subtitle.rectTransform.sizeDelta = new Vector2(600, 20);

            // LEFT COLUMN: Vicinity Items
            GameObject leftCol = CreatePanel(_inventoryContainer, "VicinityColumn", new Vector2(0.04f, 0.05f), new Vector2(0.32f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorDeepCharcoal);
            CreatePanel(leftCol, "LeftBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 3), new Vector2(0, 0), ColorMutedSage);
            CreateColumnHeader(leftCol, "VICINITY (GROUND)");

            _vicinityContent = CreatePanel(leftCol, "VicinityContent", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
            var vLayout = _vicinityContent.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // CENTER COLUMN: Equipped Gear Skeletal Slots
            GameObject centerCol = CreatePanel(_inventoryContainer, "EquipmentColumn", new Vector2(0.35f, 0.05f), new Vector2(0.64f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorDeepCharcoal);
            CreatePanel(centerCol, "CenterBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 3), new Vector2(0, 0), ColorActiveMoss);
            CreateColumnHeader(centerCol, "EQUIPPED GEAR");
            CreateEquipmentSkeletalSlots(centerCol);

            // RIGHT COLUMN: Network Container Grid
            GameObject rightCol = CreatePanel(_inventoryContainer, "InventoryGridColumn", new Vector2(0.67f, 0.05f), new Vector2(0.96f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorDeepCharcoal);
            CreatePanel(rightCol, "RightBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 3), new Vector2(0, 0), ColorActiveMoss);
            CreateColumnHeader(rightCol, "CONTAINER GRID");

            // Selected Item Info Panel
            GameObject infoPanel = CreatePanel(rightCol, "SelectedItemInfoPanel", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, ColorMediumCharcoal);
            _selectedItemInfoText = CreateText(infoPanel, "No item selected", 13, ColorOffWhite, new Vector2(10, -5));
            _selectedItemInfoText.rectTransform.sizeDelta = new Vector2(400, 50);

            // Container interactive grid holder
            _gridContainer = CreatePanel(rightCol, "GridHolder", new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.5f), new Vector2(460, 460), Vector2.zero, ColorMediumCharcoal);
        }

        private void CreateEquipmentSkeletalSlots(GameObject parent)
        {
            CreateEquipSlot(parent, "HEAD", new Vector2(0, 160), "Headwear");
            CreateEquipSlot(parent, "BACK", new Vector2(-110, 80), "Backpack");
            CreateEquipSlot(parent, "TORSO", new Vector2(0, 80), "Jacket");
            CreateEquipSlot(parent, "HANDS", new Vector2(110, 80), "Weapon Slot");
            CreateEquipSlot(parent, "LEGS", new Vector2(0, 0), "Trousers");
            CreateEquipSlot(parent, "FEET", new Vector2(0, -80), "Boots");
        }

        private void CreateEquipSlot(GameObject parent, string slotName, Vector2 position, string description)
        {
            GameObject slotPanel = CreatePanel(parent, $"EquipSlot_{slotName}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(100, 70), position, ColorMediumCharcoal);
            
            // Add subtle active moss visual highlight border
            GameObject highlight = CreatePanel(slotPanel, "HighlightBorder", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
            var highlightImg = highlight.GetComponent<UnityEngine.UI.Image>();
            highlightImg.color = new Color(ColorActiveMoss.r, ColorActiveMoss.g, ColorActiveMoss.b, 0.18f);

            var label = CreateText(slotPanel, slotName, 11, ColorOffWhite, new Vector2(6, -5));
            label.fontStyle = TMPro.FontStyles.Bold;
            label.rectTransform.sizeDelta = new Vector2(90, 18);

            var placeholderText = CreateText(slotPanel, description, 9, ColorMutedSage, new Vector2(6, -24));
            placeholderText.rectTransform.sizeDelta = new Vector2(90, 40);
        }

        private void CreateColumnHeader(GameObject parent, string text)
        {
            var header = CreateText(parent, text, 18, ColorOffWhite, new Vector2(15, -15));
            header.fontStyle = TMPro.FontStyles.Bold;
            header.rectTransform.sizeDelta = new Vector2(400, 30);
        }

        private void CreateStatLabel(GameObject parent, string text, Vector2 anchoredPos)
        {
            var label = CreateText(parent, text, 14, ColorMutedSage, anchoredPos, TMPro.TextAlignmentOptions.Left);
            label.fontStyle = TMPro.FontStyles.Bold;
        }

        private void UpdateHUDValues()
        {
            if (_localStats == null) return;

            float currentHealth = _localStats.Health.Value;
            float currentStamina = _localStats.Stamina.Value;
            float currentHunger = _localStats.Hunger.Value;
            float currentThirst = _localStats.Thirst.Value;
            float currentTemp = _localStats.Temperature.Value;
            int bleedingStacks = _localStats.BleedingStacks.Value;

            // Health color warnings (<20% crimson, otherwise moss/ochre)
            _healthText.text = $"{Mathf.RoundToInt(currentHealth)}%";
            if (currentHealth < 20f || bleedingStacks > 0)
            {
                _healthText.color = ColorCrimson;
            }
            else if (currentHealth < 50f)
            {
                _healthText.color = ColorDullOchre;
            }
            else
            {
                _healthText.color = ColorActiveMoss;
            }

            // Hunger warnings
            _hungerText.text = $"{Mathf.RoundToInt(currentHunger)}%";
            if (currentHunger < 20f)
            {
                _hungerText.color = ColorCrimson;
            }
            else if (currentHunger < 50f)
            {
                _hungerText.color = ColorDullOchre;
            }
            else
            {
                _hungerText.color = ColorOffWhite;
            }

            // Thirst warnings
            _thirstText.text = $"{Mathf.RoundToInt(currentThirst)}%";
            if (currentThirst < 20f)
            {
                _thirstText.color = ColorCrimson;
            }
            else if (currentThirst < 50f)
            {
                _thirstText.color = ColorDullOchre;
            }
            else
            {
                _thirstText.color = ColorOffWhite;
            }

            // Temperature warnings
            _tempText.text = $"{currentTemp:F1}°C";
            if (currentTemp < 35.5f)
            {
                _tempText.color = Color.cyan;
            }
            else if (currentTemp > 38.0f)
            {
                _tempText.color = ColorCrimson;
            }
            else
            {
                _tempText.color = ColorOffWhite;
            }

            // Bleeding alert
            if (bleedingStacks > 0)
            {
                _bleedingText.gameObject.SetActive(true);
                _bleedingText.text = $"BLEEDING x{bleedingStacks}";
                float flashValue = Mathf.PingPong(Time.time * 3f, 1f);
                _bleedingText.color = new Color(ColorCrimson.r, ColorCrimson.g, ColorCrimson.b, flashValue);
            }
            else
            {
                _bleedingText.gameObject.SetActive(false);
            }

            // Stamina bar depletion
            if (_staminaFill != null)
            {
                _staminaFill.fillAmount = currentStamina / 100f;
            }
        }

        private void UpdateVicinityItems()
        {
            if (Time.time - _lastVicinityUpdate < 0.5f) return;
            _lastVicinityUpdate = Time.time;

            if (_localPlayerController == null) return;

            Vector3 playerPosition = _localPlayerController.transform.position;
            var pickables = FindObjectsByType<PickableItem>(FindObjectsInactive.Exclude);

            _cachedVicinityItems.Clear();
            foreach (var item in pickables)
            {
                if (item == null) continue;
                float distance = Vector3.Distance(playerPosition, item.transform.position);
                if (distance <= 2.5f) // Query vicinity within 2.5 meters
                {
                    _cachedVicinityItems.Add(item);
                }
            }

            // Rebuild vicinity UI
            foreach (Transform child in _vicinityContent.transform)
            {
                Destroy(child.gameObject);
            }

            if (_cachedVicinityItems.Count == 0)
            {
                GameObject emptyLabel = new GameObject("VicinityEmptyLabel");
                var rect = emptyLabel.AddComponent<RectTransform>();
                rect.SetParent(_vicinityContent.transform, false);
                rect.sizeDelta = new Vector2(250, 40);

                var tmp = emptyLabel.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "No items nearby";
                tmp.fontSize = 13;
                tmp.color = ColorMutedSage;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                return;
            }

            foreach (var pickable in _cachedVicinityItems)
            {
                string itemIdStr = pickable.ItemID.Value.ToString();
                var itemData = _localInventory != null ? _localInventory.GetItemData(itemIdStr) : null;
                string displayName = itemData != null ? itemData.DisplayName : itemIdStr;

                GameObject rowPanel = CreatePanel(_vicinityContent, $"VicinityRow_{itemIdStr}", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(240, 50), Vector2.zero, ColorMediumCharcoal);
                
                var lElement = rowPanel.AddComponent<LayoutElement>();
                lElement.minHeight = 50f;
                lElement.preferredHeight = 50f;

                var textLabel = CreateText(rowPanel, $"{displayName} (x{pickable.Quantity})", 12, ColorOffWhite, new Vector2(10, -10));
                textLabel.rectTransform.sizeDelta = new Vector2(140, 30);

                // Take button
                GameObject takeBtn = CreatePanel(rowPanel, "TakeButton", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(70, 34), new Vector2(-10, 0), ColorLighterCharcoal);
                var takeLabel = CreateText(takeBtn, "TAKE", 11, ColorOffWhite, Vector2.zero, TMPro.TextAlignmentOptions.Center);
                takeLabel.rectTransform.anchorMin = new Vector2(0, 0);
                takeLabel.rectTransform.anchorMax = new Vector2(1, 1);
                takeLabel.rectTransform.offsetMin = Vector2.zero;
                takeLabel.rectTransform.offsetMax = Vector2.zero;
                takeLabel.fontStyle = TMPro.FontStyles.Bold;

                var btn = takeBtn.AddComponent<Button>();
                var currentPickable = pickable;
                btn.onClick.AddListener(() =>
                {
                    if (currentPickable != null)
                    {
                        Debug.Log($"[SurvivalHUDController] Vicinity click TAKE: {itemIdStr}");
                        currentPickable.InteractServerRpc(NetworkManager.Singleton.LocalClientId);
                        ClearSelection();
                    }
                });
            }
        }

        private void RefreshInventoryGrid()
        {
            if (_gridContainer == null || _localInventory == null) return;

            foreach (Transform child in _gridContainer.transform)
            {
                Destroy(child.gameObject);
            }

            int gridW = _localInventory.GridWidth;
            int gridH = _localInventory.GridHeight;

            // Fit grid inside bounds beautifully
            _cellSize = Mathf.Min((420f - (gridW - 1) * _cellGap) / gridW, (420f - (gridH - 1) * _cellGap) / gridH);

            float startX = - (gridW * _cellSize + (gridW - 1) * _cellGap) / 2f;
            float startY = (gridH * _cellSize + (gridH - 1) * _cellGap) / 2f;

            // Draw background slot representation
            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    float cellLeft = startX + x * (_cellSize + _cellGap);
                    float cellTop = startY - y * (_cellSize + _cellGap);
                    float cellCenterX = cellLeft + _cellSize / 2f;
                    float cellCenterY = cellTop - _cellSize / 2f;

                    GameObject cellGo = CreatePanel(_gridContainer, $"GridCell_{x}_{y}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(_cellSize, _cellSize), new Vector2(cellCenterX, cellCenterY), ColorMediumCharcoal);
                    
                    var btn = cellGo.AddComponent<Button>();
                    int targetX = x;
                    int targetY = y;
                    btn.onClick.AddListener(() => OnCellClicked(targetX, targetY));
                }
            }

            // Draw occupied item overlays
            foreach (var item in _localInventory.Items)
            {
                string itemIdStr = item.ItemID.ToString();
                var itemData = _localInventory.GetItemData(itemIdStr);
                if (itemData == null) continue;

                int w = item.isRotated ? itemData.Height : itemData.Width;
                int h = item.isRotated ? itemData.Width : itemData.Height;

                float itemLeft = startX + item.slotX * (_cellSize + _cellGap);
                float itemTop = startY - item.slotY * (_cellSize + _cellGap);
                float itemWidth = w * _cellSize + (w - 1) * _cellGap;
                float itemHeight = h * _cellSize + (h - 1) * _cellGap;
                float itemCenterX = itemLeft + itemWidth / 2f;
                float itemCenterY = itemTop - itemHeight / 2f;

                Color itemColor = ColorLighterCharcoal;
                if (item.Guid.ToString() == _selectedItemInstanceId)
                {
                    itemColor = ColorActiveMoss; // Highlight selected item green
                }

                GameObject itemPanel = CreatePanel(_gridContainer, $"GridItem_{item.Guid}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(itemWidth, itemHeight), new Vector2(itemCenterX, itemCenterY), itemColor);
                
                // Add soft item border
                GameObject border = CreatePanel(itemPanel, "ItemBorder", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
                var borderImg = border.GetComponent<UnityEngine.UI.Image>();
                borderImg.color = new Color(ColorOffWhite.r, ColorOffWhite.g, ColorOffWhite.b, 0.15f);

                // Add icon if configured
                if (itemData.Icon != null)
                {
                    GameObject iconGo = CreatePanel(itemPanel, "ItemIcon", new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-8, -8), Vector2.zero, Color.white);
                    var iconImg = iconGo.GetComponent<UnityEngine.UI.Image>();
                    iconImg.sprite = itemData.Icon;
                    iconImg.preserveAspect = true;
                }

                // Add Display Name text
                GameObject nameTextGo = new GameObject("ItemNameText");
                var nameTextRect = nameTextGo.AddComponent<RectTransform>();
                nameTextRect.SetParent(itemPanel.transform, false);
                nameTextRect.anchorMin = new Vector2(0, 0);
                nameTextRect.anchorMax = new Vector2(1, 1);
                nameTextRect.offsetMin = new Vector2(4, 4);
                nameTextRect.offsetMax = new Vector2(-4, -4);

                var tmp = nameTextGo.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = itemData.DisplayName;
                tmp.fontSize = 10;
                tmp.color = ColorOffWhite;
                tmp.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                tmp.raycastTarget = false;

                // Make panel clickable to select/move
                var panelBtn = itemPanel.AddComponent<Button>();
                var currentItem = item;
                panelBtn.onClick.AddListener(() => OnItemClicked(currentItem));
            }
        }

        private void OnItemClicked(InventoryItemInstance item)
        {
            string instanceIdStr = item.Guid.ToString();
            string itemIdStr = item.ItemID.ToString();
            var itemData = _localInventory.GetItemData(itemIdStr);
            if (itemData == null) return;

            if (_selectedItemInstanceId == instanceIdStr)
            {
                ClearSelection();
            }
            else
            {
                _selectedItemInstanceId = instanceIdStr;
                _selectedItemId = itemIdStr;
                _selectedItemRotated = item.isRotated;
                _selectedItemWidth = itemData.Width;
                _selectedItemHeight = itemData.Height;

                _selectedItemInfoText.text = $"Selected: {itemData.DisplayName}\nSize: {itemData.Width}x{itemData.Height} | Rotated: {_selectedItemRotated}\nPress R to Rotate | Click empty cell to Move";
            }

            RefreshInventoryGrid();
        }

        private void OnCellClicked(int x, int y)
        {
            if (string.IsNullOrEmpty(_selectedItemInstanceId)) return;

            // Trigger server RPC to update placement authoritatively
            _localInventory.MoveItemServerRpc(_selectedItemInstanceId, x, y, _selectedItemRotated);
            ClearSelection();
            RefreshInventoryGrid();
        }

        private void RotateSelectedItem()
        {
            if (string.IsNullOrEmpty(_selectedItemInstanceId)) return;

            _selectedItemRotated = !_selectedItemRotated;

            var itemData = _localInventory.GetItemData(_selectedItemId);
            if (itemData != null)
            {
                _selectedItemInfoText.text = $"Selected: {itemData.DisplayName}\nSize: {itemData.Width}x{itemData.Height} | Rotated: {_selectedItemRotated}\nPress R to Rotate | Click empty cell to Move";
            }

            RefreshInventoryGrid();
        }

        private void ClearSelection()
        {
            _selectedItemInstanceId = "";
            _selectedItemId = "";
            _selectedItemRotated = false;
            _selectedItemWidth = 1;
            _selectedItemHeight = 1;
            if (_selectedItemInfoText != null)
            {
                _selectedItemInfoText.text = "No item selected";
            }
        }

        public void SetInventoryOpen(bool isOpen)
        {
            _isInventoryOpen = isOpen;

            if (_inventoryContainer != null)
            {
                _inventoryContainer.SetActive(isOpen);
            }

            if (_localPlayerController != null)
            {
                _localPlayerController.DisableInput = isOpen;
            }

            if (isOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                ClearSelection();
                RefreshInventoryGrid();
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // --- Procedural Layout Helpers ---

        private GameObject CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
        {
            GameObject go = new GameObject(name);
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = color;
            return go;
        }

        private TMPro.TextMeshProUGUI CreateText(GameObject parent, string text, int fontSize, Color color, Vector2 anchoredPos, TMPro.TextAlignmentOptions alignment = TMPro.TextAlignmentOptions.Left)
        {
            GameObject go = new GameObject($"Label_{text}");
            var rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent.transform, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(300, 30);

            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            return Color.white;
        }
    }
}