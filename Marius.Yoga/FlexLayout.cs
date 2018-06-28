using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marius.Layout
{
    public class FlexLayout
    {
        public bool IsWrap;
        public bool IsMainReversed;              // whether main axis is reversed
        public bool IsCrossReversed;             // whether cross axis is reversed (wrap only)
        public bool IsVertical;
        public float SizeDimension;             // main axis parent size
        public float AlignDimension;            // cross axis parent size
        public int[] OrderedIndexes;

        public float LineDimension;
        public float FlexDimension;
        public float ExtraFlexDimension;
        public float FlexGrows;
        public float FlexShrinks;
        public float CrossPosition;

        public bool NeedLines;
        public IList<FlexLayoutLine> Lines;
        public float LineSizes;

        public static void Layout(FlexItem item)
        {
            Contract.Requires(item.Parent == null);
            Contract.Requires(!float.IsNaN(item.Width));
            Contract.Requires(!float.IsNaN(item.Height));
            Contract.Requires(item.Measure == null);

            var engine = new FlexLayout();
            engine.LayoutItem(item, item.Width, item.Height);
        }

        private void Initialize(FlexItem item, float width, float height)
        {
            Contract.Requires(item.PaddingLeft >= 0);
            Contract.Requires(item.PaddingRight >= 0);
            Contract.Requires(item.PaddingTop >= 0);
            Contract.Requires(item.PaddingBottom >= 0);

            width -= item.PaddingLeft + item.PaddingRight;
            height -= item.PaddingTop + item.PaddingBottom;

            Debug.Assert(width >= 0);
            Debug.Assert(height >= 0);

            IsMainReversed = false;
            IsVertical = true;
            switch (item.Direction)
            {
                case FlexDirection.RowReverse:
                    IsMainReversed = true;
                    goto case FlexDirection.Row;
                case FlexDirection.Row:
                    IsVertical = false;
                    SizeDimension = width;
                    AlignDimension = height;
                    break;

                case FlexDirection.ColumnReverse:
                    IsMainReversed = true;
                    goto case FlexDirection.Column;

                case FlexDirection.Column:
                    SizeDimension = height;
                    AlignDimension = width;
                    break;

                default:
                    throw new InvalidOperationException();
            }

            OrderedIndexes = null;
            if (item.ShouldOrderChildren && item.Count > 0)
            {
                OrderedIndexes = new int[item.Count];

                // Creating a list of item indices sorted using the children's `order'
                // attribute values. We are using a simple insertion sort as we need
                // stability (insertion order must be preserved) and cross-platform
                // support. We should eventually switch to merge sort (or something
                // else) if the number of items becomes significant enough.
                for (var i = 0; i < item.Count; i++)
                {
                    OrderedIndexes[i] = i;
                    for (var j = i; j > 0; j--)
                    {
                        var prev = OrderedIndexes[j - 1];
                        var curr = OrderedIndexes[j];
                        if (item[prev].Order <= item[curr].Order)
                            break;

                        OrderedIndexes[j - 1] = curr;
                        OrderedIndexes[j] = prev;
                    }
                }
            }

            FlexDimension = 0;
            FlexGrows = 0;
            FlexShrinks = 0;

            IsCrossReversed = false;
            IsWrap = item.Wrap != FlexWrap.NoWrap;
            if (IsWrap)
            {
                if (item.Wrap == FlexWrap.WrapReverse)
                {
                    IsCrossReversed = true;
                    CrossPosition = AlignDimension;
                }
            }
            else
            {
                CrossPosition = IsVertical ? item.PaddingLeft : item.PaddingTop;
            }

            NeedLines = IsWrap && item.AlignContent != FlexAlign.Start;
            Lines = null;
            LineSizes = 0;
        }

        private void Reset()
        {
            LineDimension = IsWrap ? 0 : AlignDimension;
            FlexDimension = SizeDimension;
            ExtraFlexDimension = 0;
            FlexGrows = 0;
            FlexShrinks = 0;
        }

        private bool Align(FlexAlign align, float flexDimension, int childrenCount, ref float position, ref float spacing, bool isStretchAllowed)
        {
            Contract.Requires(flexDimension > 0);

            position = 0;
            spacing = 0;
            switch (align)
            {
                case FlexAlign.Start:
                    break;

                case FlexAlign.End:
                    position = flexDimension;
                    break;

                case FlexAlign.Center:
                    position = flexDimension / 2;
                    break;

                case FlexAlign.SpaceBetween:
                    if (childrenCount > 0)
                        spacing = flexDimension / (childrenCount - 1);

                    break;

                case FlexAlign.SpaceAround:
                    if (childrenCount > 0)
                    {
                        spacing = flexDimension / childrenCount;
                        position = spacing / 2;
                    }
                    break;

                case FlexAlign.SpaceEvenly:
                    if (childrenCount > 0)
                    {
                        spacing = flexDimension / (childrenCount + 1);
                        position = spacing;
                    }
                    break;

                case FlexAlign.Stretch:
                    if (isStretchAllowed)
                    {
                        spacing = flexDimension / childrenCount;
                        break;
                    }
                    return false;

                default:
                    return false;
            }

            return true;
        }

        private FlexAlign AlignChild(FlexItem child, FlexItem parent)
        {
            var align = child.AlignSelf;
            if (align == FlexAlign.Auto)
                align = parent.AlignItems;

            return align;
        }

        private void LayoutItems(FlexItem item, int childBegin, int childEnd, int childrenCount)
        {
            Contract.Requires(childrenCount <= (childEnd - childBegin));
            if (childrenCount <= 0)
                return;

            if (FlexDimension > 0 && ExtraFlexDimension > 0)
            {
                // If the container has a positive flexible space, let's add to it
                // the sizes of all flexible children.
                FlexDimension += ExtraFlexDimension;
            }

            // Determine the main axis initial position and optional spacing.
            var position = 0F;
            var spacing = 0F;
            if (FlexGrows == 0 && FlexDimension > 0)
            {
                if (!Align(item.JustifyContent, FlexDimension, childrenCount, ref position, ref spacing, false))
                    throw new InvalidOperationException();
                if (IsMainReversed)
                    position = SizeDimension - position;
            }

            if (IsMainReversed)
                position -= IsVertical ? item.PaddingBottom : item.PaddingRight;
            else
                position += IsVertical ? item.PaddingTop : item.PaddingLeft;

            if (IsWrap && IsCrossReversed)
                CrossPosition -= LineDimension;

            for (var i = childBegin; i < childEnd; i++)
            {
                var child = LayoutChildAt(item, i);
                if (child.Position == FlexPosition.Absolute) // already positioned
                    continue;

                // Grow or shrink the main axis item size if needed.
                var flexSize = 0F;
                if (FlexDimension > 0)
                {
                    if (child.Grow != 0)
                    {
                        SetMainSize(child, 0);

                        flexSize = (FlexDimension / FlexGrows) * child.Grow;
                    }
                }
                else if (FlexDimension < 0)
                {
                    if (child.Shrink != 0)
                        flexSize = (FlexDimension / FlexShrinks) * child.Shrink;
                }

                SetMainSize(child, GetMainSize(child) + flexSize);

                // Set the cross axis position (and stretch the cross axis size if
                // needed).
                var alignSize = GetMainSize(child);
                var alignPosition = CrossPosition + 0;
                switch (AlignChild(child, item))
                {
                    case FlexAlign.End:
                        alignPosition += LineDimension - alignSize - GetCrossMarginEnd(child);
                        break;

                    case FlexAlign.Center:
                        alignPosition += (LineDimension / 2) - (alignSize / 2) + (GetCrossMarginStart(child) - GetCrossMarginEnd(child));
                        break;

                    case FlexAlign.Stretch:
                        if (alignSize == 0)
                            SetCrossSize(child, LineDimension - (GetCrossMarginStart(child) + GetCrossMarginEnd(child)));
                        goto case FlexAlign.Start;

                    case FlexAlign.Start:
                        alignPosition += GetCrossMarginStart(child);
                        break;

                    default:
                        Debug.Assert(false, "incorrect align_self");
                        break;
                }

                SetCrossPosition(child, alignPosition);

                // Set the main axis position.
                if (IsMainReversed)
                {
                    position -= GetMainMarginEnd(child);
                    position -= GetMainSize(child);
                    SetMainPosition(child, position);
                    position -= spacing;
                    position -= GetMainMarginStart(child);
                }
                else
                {
                    position += GetMainMarginStart(child);
                    SetMainPosition(child, position);
                    position += GetMainSize(child);
                    position += spacing;
                    position += GetMainMarginEnd(child);
                }

                // Now that the item has a frame, we can layout its children.
                LayoutItem(child, child.LayoutWidth, child.LayoutHeight);
            }

            if (IsWrap && !IsCrossReversed)
                CrossPosition += LineDimension;

            if (NeedLines)
            {
                if (Lines == null)
                    Lines = new List<FlexLayoutLine>();

                var line = new FlexLayoutLine()
                {
                    ChildBegin = childBegin,
                    ChildEnd = childEnd,
                    Size = LineDimension,
                };

                Lines.Add(line);
                LineSizes += line.Size;
            }
        }

        private void LayoutItem(FlexItem item, float width, float height)
        {
            if (item.Count == 0)
                return;

            Initialize(item, width, height);

            Reset();

            var lastLayoutChild = 0;
            var relativeChildCount = 0;
            for (var i = 0; i < item.Count; i++)
            {
                var child = LayoutChildAt(item, i);

                // Items with an absolute position have their frames determined
                // directly and are skipped during layout.
                if (child.Position == FlexPosition.Absolute)
                {
                    var childWidth = GetAbsoluteSize(child.Width, child.Left, child.Right, width);
                    var childHeight = GetAbsoluteSize(child.Height, child.Top, child.Bottom, height);
                    var childX = GetAbsolutePosition(child.Left, child.Right, childWidth, width);
                    var childY = GetAbsolutePosition(child.Top, child.Bottom, childHeight, height);

                    child.LayoutX = childX;
                    child.LayoutY = childY;
                    child.LayoutWidth = childWidth;
                    child.LayoutHeight = childHeight;

                    // Now that the item has a frame, we can layout its children.
                    LayoutItem(child, child.LayoutWidth, child.LayoutHeight);

                    continue;
                }

                // Initialize frame.
                child.LayoutX = 0;
                child.LayoutY = 0;
                child.LayoutWidth = child.Width;
                child.LayoutHeight = child.Height;

                // Main axis size defaults to 0.
                if (float.IsNaN(GetMainSize(child)))
                {
                    SetMainSize(child, 0);
                }

                // Cross axis size defaults to the parent's size (or line size in wrap
                // mode, which is calculated later on).
                if (float.IsNaN(GetCrossSize(child)))
                {
                    if (IsWrap)
                        NeedLines = true;
                    else
                        SetCrossSize(child, (IsVertical ? width : height) - GetCrossMarginStart(child) - GetCrossMarginEnd(child));
                }

                // Call the self sizing callback if provided. Only non-NAN values
                // are taken into account. If the item's cross-axis align property
                // is set to stretch, ignore the value returned by the callback.
                if (child.Measure != null)
                {
                    var childWidth = default(float);
                    var childHeight = default(float);

                    child.Measure(child, ref childWidth, ref childHeight);


                    if (!IsVertical || AlignChild(child, item) != FlexAlign.Stretch)
                    {
                        if (!float.IsNaN(childWidth))
                            child.LayoutWidth = childWidth;
                    }

                    if (IsVertical || AlignChild(child, item) != FlexAlign.Stretch)
                    {
                        if (!float.IsNaN(childHeight))
                            child.LayoutHeight = childHeight;
                    }
                }

                // Honor the `basis' property which overrides the main-axis size.
                if (!float.IsNaN(child.Basis))
                {
                    Debug.Assert(child.Basis >= 0);
                    SetMainSize(child, child.Basis);
                }

                var childMainSize = GetMainSize(child);
                if (IsWrap)
                {
                    if (FlexDimension < childMainSize)
                    {
                        // Not enough space for this child on this line, layout the
                        // remaining items and move it to a new line.
                        LayoutItems(item, lastLayoutChild, i, relativeChildCount);

                        Reset();
                        lastLayoutChild = i;
                        relativeChildCount = 0;
                    }

                    var childCrossSize = GetCrossSize(child);
                    if (!float.IsNaN(childCrossSize) && childCrossSize > LineDimension)
                    {
                        LineDimension = childCrossSize;
                    }
                }

                Debug.Assert(child.Grow >= 0);
                Debug.Assert(child.Shrink >= 0);

                FlexGrows += child.Grow;
                FlexShrinks += child.Shrink;

                FlexDimension -= childMainSize + (GetMainMarginStart(child) + GetMainMarginEnd(child));

                relativeChildCount++;

                if (childMainSize > 0 && child.Grow > 0)
                {
                    ExtraFlexDimension += childMainSize;
                }
            }

            // Layout remaining items in wrap mode, or everything otherwise.
            LayoutItems(item, lastLayoutChild, item.Count, relativeChildCount);

            // In wrap mode we may need to tweak the position of each line according to
            // the align content property as well as the cross-axis size of items that
            // haven't been set yet.
            if (NeedLines && Lines?.Count > 0)
            {
                var pos = 0F;
                var spacing = 0F;
                var flexDimension = AlignDimension - LineSizes;
                if (flexDimension > 0)
                {
                    Align(item.AlignContent, flexDimension, Lines.Count, ref pos, ref spacing, true);
                }

                var oldPosition = 0F;
                if (IsCrossReversed)
                {
                    pos = AlignDimension - pos;
                    oldPosition = AlignDimension;
                }

                for (var i = 0; i < Lines.Count; i++)
                {
                    var line = Lines[i];

                    if (IsCrossReversed)
                    {
                        pos -= line.Size;
                        pos -= spacing;
                        oldPosition -= line.Size;
                    }

                    // Re-position the children of this line, honoring any child
                    // alignment previously set within the line.
                    for (int j = line.ChildBegin; j < line.ChildEnd; j++)
                    {
                        var child = LayoutChildAt(item, j);
                        if (child.Position == FlexPosition.Absolute)
                        {
                            // Should not be re-positioned.
                            continue;
                        }

                        if (float.IsNaN(GetCrossSize(child)))
                        {
                            // If the child's cross axis size hasn't been set it, it
                            // defaults to the line size.
                            SetCrossSize(child, line.Size + (item.AlignContent == FlexAlign.Stretch ? spacing : 0));
                        }

                        SetCrossSize(child, pos + (GetCrossSize(child) - oldPosition));
                    }

                    if (!IsCrossReversed)
                    {
                        pos += line.Size;
                        pos += spacing;
                        oldPosition += line.Size;
                    }
                }
            }
        }

        private float GetAbsoluteSize(float val, float pos1, float pos2, float dim)
        {
            if (!float.IsNaN(val))
                return val;

            if (!float.IsNaN(pos1) && !float.IsNaN(pos2))
                return dim - pos2 - pos1;

            return 0;
        }

        private float GetAbsolutePosition(float pos1, float pos2, float size, float dim)
        {
            if (!float.IsNaN(pos1))
                return pos1;

            if (!float.IsNaN(pos2))
                return dim - size - pos2;

            return 0;
        }

        private FlexItem LayoutChildAt(FlexItem item, int i)
        {
            var child = default(FlexItem);
            if (OrderedIndexes != null)
                child = item[OrderedIndexes[i]];
            else
                child = item[i];
            return child;
        }

        private float GetMainSize(FlexItem child)
        {
            if (IsVertical)
                return child.LayoutHeight;

            return child.LayoutWidth;
        }

        private void SetMainSize(FlexItem child, float value)
        {
            if (IsVertical)
                child.LayoutHeight = value;
            else
                child.LayoutWidth = value;
        }

        private float GetCrossSize(FlexItem child)
        {
            if (IsVertical)
                return child.LayoutWidth;

            return child.LayoutHeight;
        }

        private void SetCrossSize(FlexItem child, float value)
        {
            if (IsVertical)
                child.LayoutWidth = value;
            else
                child.LayoutHeight = value;
        }

        private float GetMainPosition(FlexItem child)
        {
            if (IsVertical)
                return child.LayoutY;

            return child.LayoutX;
        }

        private void SetMainPosition(FlexItem child, float value)
        {
            if (IsVertical)
                child.LayoutY = value;
            else
                child.LayoutX = value;
        }

        private float GetCrossPosition(FlexItem child)
        {
            if (IsVertical)
                return child.LayoutX;

            return child.LayoutY;
        }

        private void SetCrossPosition(FlexItem child, float value)
        {
            if (IsVertical)
                child.LayoutX = value;
            else
                child.LayoutY = value;
        }

        private float GetMainMarginStart(FlexItem child)
        {
            if (IsVertical)
                return child.MarginTop;

            return child.MarginLeft;
        }

        private float GetCrossMarginStart(FlexItem child)
        {
            if (IsVertical)
                return child.MarginLeft;

            return child.MarginTop;
        }

        private float GetMainMarginEnd(FlexItem child)
        {
            if (IsVertical)
                return child.MarginBottom;

            return child.MarginRight;
        }

        private float GetCrossMarginEnd(FlexItem child)
        {
            if (IsVertical)
                return child.MarginRight;

            return child.MarginBottom;
        }
    }
}
