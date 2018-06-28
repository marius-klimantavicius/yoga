/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * Copyright (c) 2018-present, Marius Klimantavičius
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marius.Yoga
{
    internal static class YogaGlobal
    {
        public const float DefaultFlexGrow = 0.0f;
        public const float DefaultFlexShrink = 0.0f;
        public const float WebDefaultFlexShrink = 1.0f;

        public static readonly YogaArray<YogaEdge> Leading = new YogaArray<YogaEdge>(YogaEdge.Top, YogaEdge.Bottom, YogaEdge.Left, YogaEdge.Right);
        public static readonly YogaArray<YogaEdge> Trailing = new YogaArray<YogaEdge>(YogaEdge.Bottom, YogaEdge.Top, YogaEdge.Right, YogaEdge.Left);
        public static readonly YogaArray<YogaEdge> Position = new YogaArray<YogaEdge>(YogaEdge.Top, YogaEdge.Bottom, YogaEdge.Left, YogaEdge.Right);
        public static readonly YogaArray<YogaDimension> Dimension = new YogaArray<YogaDimension>(YogaDimension.Height, YogaDimension.Height, YogaDimension.Width, YogaDimension.Width);

        public static float? ResolveValue(YogaValue value, float? ownerSize)
        {
            switch (value.Unit)
            {
                case YogaUnit.Undefined:
                case YogaUnit.Auto:
                    return null;
                case YogaUnit.Point:
                    return value.Value;
                case YogaUnit.Percent:
                    return value.Value * ownerSize * 0.01f;
            }

            return null;
        }

        public static bool FlexDirectionIsRow(YogaFlexDirection flexDirection)
        {
            return flexDirection == YogaFlexDirection.Row || flexDirection == YogaFlexDirection.RowReverse;
        }

        public static bool FlexDirectionIsColumn(YogaFlexDirection flexDirection)
        {
            return flexDirection == YogaFlexDirection.Column || flexDirection == YogaFlexDirection.ColumnReverse;
        }

        public static YogaFlexDirection ResolveFlexDirection(YogaFlexDirection flexDirection, YogaDirection direction)
        {
            if (direction == YogaDirection.RightToLeft)
            {
                if (flexDirection == YogaFlexDirection.Row)
                {
                    return YogaFlexDirection.RowReverse;
                }
                else if (flexDirection == YogaFlexDirection.RowReverse)
                {
                    return YogaFlexDirection.Row;
                }
            }

            return flexDirection;
        }

        public static YogaFlexDirection FlexDirectionCross(YogaFlexDirection flexDirection, YogaDirection direction)
        {
            return FlexDirectionIsColumn(flexDirection)
                ? ResolveFlexDirection(YogaFlexDirection.Row, direction)
                : YogaFlexDirection.Column;
        }

        public static float? ResolveValueMargin(YogaValue value, float? ownerSize)
        {
            return value.Unit == YogaUnit.Auto ? 0F : ResolveValue(value, ownerSize);
        }

        public static YogaValue ComputedEdgeValue(YogaArray<YogaValue> edges, YogaEdge edge, YogaValue defaultValue)
        {
            if (edges[edge].Unit != YogaUnit.Undefined)
                return edges[edge];

            if ((edge == YogaEdge.Top || edge == YogaEdge.Bottom) && edges[YogaEdge.Vertical].Unit != YogaUnit.Undefined)
                return edges[YogaEdge.Vertical];

            if ((edge == YogaEdge.Left || edge == YogaEdge.Right || edge == YogaEdge.Start || edge == YogaEdge.End) && edges[YogaEdge.Horizontal].Unit != YogaUnit.Undefined)
                return edges[YogaEdge.Horizontal];

            if (edges[YogaEdge.All].Unit != YogaUnit.Undefined)
                return edges[YogaEdge.All];

            if (edge == YogaEdge.Start || edge == YogaEdge.End)
                return YogaValue.Undefined;

            return defaultValue;
        }


        public static bool ValueEqual(YogaValue a, YogaValue b)
        {
            if (a.Unit != b.Unit)
                return false;

            if (a.Unit == YogaUnit.Undefined || (a.Value == null && b.Value == null))
                return true;

            if (a.Value == null && b.Value == null)
                return true;

            if (a.Value == null || b.Value == null)
                return false;

            return Math.Abs(a.Value.Value - b.Value.Value) < 0.0001f;
        }

        public static bool FloatsEqual(float? a, float? b)
        {
            if (a != null && b != null)
                return Math.Abs(a.Value - b.Value) < 0.0001f;

            return a == null && b == null;
        }
    }
}
