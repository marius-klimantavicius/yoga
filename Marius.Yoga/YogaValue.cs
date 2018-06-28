/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * Copyright (c) 2018-present, Marius Klimantavičius
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Marius.Yoga
{
    public struct YogaValue
    {
        public static readonly YogaValue Zero = new YogaValue { Value = 0, Unit = YogaUnit.Point };
        public static readonly YogaValue Undefined = new YogaValue { Value = null, Unit = YogaUnit.Undefined };
        public static readonly YogaValue Auto = new YogaValue { Value = null, Unit = YogaUnit.Auto };

        public float? Value;
        public YogaUnit Unit;

        public static YogaValue Percent(float percentValue)
        {
            return new YogaValue() { Unit = YogaUnit.Percent, Value = percentValue };
        }

        public static YogaValue Point(float pointValue)
        {
            return new YogaValue() { Unit = YogaUnit.Point, Value = pointValue };
        }

        public override string ToString()
        {
            if (Unit == YogaUnit.Undefined)
                return "undefined";

            if (Unit == YogaUnit.Auto)
                return "auto";

            if (Unit == YogaUnit.Point)
                return $"{Value} pt";

            return $"{Value} %";
        }

        public static implicit operator YogaValue(float pointValue)
        {
            return Point(pointValue);
        }
    }
}
