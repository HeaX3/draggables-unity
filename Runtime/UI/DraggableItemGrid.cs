using System;
using System.Collections.Generic;
using System.Linq;
using ObjectPooling;
using UnityEngine;
using UnityEngine.UI;

namespace Draggables.UI
{
    public class DraggableItemGrid : MonoBehaviour, ILayoutElement
    {
        [SerializeField] private DraggableItemUIController _prefab;
        [SerializeField] private DraggableItemUIPlaceholder _placeholder;
        [SerializeField] private RectTransform _referenceArea;
        [SerializeField] private RectTransform _area;
        [SerializeField] private Vector2 _referenceEntrySize = new(120, 120);
        [SerializeField] private EntryLayout[] _layouts;

        private bool initialized;
        private Vector2 _entrySize = new(120, 120);
        private int _columns = 1;
        private int _rows = 1;

        private ObjectPool<DraggableItemUIController> ItemPool { get; set; }
        private ObjectPool<DraggableItemUIPlaceholder> PlaceholderPool { get; set; }

        private DraggableItem[] items { get; set; }
        private List<DraggableItemUIController> controllers = new();

        public float minWidth => _entrySize.x * _columns;
        public float preferredWidth => minWidth;
        public float flexibleWidth => 0;
        public float minHeight => _entrySize.y * _rows;
        public float preferredHeight => minHeight;
        public float flexibleHeight => 0;
        public int layoutPriority => 1;

        private void Awake()
        {
            if (!initialized) Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            ItemPool = new ObjectPool<DraggableItemUIController>(_prefab, _area,
                item => { item.Initialize(); });
            PlaceholderPool = _placeholder
                ? new ObjectPool<DraggableItemUIPlaceholder>(_placeholder, _area,
                    item => { item.Initialize(_entrySize); })
                : null;
            if (_prefab.gameObject.scene.name != null)
            {
                _prefab.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            RecalculateLayout();
        }

        public void SetItems<T>(IEnumerable<T> items) => SetItems(items, _ => Guid.NewGuid());

        public void SetItems<T>(IEnumerable<T> items, Func<T, Guid> idCallback)
        {
            SetItems(items.Select(i => new DraggableItem(idCallback(i), i)));
        }

        public void SetItems(IEnumerable<DraggableItem> items)
        {
            if (!initialized) Initialize();
            this.items = items.ToArray();
            foreach (var obsolete in this.controllers.Where(i => this.items.All(e => e.id != i.entry.id)).ToList())
            {
                this.controllers.Remove(obsolete);
                ItemPool.ReturnInstance(obsolete);
            }

            var controllers = new List<DraggableItemUIController>();
            foreach (var item in this.items)
            {
                var controller = this.controllers.FirstOrDefault(i => i.entry?.id == item.id) ??
                                 ItemPool.GetInstance(_ => { });
                var entry = new Entry(item.id, item.data);

                controller.entry = entry;
                controllers.Add(controller);
            }

            this.controllers = controllers;
            RecalculateLayout();
        }

        public void RecalculateLayout()
        {
            if (!gameObject.activeInHierarchy) return;
            var areaWidth = _referenceArea.rect.width;
            var layout = _layouts
                .Where(l => l.areaWidth <= 0 || l.areaWidth >= areaWidth)
                .OrderBy(l => Mathf.Abs(l.areaWidth - areaWidth))
                .FirstOrDefault();
            var maxColumns = layout.minEntryWidth > 0
                ? Mathf.FloorToInt(areaWidth / layout.minEntryWidth)
                : 0;
            var preferredCalculatedColumns = layout is { preferredColumns: <= 0, preferredEntryWidth: > 0 }
                ? Mathf.FloorToInt(areaWidth / layout.preferredEntryWidth)
                : layout.preferredColumns;
            var minColumns = layout.maxEntryWidth > 0
                ? Mathf.CeilToInt(areaWidth / layout.maxEntryWidth)
                : 0;
            if (minColumns <= 0 && preferredCalculatedColumns <= 0) preferredCalculatedColumns = maxColumns;
            if (maxColumns <= 0) maxColumns = int.MaxValue;
            var columns = Mathf.Clamp(preferredCalculatedColumns, minColumns, maxColumns);
            var entryWidth = columns > 0 ? areaWidth / columns : 0;
            var aspect = _referenceEntrySize.x > 0 ? _referenceEntrySize.y / _referenceEntrySize.x : 1;
            _entrySize = new Vector2(entryWidth, aspect * entryWidth);
            var row = 0;
            var column = 0;
            foreach (var controller in controllers)
            {
                controller.size = _entrySize;
                var entry = controller.entry;
                entry.slotX = column;
                entry.slotY = row;
                entry.x = column * _entrySize.x;
                entry.y = -row * _entrySize.y;
                column++;
                if (column >= columns)
                {
                    column = 0;
                    row++;
                }
            }

            _columns = columns;
            _rows = column > 0 ? row + 1 : row;
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }

        public class Entry
        {
            public readonly Guid id;
            public readonly object item;
            public float x;
            public float y;
            public int slotX;
            public int slotY;

            public Entry(Guid id, object item)
            {
                this.id = id;
                this.item = item;
            }
        }

        [Serializable]
        public struct EntryLayout
        {
            public string name;
            public int areaWidth;
            public int preferredColumns;
            public int minEntryWidth;
            public int preferredEntryWidth;
            public int maxEntryWidth;
        }
    }
}