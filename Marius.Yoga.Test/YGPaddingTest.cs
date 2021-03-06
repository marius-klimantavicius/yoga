/**
 * Copyright (c) 2014-present, Facebook, Inc.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

// @Generated by gentest/gentest.rb from gentest/fixtures/YGPaddingTest.html

using System;
using NUnit.Framework;

namespace Marius.Yoga
{
    [TestFixture]
    public class YGPaddingTest
    {
        [Test]
        public void Test_padding_no_size()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.PaddingLeft = 10;
            root.PaddingTop = 10;
            root.PaddingRight = 10;
            root.PaddingBottom = 10;
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(20f, root.LayoutWidth);
            Assert.AreEqual(20f, root.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(20f, root.LayoutWidth);
            Assert.AreEqual(20f, root.LayoutHeight);
        }

        [Test]
        public void Test_padding_container_match_child()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.PaddingLeft = 10;
            root.PaddingTop = 10;
            root.PaddingRight = 10;
            root.PaddingBottom = 10;

            YogaNode root_child0 = new YogaNode(config);
            root_child0.Width = 10;
            root_child0.Height = 10;
            root.Insert(0, root_child0);
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(30f, root.LayoutWidth);
            Assert.AreEqual(30f, root.LayoutHeight);

            Assert.AreEqual(10f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(30f, root.LayoutWidth);
            Assert.AreEqual(30f, root.LayoutHeight);

            Assert.AreEqual(10f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);
        }

        [Test]
        public void Test_padding_flex_child()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.PaddingLeft = 10;
            root.PaddingTop = 10;
            root.PaddingRight = 10;
            root.PaddingBottom = 10;
            root.Width = 100;
            root.Height = 100;

            YogaNode root_child0 = new YogaNode(config);
            root_child0.FlexGrow = 1;
            root_child0.Width = 10;
            root.Insert(0, root_child0);
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(10f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(80f, root_child0.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(80f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(80f, root_child0.LayoutHeight);
        }

        [Test]
        public void Test_padding_stretch_child()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.PaddingLeft = 10;
            root.PaddingTop = 10;
            root.PaddingRight = 10;
            root.PaddingBottom = 10;
            root.Width = 100;
            root.Height = 100;

            YogaNode root_child0 = new YogaNode(config);
            root_child0.Height = 10;
            root.Insert(0, root_child0);
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(10f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(80f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(10f, root_child0.LayoutX);
            Assert.AreEqual(10f, root_child0.LayoutY);
            Assert.AreEqual(80f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);
        }

        [Test]
        public void Test_padding_center_child()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.JustifyContent = YogaJustify.Center;
            root.AlignItems = YogaAlign.Center;
            root.PaddingStart = 10;
            root.PaddingEnd = 20;
            root.PaddingBottom = 20;
            root.Width = 100;
            root.Height = 100;

            YogaNode root_child0 = new YogaNode(config);
            root_child0.Width = 10;
            root_child0.Height = 10;
            root.Insert(0, root_child0);
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(40f, root_child0.LayoutX);
            Assert.AreEqual(35f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(100f, root.LayoutWidth);
            Assert.AreEqual(100f, root.LayoutHeight);

            Assert.AreEqual(50f, root_child0.LayoutX);
            Assert.AreEqual(35f, root_child0.LayoutY);
            Assert.AreEqual(10f, root_child0.LayoutWidth);
            Assert.AreEqual(10f, root_child0.LayoutHeight);
        }

        [Test]
        public void Test_child_with_padding_align_end()
        {
            YogaConfig config = new YogaConfig();

            YogaNode root = new YogaNode(config);
            root.JustifyContent = YogaJustify.FlexEnd;
            root.AlignItems = YogaAlign.FlexEnd;
            root.Width = 200;
            root.Height = 200;

            YogaNode root_child0 = new YogaNode(config);
            root_child0.PaddingLeft = 20;
            root_child0.PaddingTop = 20;
            root_child0.PaddingRight = 20;
            root_child0.PaddingBottom = 20;
            root_child0.Width = 100;
            root_child0.Height = 100;
            root.Insert(0, root_child0);
            root.StyleDirection = YogaDirection.LeftToRight;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(200f, root.LayoutWidth);
            Assert.AreEqual(200f, root.LayoutHeight);

            Assert.AreEqual(100f, root_child0.LayoutX);
            Assert.AreEqual(100f, root_child0.LayoutY);
            Assert.AreEqual(100f, root_child0.LayoutWidth);
            Assert.AreEqual(100f, root_child0.LayoutHeight);

            root.StyleDirection = YogaDirection.RightToLeft;
            root.CalculateLayout();

            Assert.AreEqual(0f, root.LayoutX);
            Assert.AreEqual(0f, root.LayoutY);
            Assert.AreEqual(200f, root.LayoutWidth);
            Assert.AreEqual(200f, root.LayoutHeight);

            Assert.AreEqual(0f, root_child0.LayoutX);
            Assert.AreEqual(100f, root_child0.LayoutY);
            Assert.AreEqual(100f, root_child0.LayoutWidth);
            Assert.AreEqual(100f, root_child0.LayoutHeight);
        }
        [Test]
        public void Test_padding_side_overrides_horizontal_and_vertical()
        {
            var edges = new[] { YogaEdge.Top,
                                YogaEdge.Bottom,
                                YogaEdge.Start,
                                YogaEdge.End,
                                YogaEdge.Left,
                                YogaEdge.Right};
            for (float edgeValue = 0; edgeValue < 2; ++edgeValue)
            {
                foreach (var edge in edges)
                {
                    var horizontalOrVertical = edge == YogaEdge.Top || edge == YogaEdge.Bottom
                        ? YogaEdge.Vertical
                        : YogaEdge.Horizontal;
                    var root = new YogaNode();
                    root.Width = 100;
                    root.Height = 100;
                    root.Style.Padding[horizontalOrVertical] = 10;
                    root.Style.Padding[edge] = edgeValue;
                    root.CalculateLayout(100, 100, YogaDirection.LeftToRight);
                    Assert.AreEqual(edgeValue, root.GetLayoutPadding(edge));
                }
            }
        }
        [Test]
        public void Test_padding_side_overrides_all()
        {
            var edges = new[]{ YogaEdge.Top,
                               YogaEdge.Bottom,
                               YogaEdge.Start,
                               YogaEdge.End,
                               YogaEdge.Left,
                               YogaEdge.Right};
            for (float edgeValue = 0; edgeValue < 2; ++edgeValue)
            {
                foreach (var edge in edges)
                {
                    var root = new YogaNode();
                    root.Width = 100;
                    root.Height = 100;
                    root.Style.Padding[YogaEdge.All] = 10;
                    root.Style.Padding[edge] = edgeValue;
                    root.CalculateLayout(100, 100, YogaDirection.LeftToRight);
                    Assert.AreEqual(edgeValue, root.GetLayoutPadding(edge));
                }
            }
        }
        [Test]
        public void Test_padding_horizontal_and_vertical_overrides_all()
        {
            var directions = new[] { YogaEdge.Horizontal, YogaEdge.Vertical };
            for (float directionValue = 0; directionValue < 2; ++directionValue)
            {
                foreach (var direction in directions)
                {
                    var root = new YogaNode();
                    root.Width = 100;
                    root.Height = 100;
                    root.Style.Padding[YogaEdge.All] = 10;
                    root.Style.Padding[direction] = directionValue;
                    root.CalculateLayout(100, 100, YogaDirection.LeftToRight);
                    if (direction == YogaEdge.Vertical)
                    {
                        Assert.AreEqual(directionValue, root.LayoutPaddingTop);
                        Assert.AreEqual(directionValue, root.LayoutPaddingBottom);
                    }
                    else
                    {
                        Assert.AreEqual(directionValue, root.LayoutPaddingStart);
                        Assert.AreEqual(directionValue, root.LayoutPaddingEnd);
                        Assert.AreEqual(directionValue, root.LayoutPaddingLeft);
                        Assert.AreEqual(directionValue, root.LayoutPaddingRight);
                    }
                }
            }
        }
    }
}