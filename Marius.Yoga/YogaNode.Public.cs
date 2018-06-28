/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * Copyright (c) 2018-present, Marius Klimantavičius
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marius.Yoga
{
    public partial class YogaNode
    {
        private object _data;

        public virtual void MarkDirty()
        {
            MarkDirtyAndPropogate();
        }

        public void MarkHasNewLayout()
        {
            _hasNewLayout = true;
        }

        public YogaNode Parent { get { return _owner; } }

        public bool IsMeasureDefined { get { return _measure != null; } }

        public bool IsBaselineDefined { get { return _baseline != null; } }

        public void CopyStyle(YogaNode srcNode)
        {
            _style.CopyFrom(srcNode.Style);
        }

        public YogaDirection StyleDirection
        {
            get
            {
                return _style.Direction;
            }

            set
            {
                if (_style.Direction != value)
                {
                    _style.Direction = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaFlexDirection FlexDirection
        {
            get
            {
                return _style.FlexDirection;
            }

            set
            {
                if (_style.FlexDirection != value)
                {
                    _style.FlexDirection = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaJustify JustifyContent
        {
            get
            {
                return _style.JustifyContent;
            }

            set
            {
                if (_style.JustifyContent != value)
                {
                    _style.JustifyContent = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaDisplay Display
        {
            get
            {
                return _style.Display;
            }

            set
            {
                if (_style.Display != value)
                {
                    _style.Display = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaAlign AlignItems
        {
            get
            {
                return _style.AlignItems;
            }

            set
            {
                if (_style.AlignItems != value)
                {
                    _style.AlignItems = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaAlign AlignSelf
        {
            get
            {
                return _style.AlignSelf;
            }

            set
            {
                if (_style.AlignSelf != value)
                {
                    _style.AlignSelf = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaAlign AlignContent
        {
            get
            {
                return _style.AlignContent;
            }

            set
            {
                if (_style.AlignContent != value)
                {
                    _style.AlignContent = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaPositionType PositionType
        {
            get
            {
                return _style.PositionType;
            }

            set
            {
                if (_style.PositionType != value)
                {
                    _style.PositionType = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaWrap Wrap
        {
            get
            {
                return _style.FlexWrap;
            }

            set
            {
                if (_style.FlexWrap != value)
                {
                    _style.FlexWrap = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public float? Flex
        {
            set
            {
                if (value == float.NaN)
                    value = null;

                if (_style.Flex != value)
                {
                    _style.Flex = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public float? FlexGrow
        {
            get
            {
                return _style.FlexGrow;
            }

            set
            {
                if (value == float.NaN)
                    value = null;

                if (_style.FlexGrow != value)
                {
                    _style.FlexGrow = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public float? FlexShrink
        {
            get
            {
                return _style.FlexShrink;
            }

            set
            {
                if (value == float.NaN)
                    value = null;

                if (_style.FlexShrink != value)
                {
                    _style.FlexShrink = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue FlexBasis
        {
            get
            {
                return _style.FlexBasis;
            }

            set
            {
                var current = _style.FlexBasis;
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.FlexBasis = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue Width
        {
            get
            {
                return _style.Dimensions[YogaDimension.Width];
            }

            set
            {
                var current = _style.Dimensions[YogaDimension.Width];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.Dimensions[YogaDimension.Width] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue Height
        {
            get
            {
                return _style.Dimensions[YogaDimension.Height];
            }

            set
            {
                var current = _style.Dimensions[YogaDimension.Height];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.Dimensions[YogaDimension.Height] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue MaxWidth
        {
            get
            {
                return _style.MaxDimensions[YogaDimension.Width];
            }

            set
            {
                var current = _style.MaxDimensions[YogaDimension.Width];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.MaxDimensions[YogaDimension.Width] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue MaxHeight
        {
            get
            {
                return _style.MaxDimensions[YogaDimension.Height];
            }

            set
            {
                var current = _style.MaxDimensions[YogaDimension.Height];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.MaxDimensions[YogaDimension.Height] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue MinWidth
        {
            get
            {
                return _style.MinDimensions[YogaDimension.Width];
            }

            set
            {
                var current = _style.MinDimensions[YogaDimension.Width];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.MinDimensions[YogaDimension.Width] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public YogaValue MinHeight
        {
            get
            {
                return _style.MinDimensions[YogaDimension.Height];
            }

            set
            {
                var current = _style.MinDimensions[YogaDimension.Height];
                if (current.Unit != value.Unit || (value.Unit != YogaUnit.Undefined && current.Value != value.Value))
                {
                    _style.MinDimensions[YogaDimension.Height] = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public float? AspectRatio
        {
            get
            {
                return _style.AspectRatio;
            }

            set
            {
                if (_style.AspectRatio != value)
                {
                    _style.AspectRatio = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public float LayoutX
        {
            get
            {

                return _layout.Position[YogaEdge.Left] ?? 0;
            }
        }

        public float LayoutY
        {
            get
            {
                return _layout.Position[YogaEdge.Top] ?? 0;
            }
        }

        public float LayoutWidth
        {
            get
            {
                return _layout.Dimensions[YogaDimension.Width] ?? 0;
            }
        }

        public float LayoutHeight
        {
            get
            {
                return _layout.Dimensions[YogaDimension.Height] ?? 0;
            }
        }

        public YogaDirection LayoutDirection
        {
            get
            {
                return _layout.Direction;
            }
        }

        public YogaOverflow Overflow
        {
            get
            {
                return _style.Overflow;
            }

            set
            {
                if (_style.Overflow != value)
                {
                    _style.Overflow = value;
                    MarkDirtyAndPropogate();
                }
            }
        }

        public object Data
        {
            get
            {
                return _data;
            }

            set
            {
                _data = value;
            }
        }

        public YogaNode this[int index]
        {
            get
            {
                return _children[index];
            }
        }

        public int Count
        {
            get
            {
                return _children != null ? _children.Count : 0;
            }
        }

        public void MarkLayoutSeen()
        {
            _hasNewLayout = false;
        }

        public void SetMeasureFunction(YogaMeasure measure)
        {
            Measure = measure;
        }

        public void SetBaselineFunction(YogaBaseline baseline)
        {
            Baseline = baseline;
        }

        public void Reset()
        {
            if (_children.Count > 0)
                throw new InvalidOperationException("Cannot reset a node which still has children attached");

            if (_owner != null)
                throw new InvalidOperationException("Cannot reset a node still attached to a owner");

            Clear();

            _print = null;
            _hasNewLayout = true;
            _nodeType = YogaNodeType.Default;
            _measure = null;
            _baseline = null;
            _dirtied = null;
            _style = new YogaStyle();
            _layout = new YogaLayout();
            _lineIndex = 0;
            _owner = null;
            _children = new List<YogaNode>();
            _nextChild = null;
            _isDirty = false;
            _resolvedDimensions = new YogaArray<YogaValue>(new YogaValue[] { YogaValue.Undefined, YogaValue.Undefined });

            if (_config.UseWebDefaults)
            {
                SetStyleFlexDirection(YogaFlexDirection.Row);
                SetStyleAlignContent(YogaAlign.Stretch);
            }
        }

        public IEnumerator<YogaNode> GetEnumerator()
        {
            return _children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
