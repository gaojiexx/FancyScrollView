﻿using System;
using System.Collections.Generic;
using UnityEngine;
using EasingCore;

namespace FancyScrollView
{
    [RequireComponent(typeof(Scroller))]
    public abstract class FancyScrollRect<TItemData, TContext> : FancyScrollView<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        [SerializeField] protected float cellSpacing = 10;
        [SerializeField] protected float reuseCellMarginCount = 0;

        protected virtual float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;

        protected virtual float ViewportLength => ScrollLength - reuseCellMarginCount * 2f;

        protected virtual float MaxScrollPosition => ItemsSource.Count - ScrollLength + reuseCellMarginCount * 2f;

        protected virtual bool ScrollEnabled => MaxScrollPosition > 0f;

        protected virtual float CellSize => Scroller.ScrollDirection == ScrollDirection.Horizontal
            ? CellRectTransform.rect.width
            : CellRectTransform.rect.height;

        Scroller cachedScroller;
        protected Scroller Scroller => cachedScroller ?? (cachedScroller = GetComponent<Scroller>());

        RectTransform cachedCellRect;
        RectTransform CellRectTransform => cachedCellRect ?? (cachedCellRect = CellPrefab.transform as RectTransform);

        protected virtual void Awake()
        {
            Context.CalculateScrollSize = () =>
            {
                var interval = CellSize + cellSpacing;
                var reuseMargin = interval * reuseCellMarginCount;
                var scrollSize = Scroller.ViewportSize + interval + reuseMargin * 2f;
                return (scrollSize, reuseMargin);
            };
        }

        protected virtual void Start()
        {
            scrollOffset = cellInterval;
            Scroller.OnValueChanged(OnScrollerValueChanged);
        }

        void OnScrollerValueChanged(float p)
        {
            base.UpdatePosition(ScrollEnabled ? ToFancyScrollViewPosition(p) : 0f);

            if (Scroller.Scrollbar)
            {
                if (p > ItemsSource.Count - 1)
                {
                    ShrinkScrollbar(p - (ItemsSource.Count - 1));
                }
                else if (p < 0f)
                {
                    ShrinkScrollbar(-p);
                }
            }
        }

        void ShrinkScrollbar(float offset)
        {
            var scale = 1f - ToFancyScrollViewPosition(offset) / ViewportLength;
            UpdateScrollbarSize(ViewportLength * scale / Mathf.Max(ItemsSource.Count, 1));
        }

        protected override void UpdateContents(IList<TItemData> items)
        {
            AdjustCellIntervalAndScrollOffset();
            base.UpdateContents(items);

            Scroller.SetTotalCount(items.Count);
            Scroller.Draggable = ScrollEnabled;
            Scroller.ScrollSensitivity = ToScrollerPosition(ViewportLength);
            Scroller.Position = ToScrollerPosition(currentPosition);

            if (Scroller.Scrollbar)
            {
                Scroller.Scrollbar.gameObject.SetActive(ScrollEnabled);
                UpdateScrollbarSize(ViewportLength / Mathf.Max(ItemsSource.Count, 1));
            }
        }

        protected new void UpdatePosition(float position)
        {
            UpdatePosition(position, Alignment.Center);
        }

        protected virtual void UpdatePosition(float position, Alignment alignment)
        {
            Scroller.Position = ToScrollerPosition(position, alignment);
        }

        public virtual void ScrollTo(int index, float duration, Alignment alignment = Alignment.Center, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, onComplete);
        }

        public virtual void ScrollTo(int index, float duration, Ease easing, Alignment alignment = Alignment.Center, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, easing, onComplete);
        }

        protected void UpdateScrollbarSize(float size)
        {
            Scroller.Scrollbar.size = ScrollEnabled ? Mathf.Clamp01(size) : 1f;
        }

        protected virtual float ToFancyScrollViewPosition(float scrollerPosition)
        {
            return scrollerPosition / Mathf.Max(ItemsSource.Count - 1, 1) * MaxScrollPosition;
        }

        protected virtual float ToScrollerPosition(float position)
        {
            return position / MaxScrollPosition * (ItemsSource.Count - 1);
        }

        protected virtual float ToScrollerPosition(float position, Alignment alignment = Alignment.Center)
        {
            var offset = (ScrollLength - (1f + reuseCellMarginCount * 2f)) * GetAnchore(alignment);
            return ToScrollerPosition(Mathf.Clamp(position - offset, 0f, MaxScrollPosition));
        }

        protected virtual float GetAnchore(Alignment alignment)
        {
            switch (alignment)
            {
                case Alignment.Head: return 0.0f;
                case Alignment.Center: return 0.5f;
                case Alignment.Tail: return 1.0f;
                default: return GetAnchore(Alignment.Center);
            }
        }

        void AdjustCellIntervalAndScrollOffset()
        {
            var totalSize = Scroller.ViewportSize + (CellSize + cellSpacing) * (1f + reuseCellMarginCount * 2f);
            cellInterval = (CellSize + cellSpacing) / totalSize;
            scrollOffset = cellInterval * (1f + reuseCellMarginCount);
        }

        protected virtual void OnValidate()
        {
            if (CellPrefab)
            {
                AdjustCellIntervalAndScrollOffset();
            }

            if (loop)
            {
                loop = false;
                Debug.LogError("Loop is currently not supported in FancyScrollRect.");
            }

            if (Scroller.SnapEnabled)
            {
                Scroller.SnapEnabled = false;
                Debug.LogError("Snap is currently not supported in FancyScrollRect.");
            }

            if (Scroller.MovementType == MovementType.Unrestricted)
            {
                Scroller.MovementType = MovementType.Elastic;
                Debug.LogError("MovementType.Unrestricted is currently not supported in FancyScrollRect.");
            }
        }
    }

    public abstract class FancyScrollRect<TItemData> : FancyScrollRect<TItemData, FancyScrollRectContext> { }
}
