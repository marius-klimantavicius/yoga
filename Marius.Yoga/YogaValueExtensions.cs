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
    public static class YogaValueExtensions
    {
        public static YogaValue Percent(this float value)
        {
            return YogaValue.Percent(value);
        }

        public static YogaValue Pt(this float value)
        {
            return YogaValue.Point(value);
        }

        public static YogaValue Percent(this int value)
        {
            return YogaValue.Percent(value);
        }

        public static YogaValue Pt(this int value)
        {
            return YogaValue.Point(value);
        }
    }
}
