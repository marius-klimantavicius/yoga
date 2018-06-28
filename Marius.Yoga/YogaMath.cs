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
    public static class YogaMath
    {
        public static float Max(float a, float? b)
        {
            if (b != null)
                return Math.Max(a, b.Value);

            return a;
        }

        public static float Max(float? a, float b)
        {
            if (a != null)
                return Math.Max(a.Value, b);

            return b;
        }

        public static float? Max(float? a, float? b)
        {
            if (a != null && b != null)
                return Math.Max(a.Value, b.Value);

            return (a == null) ? b : a;
        }

        public static float? Min(float? a, float? b)
        {
            if (a != null && b != null)
                return Math.Min(a.Value, b.Value);

            return a == null ? b : a;
        }
    }
}
