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
    using static YogaGlobal;

    public partial class YogaNode
    {
        private static uint gDepth = 0;
        private static uint gCurrentGenerationCount = 0;

        public void CalculateLayout()
        {
            CalculateLayout(null, null, StyleDirection);
        }

        public void CalculateLayout(float? ownerWidth, float? ownerHeight, YogaDirection ownerDirection)
        {
            var node = this;

            // Increment the generation count. This will force the recursive routine to
            // visit
            // all dirty nodes at least once. Subsequent visits will be skipped if the
            // input
            // parameters don't change.
            gCurrentGenerationCount++;
            node.ResolveDimension();

            var width = default(float?);
            var widthMeasureMode = YogaMeasureMode.Undefined;
            if (IsStyleDimDefined(node, YogaFlexDirection.Row, ownerWidth))
            {
                width = ResolveValue(node.GetResolvedDimension(Dimension[YogaFlexDirection.Row]), ownerWidth) + node.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);
                widthMeasureMode = YogaMeasureMode.Exactly;
            }
            else if (ResolveValue(node.Style.MaxDimensions[YogaDimension.Width], ownerWidth) != null)
            {
                width = ResolveValue(node.Style.MaxDimensions[YogaDimension.Width], ownerWidth);
                widthMeasureMode = YogaMeasureMode.AtMost;
            }
            else
            {
                width = ownerWidth;
                widthMeasureMode = width == null ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;
            }

            var height = default(float?);
            var heightMeasureMode = YogaMeasureMode.Undefined;
            if (IsStyleDimDefined(node, YogaFlexDirection.Column, ownerHeight))
            {
                height = ResolveValue(node.GetResolvedDimension(Dimension[YogaFlexDirection.Column]), ownerHeight) + node.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);
                heightMeasureMode = YogaMeasureMode.Exactly;
            }
            else if (ResolveValue(node.Style.MaxDimensions[YogaDimension.Height], ownerHeight) != null)
            {
                height = ResolveValue(node.Style.MaxDimensions[YogaDimension.Height], ownerHeight);
                heightMeasureMode = YogaMeasureMode.AtMost;
            }
            else
            {
                height = ownerHeight;
                heightMeasureMode = height == null ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;
            }

            if (LayoutNode(
                    node,
                    width,
                    height,
                    ownerDirection,
                    widthMeasureMode,
                    heightMeasureMode,
                    ownerWidth,
                    ownerHeight,
                    true,
                    "initial",
                    node.Config))
            {
                node.SetPosition(node.Layout.Direction, ownerWidth, ownerHeight, ownerWidth);
                RoundToPixelGrid(node, node.Config.PointScaleFactor, 0.0f, 0.0f);
            }

            // We want to get rid off `useLegacyStretchBehaviour` from YGConfig. But we
            // aren't sure whether client's of yoga have gotten rid off this flag or not.
            // So logging this in YGLayout would help to find out the call sites depending
            // on this flag. This check would be removed once we are sure no one is
            // dependent on this flag anymore. The flag
            // `shouldDiffLayoutWithoutLegacyStretchBehaviour` in YGConfig will help to
            // run experiments.
            if (node.Config.ShouldDiffLayoutWithoutLegacyStretchBehaviour && node.DidUseLegacyFlag())
            {
                var originalNode = DeepClone(node);
                originalNode.ResolveDimension();
                // Recursively mark nodes as dirty
                originalNode.MarkDirtyAndPropogateDownwards();
                gCurrentGenerationCount++;
                // Rerun the layout, and calculate the diff
                originalNode.SetAndPropogateUseLegacyFlag(false);
                if (LayoutNode(
                        originalNode,
                        width,
                        height,
                        ownerDirection,
                        widthMeasureMode,
                        heightMeasureMode,
                        ownerWidth,
                        ownerHeight,
                        true,
                        "initial",
                        originalNode.Config))
                {
                    originalNode.SetPosition(
                        originalNode.Layout.Direction,
                        ownerWidth,
                        ownerHeight,
                        ownerWidth);
                    RoundToPixelGrid(
                        originalNode,
                        originalNode.Config.PointScaleFactor,
                        0.0f,
                        0.0f);

                    // Set whether the two layouts are different or not.
                    node.SetLayoutDoesLegacyFlagAffectsLayout(!originalNode.IsLayoutTreeEqualToNode(node));
                }
            }
        }

        //
        // This is a wrapper around the YGNodelayoutImpl function. It determines
        // whether the layout request is redundant and can be skipped.
        //
        // Parameters:
        //  Input parameters are the same as YGNodelayoutImpl (see above)
        //  Return parameter is true if layout was performed, false if skipped
        //
        private static bool LayoutNode(YogaNode node, float? availableWidth, float? availableHeight, YogaDirection ownerDirection, YogaMeasureMode widthMeasureMode, YogaMeasureMode heightMeasureMode, float? ownerWidth, float? ownerHeight, bool performLayout, string reason, YogaConfig config)
        {
            var layout = node.Layout;

            gDepth++;

            var needToVisitNode =
                (node.IsDirty && layout.GenerationCount != gCurrentGenerationCount) ||
                layout.LastOwnerDirection != ownerDirection;

            if (needToVisitNode)
            {
                // Invalidate the cached results.
                layout.NextCachedMeasurementsIndex = 0;
                layout.CachedLayout.WidthMeasureMode = (YogaMeasureMode)(-1);
                layout.CachedLayout.HeightMeasureMode = (YogaMeasureMode)(-1);
                layout.CachedLayout.ComputedWidth = -1;
                layout.CachedLayout.ComputedHeight = -1;
            }

            var cachedResults = default(YogaCachedMeasurement);

            // Determine whether the results are already cached. We maintain a separate
            // cache for layouts and measurements. A layout operation modifies the
            // positions
            // and dimensions for nodes in the subtree. The algorithm assumes that each
            // node
            // gets layed out a maximum of one time per tree layout, but multiple
            // measurements
            // may be required to resolve all of the flex dimensions.
            // We handle nodes with measure functions specially here because they are the
            // most
            // expensive to measure, so it's worth avoiding redundant measurements if at
            // all possible.
            if (node.Measure != null)
            {
                var marginAxisRow = node.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);
                var marginAxisColumn = node.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);

                // First, try to use the layout cache.
                if (CanUseCachedMeasurement(widthMeasureMode,
                                                  availableWidth,
                                                  heightMeasureMode,
                                                  availableHeight,
                                                  layout.CachedLayout.WidthMeasureMode,
                                                  layout.CachedLayout.AvailableWidth,
                                                  layout.CachedLayout.HeightMeasureMode,
                                                  layout.CachedLayout.AvailableHeight,
                                                  layout.CachedLayout.ComputedWidth,
                                                  layout.CachedLayout.ComputedHeight,
                                                  marginAxisRow,
                                                  marginAxisColumn,
                                                  config))
                {
                    cachedResults = layout.CachedLayout;
                }
                else
                {
                    // Try to use the measurement cache.
                    for (var i = 0; i < layout.NextCachedMeasurementsIndex; i++)
                    {
                        if (CanUseCachedMeasurement(widthMeasureMode,
                                                          availableWidth,
                                                          heightMeasureMode,
                                                          availableHeight,
                                                          layout.CachedMeasurements[i].WidthMeasureMode,
                                                          layout.CachedMeasurements[i].AvailableWidth,
                                                          layout.CachedMeasurements[i].HeightMeasureMode,
                                                          layout.CachedMeasurements[i].AvailableHeight,
                                                          layout.CachedMeasurements[i].ComputedWidth,
                                                          layout.CachedMeasurements[i].ComputedHeight,
                                                          marginAxisRow,
                                                          marginAxisColumn,
                                                          config))
                        {
                            cachedResults = layout.CachedMeasurements[i];
                            break;
                        }
                    }
                }
            }
            else if (performLayout)
            {
                if (FloatsEqual(layout.CachedLayout.AvailableWidth, availableWidth) &&
                    FloatsEqual(layout.CachedLayout.AvailableHeight, availableHeight) &&
                    layout.CachedLayout.WidthMeasureMode == widthMeasureMode &&
                    layout.CachedLayout.HeightMeasureMode == heightMeasureMode)
                {
                    cachedResults = layout.CachedLayout;
                }
            }
            else
            {
                for (var i = 0; i < layout.NextCachedMeasurementsIndex; i++)
                {
                    if (FloatsEqual(layout.CachedMeasurements[i].AvailableWidth, availableWidth) &&
                        FloatsEqual(layout.CachedMeasurements[i].AvailableHeight, availableHeight) &&
                        layout.CachedMeasurements[i].WidthMeasureMode == widthMeasureMode &&
                        layout.CachedMeasurements[i].HeightMeasureMode == heightMeasureMode)
                    {
                        cachedResults = layout.CachedMeasurements[i];
                        break;
                    }
                }
            }

            if (!needToVisitNode && cachedResults != null)
            {
                layout.MeasuredDimensions[YogaDimension.Width] = cachedResults.ComputedWidth;
                layout.MeasuredDimensions[YogaDimension.Height] = cachedResults.ComputedHeight;
            }
            else
            {
                LayoutNode(node,
                                 availableWidth,
                                 availableHeight,
                                 ownerDirection,
                                 widthMeasureMode,
                                 heightMeasureMode,
                                 ownerWidth,
                                 ownerHeight,
                                 performLayout,
                                 config);

                layout.LastOwnerDirection = ownerDirection;

                if (cachedResults == null)
                {
                    if (layout.NextCachedMeasurementsIndex == YogaLayout.MaxCachedResultCount)
                        layout.NextCachedMeasurementsIndex = 0;

                    var newCacheEntry = default(YogaCachedMeasurement);
                    if (performLayout)
                    {
                        // Use the single layout cache entry.
                        newCacheEntry = layout.CachedLayout;
                    }
                    else
                    {
                        // Allocate a new measurement cache entry.
                        newCacheEntry = layout.CachedMeasurements[layout.NextCachedMeasurementsIndex];
                        layout.NextCachedMeasurementsIndex++;
                    }

                    newCacheEntry.AvailableWidth = availableWidth;
                    newCacheEntry.AvailableHeight = availableHeight;
                    newCacheEntry.WidthMeasureMode = widthMeasureMode;
                    newCacheEntry.HeightMeasureMode = heightMeasureMode;
                    newCacheEntry.ComputedWidth = layout.MeasuredDimensions[YogaDimension.Width];
                    newCacheEntry.ComputedHeight = layout.MeasuredDimensions[YogaDimension.Height];
                }
            }

            if (performLayout)
            {
                node.SetLayoutDimension(node.Layout.MeasuredDimensions[YogaDimension.Width], YogaDimension.Width);
                node.SetLayoutDimension(node.Layout.MeasuredDimensions[YogaDimension.Height], YogaDimension.Height);

                node.HasNewLayout = true;
                node.IsDirty = false;
            }

            gDepth--;
            layout.GenerationCount = gCurrentGenerationCount;
            return (needToVisitNode || cachedResults == null);
        }

        private static bool CanUseCachedMeasurement(YogaMeasureMode widthMode,
                                   float? width,
                                   YogaMeasureMode heightMode,
                                   float? height,
                                   YogaMeasureMode lastWidthMode,
                                   float? lastWidth,
                                   YogaMeasureMode lastHeightMode,
                                   float? lastHeight,
                                   float? lastComputedWidth,
                                   float? lastComputedHeight,
                                   float? marginRow,
                                   float? marginColumn,
                                   YogaConfig config)
        {
            if ((lastComputedHeight != null && lastComputedHeight < 0) || (lastComputedWidth != null && lastComputedWidth < 0))
                return false;

            var useRoundedComparison = config != null && config.PointScaleFactor != 0;
            var effectiveWidth = useRoundedComparison ? RoundValueToPixelGrid(width, config.PointScaleFactor, false, false) : width;
            var effectiveHeight = useRoundedComparison ? RoundValueToPixelGrid(height, config.PointScaleFactor, false, false) : height;
            var effectiveLastWidth = useRoundedComparison ? RoundValueToPixelGrid(lastWidth, config.PointScaleFactor, false, false) : lastWidth;
            var effectiveLastHeight = useRoundedComparison ? RoundValueToPixelGrid(lastHeight, config.PointScaleFactor, false, false) : lastHeight;

            var hasSameWidthSpec = lastWidthMode == widthMode && FloatsEqual(effectiveLastWidth, effectiveWidth);
            var hasSameHeightSpec = lastHeightMode == heightMode && FloatsEqual(effectiveLastHeight, effectiveHeight);

            var widthIsCompatible = hasSameWidthSpec
                || MeasureModeSizeIsExactAndMatchesOldMeasuredSize(widthMode, width - marginRow, lastComputedWidth)
                || MeasureModeOldSizeIsUnspecifiedAndStillFits(widthMode, width - marginRow, lastWidthMode, lastComputedWidth)
                || MeasureModeNewMeasureSizeIsStricterAndStillValid(widthMode, width - marginRow, lastWidthMode, lastWidth, lastComputedWidth);

            var heightIsCompatible = hasSameHeightSpec
                || MeasureModeSizeIsExactAndMatchesOldMeasuredSize(heightMode, height - marginColumn, lastComputedHeight)
                || MeasureModeOldSizeIsUnspecifiedAndStillFits(heightMode, height - marginColumn, lastHeightMode, lastComputedHeight)
                || MeasureModeNewMeasureSizeIsStricterAndStillValid(heightMode, height - marginColumn, lastHeightMode, lastHeight, lastComputedHeight);

            return widthIsCompatible && heightIsCompatible;
        }

        private static float? RoundValueToPixelGrid(float? value, float? pointScaleFactor, bool forceCeil, bool forceFloor)
        {
            var scaledValue = value * pointScaleFactor;
            var fractial = scaledValue % 1.0f;
            if (FloatsEqual(fractial, 0))
            {
                // First we check if the value is already rounded
                scaledValue = scaledValue - fractial;
            }
            else if (FloatsEqual(fractial, 1.0f))
            {
                scaledValue = scaledValue - fractial + 1.0f;
            }
            else if (forceCeil)
            {
                // Next we check if we need to use forced rounding
                scaledValue = scaledValue - fractial + 1.0f;
            }
            else if (forceFloor)
            {
                scaledValue = scaledValue - fractial;
            }
            else
            {
                // Finally we just round the value
                scaledValue = scaledValue - fractial + ((fractial != null) && (fractial > 0.5f || FloatsEqual(fractial, 0.5f)) ? 1.0f : 0.0f);
            }
            return ((scaledValue == null) || pointScaleFactor == null) ? default(float?) : scaledValue / pointScaleFactor;
        }

        private static bool MeasureModeSizeIsExactAndMatchesOldMeasuredSize(YogaMeasureMode sizeMode, float? size, float? lastComputedSize)
        {
            return sizeMode == YogaMeasureMode.Exactly
                && FloatsEqual(size, lastComputedSize);
        }

        private static bool MeasureModeOldSizeIsUnspecifiedAndStillFits(YogaMeasureMode sizeMode, float? size, YogaMeasureMode lastSizeMode, float? lastComputedSize)
        {
            return sizeMode == YogaMeasureMode.AtMost
                && lastSizeMode == YogaMeasureMode.Undefined
                && ((size > lastComputedSize || size == lastComputedSize) || FloatsEqual(size, lastComputedSize));
        }

        private static bool MeasureModeNewMeasureSizeIsStricterAndStillValid(YogaMeasureMode sizeMode, float? size, YogaMeasureMode lastSizeMode, float? lastSize, float? lastComputedSize)
        {
            return lastSizeMode == YogaMeasureMode.AtMost
                && sizeMode == YogaMeasureMode.AtMost
                && size != null
                && lastSize != null
                && lastComputedSize != null
                && lastSize > size
                && (lastComputedSize < size || lastComputedSize == size || FloatsEqual(size, lastComputedSize));
        }

        //
        // This is the main routine that implements a subset of the flexbox layout
        // algorithm
        // described in the W3C YG documentation: https://www.w3.org/TR/YG3-flexbox/.
        //
        // Limitations of this algorithm, compared to the full standard:
        //  * Display property is always assumed to be 'flex' except for Text nodes,
        //  which
        //    are assumed to be 'inline-flex'.
        //  * The 'zIndex' property (or any form of z ordering) is not supported. Nodes
        //  are
        //    stacked in document order.
        //  * The 'order' property is not supported. The order of flex items is always
        //  defined
        //    by document order.
        //  * The 'visibility' property is always assumed to be 'visible'. Values of
        //  'collapse'
        //    and 'hidden' are not supported.
        //  * There is no support for forced breaks.
        //  * It does not support vertical inline directions (top-to-bottom or
        //  bottom-to-top text).
        //
        // Deviations from standard:
        //  * Section 4.5 of the spec indicates that all flex items have a default
        //  minimum
        //    main size. For text blocks, for example, this is the width of the widest
        //    word.
        //    Calculating the minimum width is expensive, so we forego it and assume a
        //    default
        //    minimum main size of 0.
        //  * Min/Max sizes in the main axis are not honored when resolving flexible
        //  lengths.
        //  * The spec indicates that the default value for 'flexDirection' is 'row',
        //  but
        //    the algorithm below assumes a default of 'column'.
        //
        // Input parameters:
        //    - node: current node to be sized and layed out
        //    - availableWidth & availableHeight: available size to be used for sizing
        //    the node
        //      or YGUndefined if the size is not available; interpretation depends on
        //      layout
        //      flags
        //    - ownerDirection: the inline (text) direction within the owner
        //    (left-to-right or
        //      right-to-left)
        //    - widthMeasureMode: indicates the sizing rules for the width (see below
        //    for explanation)
        //    - heightMeasureMode: indicates the sizing rules for the height (see below
        //    for explanation)
        //    - performLayout: specifies whether the caller is interested in just the
        //    dimensions
        //      of the node or it requires the entire node and its subtree to be layed
        //      out
        //      (with final positions)
        //
        // Details:
        //    This routine is called recursively to lay out subtrees of flexbox
        //    elements. It uses the
        //    information in node.style, which is treated as a read-only input. It is
        //    responsible for
        //    setting the layout.direction and layout.measuredDimensions fields for the
        //    input node as well
        //    as the layout.position and layout.lineIndex fields for its child nodes.
        //    The
        //    layout.measuredDimensions field includes any border or padding for the
        //    node but does
        //    not include margins.
        //
        //    The spec describes four different layout modes: "fill available", "max
        //    content", "min
        //    content",
        //    and "fit content". Of these, we don't use "min content" because we don't
        //    support default
        //    minimum main sizes (see above for details). Each of our measure modes maps
        //    to a layout mode
        //    from the spec (https://www.w3.org/TR/YG3-sizing/#terms):
        //      - YGMeasureModeUndefined: max content
        //      - YGMeasureModeExactly: fill available
        //      - YGMeasureModeAtMost: fit content
        //
        //    When calling YGNodelayoutImpl and YGLayoutNodeInternal, if the caller passes
        //    an available size of
        //    undefined then it must also pass a measure mode of YGMeasureModeUndefined
        //    in that dimension.
        //
        private static void LayoutNode(YogaNode node,
                              float? availableWidth,
                              float? availableHeight,
                              YogaDirection ownerDirection,
                              YogaMeasureMode widthMeasureMode,
                              YogaMeasureMode heightMeasureMode,
                              float? ownerWidth,
                              float? ownerHeight,
                              bool performLayout,
                              YogaConfig config)
        {

            // Set the resolved resolution in the node's layout.
            var direction = node.ResolveDirection(ownerDirection);
            node.SetLayoutDirection(direction);

            var flexRowDirection = ResolveFlexDirection(YogaFlexDirection.Row, direction);
            var flexColumnDirection = ResolveFlexDirection(YogaFlexDirection.Column, direction);

            node.SetLayoutMargin(node.GetLeadingMargin(flexRowDirection, ownerWidth), YogaEdge.Start);
            node.SetLayoutMargin(node.GetTrailingMargin(flexRowDirection, ownerWidth), YogaEdge.End);
            node.SetLayoutMargin(node.GetLeadingMargin(flexColumnDirection, ownerWidth), YogaEdge.Top);
            node.SetLayoutMargin(node.GetTrailingMargin(flexColumnDirection, ownerWidth), YogaEdge.Bottom);

            node.SetLayoutBorder(node.GetLeadingBorder(flexRowDirection), YogaEdge.Start);
            node.SetLayoutBorder(node.GetTrailingBorder(flexRowDirection), YogaEdge.End);
            node.SetLayoutBorder(node.GetLeadingBorder(flexColumnDirection), YogaEdge.Top);
            node.SetLayoutBorder(node.GetTrailingBorder(flexColumnDirection), YogaEdge.Bottom);

            node.SetLayoutPadding(node.GetLeadingPadding(flexRowDirection, ownerWidth), YogaEdge.Start);
            node.SetLayoutPadding(node.GetTrailingPadding(flexRowDirection, ownerWidth), YogaEdge.End);
            node.SetLayoutPadding(node.GetLeadingPadding(flexColumnDirection, ownerWidth), YogaEdge.Top);
            node.SetLayoutPadding(node.GetTrailingPadding(flexColumnDirection, ownerWidth), YogaEdge.Bottom);

            if (node.Measure != null)
            {
                WithMeasureFuncSetMeasuredDimensions(node,
                                                           availableWidth,
                                                           availableHeight,
                                                           widthMeasureMode,
                                                           heightMeasureMode,
                                                           ownerWidth,
                                                           ownerHeight);
                return;
            }

            var childCount = GetChildCount(node);
            if (childCount == 0)
            {
                EmptyContainerSetMeasuredDimensions(node,
                                                          availableWidth,
                                                          availableHeight,
                                                          widthMeasureMode,
                                                          heightMeasureMode,
                                                          ownerWidth,
                                                          ownerHeight);
                return;
            }

            // If we're not being asked to perform a full layout we can skip the algorithm if we already know
            // the size
            if (!performLayout && FixedSizeSetMeasuredDimensions(node,
                                                                       availableWidth,
                                                                       availableHeight,
                                                                       widthMeasureMode,
                                                                       heightMeasureMode,
                                                                       ownerWidth,
                                                                       ownerHeight))
            {
                return;
            }

            // At this point we know we're going to perform work. Ensure that each child has a mutable copy.
            node.CloneChildrenIfNeeded();
            // Reset layout flags, as they could have changed.
            node.SetLayoutHadOverflow(false);

            // STEP 1: CALCULATE VALUES FOR REMAINDER OF ALGORITHM
            var mainAxis = ResolveFlexDirection(node.Style.FlexDirection, direction);
            var crossAxis = FlexDirectionCross(mainAxis, direction);
            var isMainAxisRow = FlexDirectionIsRow(mainAxis);
            var isNodeFlexWrap = node.Style.FlexWrap != YogaWrap.NoWrap;

            var mainAxisOwnerSize = isMainAxisRow ? ownerWidth : ownerHeight;
            var crossAxisOwnerSize = isMainAxisRow ? ownerHeight : ownerWidth;

            var leadingPaddingAndBorderCross = node.GetLeadingPaddingAndBorder(crossAxis, ownerWidth);
            var paddingAndBorderAxisMain = PaddingAndBorderForAxis(node, mainAxis, ownerWidth);
            var paddingAndBorderAxisCross = PaddingAndBorderForAxis(node, crossAxis, ownerWidth);

            var measureModeMainDim = isMainAxisRow ? widthMeasureMode : heightMeasureMode;
            var measureModeCrossDim = isMainAxisRow ? heightMeasureMode : widthMeasureMode;

            var paddingAndBorderAxisRow = isMainAxisRow ? paddingAndBorderAxisMain : paddingAndBorderAxisCross;
            var paddingAndBorderAxisColumn = isMainAxisRow ? paddingAndBorderAxisCross : paddingAndBorderAxisMain;

            var marginAxisRow = node.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);
            var marginAxisColumn = node.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);

            var minInnerWidth = ResolveValue(node.Style.MinDimensions[YogaDimension.Width], ownerWidth) - paddingAndBorderAxisRow;
            var maxInnerWidth = ResolveValue(node.Style.MaxDimensions[YogaDimension.Width], ownerWidth) - paddingAndBorderAxisRow;
            var minInnerHeight = ResolveValue(node.Style.MinDimensions[YogaDimension.Height], ownerHeight) - paddingAndBorderAxisColumn;
            var maxInnerHeight = ResolveValue(node.Style.MaxDimensions[YogaDimension.Height], ownerHeight) - paddingAndBorderAxisColumn;

            var minInnerMainDim = isMainAxisRow ? minInnerWidth : minInnerHeight;
            var maxInnerMainDim = isMainAxisRow ? maxInnerWidth : maxInnerHeight;

            // STEP 2: DETERMINE AVAILABLE SIZE IN MAIN AND CROSS DIRECTIONS

            var availableInnerWidth = CalculateAvailableInnerDim(node, YogaFlexDirection.Row, availableWidth, ownerWidth);
            var availableInnerHeight = CalculateAvailableInnerDim(node, YogaFlexDirection.Column, availableHeight, ownerHeight);

            var availableInnerMainDim = isMainAxisRow ? availableInnerWidth : availableInnerHeight;
            var availableInnerCrossDim = isMainAxisRow ? availableInnerHeight : availableInnerWidth;

            var totalOuterFlexBasis = (float?)0F;

            // STEP 3: DETERMINE FLEX BASIS FOR EACH ITEM

            ComputeFlexBasisForChildren(
                node,
                availableInnerWidth,
                availableInnerHeight,
                widthMeasureMode,
                heightMeasureMode,
                direction,
                mainAxis,
                config,
                performLayout,
                ref totalOuterFlexBasis);

            var flexBasisOverflows = measureModeMainDim == YogaMeasureMode.Undefined ? false : totalOuterFlexBasis > availableInnerMainDim;
            if (isNodeFlexWrap && flexBasisOverflows && measureModeMainDim == YogaMeasureMode.AtMost)
                measureModeMainDim = YogaMeasureMode.Exactly;

            // STEP 4: COLLECT FLEX ITEMS INTO FLEX LINES

            // Indexes of children that represent the first and last items in the line.
            var startOfLineIndex = 0;
            var endOfLineIndex = 0;

            // Number of lines.
            var lineCount = 0;

            // Accumulated cross dimensions of all lines so far.
            var totalLineCrossDim = (float?)0F;

            // Max main dimension of all the lines.
            var maxLineMainDim = 0F;

            var collectedFlexItemsValues = default(YogaCollectFlexItemsRowValues);
            for (; endOfLineIndex < childCount; lineCount++, startOfLineIndex = endOfLineIndex)
            {
                collectedFlexItemsValues = CalculateCollectFlexItemsRowValues(
                    ref node,
                    ownerDirection,
                    mainAxisOwnerSize,
                    availableInnerWidth,
                    availableInnerMainDim,
                    startOfLineIndex,
                    lineCount);

                endOfLineIndex = collectedFlexItemsValues.EndOfLineIndex;

                // If we don't need to measure the cross axis, we can skip the entire flex
                // step.
                var canSkipFlex = !performLayout && measureModeCrossDim == YogaMeasureMode.Exactly;

                // STEP 5: RESOLVING FLEXIBLE LENGTHS ON MAIN AXIS
                // Calculate the remaining available space that needs to be allocated.
                // If the main dimension size isn't known, it is computed based on
                // the line length, so there's no more space left to distribute.

                var sizeBasedOnContent = false;
                // If we don't measure with exact main dimension we want to ensure we don't violate min and max
                if (measureModeMainDim != YogaMeasureMode.Exactly)
                {
                    if (minInnerMainDim != null && collectedFlexItemsValues.SizeConsumedOnCurrentLine < minInnerMainDim)
                    {
                        availableInnerMainDim = minInnerMainDim.Value;
                    }
                    else if (maxInnerMainDim != null && collectedFlexItemsValues.SizeConsumedOnCurrentLine > maxInnerMainDim)
                    {
                        availableInnerMainDim = maxInnerMainDim.Value;
                    }
                    else
                    {
                        if (!node.Config.UseLegacyStretchBehaviour
                            && ((collectedFlexItemsValues.TotalFlexGrowFactors != null && collectedFlexItemsValues.TotalFlexGrowFactors == 0)
                            || (node.ResolveFlexGrow() == 0)))
                        {
                            // If we don't have any children to flex or we can't flex the node
                            // itself, space we've used is all space we need. Root node also
                            // should be shrunk to minimum
                            availableInnerMainDim = collectedFlexItemsValues.SizeConsumedOnCurrentLine;
                        }

                        if (node.Config.UseLegacyStretchBehaviour)
                            node.SetLayoutDidUseLegacyFlag(true);

                        sizeBasedOnContent = !node.Config.UseLegacyStretchBehaviour;
                    }
                }

                if (!sizeBasedOnContent && availableInnerMainDim != null)
                {
                    collectedFlexItemsValues.RemainingFreeSpace = availableInnerMainDim.Value - collectedFlexItemsValues.SizeConsumedOnCurrentLine;
                }
                else if (collectedFlexItemsValues.SizeConsumedOnCurrentLine < 0)
                {
                    // availableInnerMainDim is indefinite which means the node is being sized based on its
                    // content.
                    // sizeConsumedOnCurrentLine is negative which means the node will allocate 0 points for
                    // its content. Consequently, remainingFreeSpace is 0 - sizeConsumedOnCurrentLine.
                    collectedFlexItemsValues.RemainingFreeSpace = -collectedFlexItemsValues.SizeConsumedOnCurrentLine;
                }

                if (!canSkipFlex)
                {
                    ResolveFlexibleLength(
                        node,
                        ref collectedFlexItemsValues,
                        mainAxis,
                        crossAxis,
                        mainAxisOwnerSize,
                        availableInnerMainDim,
                        availableInnerCrossDim,
                        availableInnerWidth,
                        availableInnerHeight,
                        flexBasisOverflows,
                        measureModeCrossDim,
                        performLayout,
                        config);
                }

                node.SetLayoutHadOverflow(node.Layout.HadOverflow | (collectedFlexItemsValues.RemainingFreeSpace < 0));

                // STEP 6: MAIN-AXIS JUSTIFICATION & CROSS-AXIS SIZE DETERMINATION

                // At this point, all the children have their dimensions set in the main
                // axis.
                // Their dimensions are also set in the cross axis with the exception of
                // items
                // that are aligned "stretch". We need to compute these stretch values and
                // set the final positions.

                JustifyMainAxis(
                    node,
                    ref collectedFlexItemsValues,
                    ref startOfLineIndex,
                    ref mainAxis,
                    ref crossAxis,
                    ref measureModeMainDim,
                    ref measureModeCrossDim,
                    ref mainAxisOwnerSize,
                    ref ownerWidth,
                    ref availableInnerMainDim,
                    ref availableInnerCrossDim,
                    ref availableInnerWidth,
                    ref performLayout);

                var containerCrossAxis = availableInnerCrossDim;
                if (measureModeCrossDim == YogaMeasureMode.Undefined || measureModeCrossDim == YogaMeasureMode.AtMost)
                {
                    // Compute the cross axis from the max cross dimension of the children.
                    containerCrossAxis =
                        BoundAxis(
                            node,
                            crossAxis,
                            collectedFlexItemsValues.CrossDimension + paddingAndBorderAxisCross,
                            crossAxisOwnerSize,
                            ownerWidth) -
                        paddingAndBorderAxisCross;
                }

                // If there's no flex wrap, the cross dimension is defined by the container.
                if (!isNodeFlexWrap && measureModeCrossDim == YogaMeasureMode.Exactly)
                {
                    collectedFlexItemsValues.CrossDimension = availableInnerCrossDim;
                }

                // Clamp to the min/max size specified on the container.
                collectedFlexItemsValues.CrossDimension =
                    BoundAxis(
                        node,
                        crossAxis,
                        collectedFlexItemsValues.CrossDimension + paddingAndBorderAxisCross,
                        crossAxisOwnerSize,
                        ownerWidth) -
                    paddingAndBorderAxisCross;

                // STEP 7: CROSS-AXIS ALIGNMENT
                // We can skip child alignment if we're just measuring the container.
                if (performLayout)
                {
                    for (var i = startOfLineIndex; i < endOfLineIndex; i++)
                    {
                        var child = node.GetChild(i);
                        if (child.Style.Display == YogaDisplay.None)
                            continue;

                        if (child.Style.PositionType == YogaPositionType.Absolute)
                        {
                            // If the child is absolutely positioned and has a
                            // top/left/bottom/right set, override
                            // all the previously computed positions to set it correctly.
                            var isChildLeadingPosDefined = child.IsLeadingPositionDefined(crossAxis);
                            if (isChildLeadingPosDefined)
                                child.SetLayoutPosition(child.GetLeadingPosition(crossAxis, availableInnerCrossDim) + node.GetLeadingBorder(crossAxis) + child.GetLeadingMargin(crossAxis, availableInnerWidth), Position[crossAxis]);

                            // If leading position is not defined or calculations result in Nan, default to border + margin
                            if (!isChildLeadingPosDefined || child.Layout.Position[Position[crossAxis]] == null)
                                child.SetLayoutPosition(node.GetLeadingBorder(crossAxis) + (child.GetLeadingMargin(crossAxis, availableInnerWidth)), Position[crossAxis]);
                        }
                        else
                        {
                            var leadingCrossDim = leadingPaddingAndBorderCross;

                            // For a relative children, we're either using alignItems (owner) or
                            // alignSelf (child) in order to determine the position in the cross
                            // axis
                            var alignItem = AlignItem(node, child);

                            // If the child uses align stretch, we need to lay it out one more
                            // time, this time
                            // forcing the cross-axis size to be the computed cross size for the
                            // current line.
                            if (alignItem == YogaAlign.Stretch &&
                                child.GetMarginLeadingValue(crossAxis).Unit != YogaUnit.Auto &&
                                child.GetMarginTrailingValue(crossAxis).Unit != YogaUnit.Auto)
                            {
                                // If the child defines a definite size for its cross axis, there's
                                // no need to stretch.
                                if (!IsStyleDimDefined(child, crossAxis, availableInnerCrossDim))
                                {
                                    var childMainSize = child.Layout.MeasuredDimensions[Dimension[mainAxis]];
                                    var childCrossSize =
                                        child.Style.AspectRatio != null
                                        ? ((child.GetMarginForAxis(crossAxis, availableInnerWidth) +
                                            (isMainAxisRow
                                                 ? childMainSize / child.Style.AspectRatio
                                                 : childMainSize * child.Style.AspectRatio)))
                                        : collectedFlexItemsValues.CrossDimension;

                                    childMainSize += child.GetMarginForAxis(mainAxis, availableInnerWidth);

                                    var childMainMeasureMode = YogaMeasureMode.Exactly;
                                    var childCrossMeasureMode = YogaMeasureMode.Exactly;
                                    ConstrainMaxSizeForMode(child, mainAxis, availableInnerMainDim, availableInnerWidth, ref childMainMeasureMode, ref childMainSize);
                                    ConstrainMaxSizeForMode(child, crossAxis, availableInnerCrossDim, availableInnerWidth, ref childCrossMeasureMode, ref childCrossSize);

                                    var childWidth = isMainAxisRow ? childMainSize : childCrossSize;
                                    var childHeight = !isMainAxisRow ? childMainSize : childCrossSize;

                                    var childWidthMeasureMode = (childWidth == null) ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;
                                    var childHeightMeasureMode = (childHeight == null) ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;

                                    LayoutNode(
                                        child,
                                        childWidth,
                                        childHeight,
                                        direction,
                                        childWidthMeasureMode,
                                        childHeightMeasureMode,
                                        availableInnerWidth,
                                        availableInnerHeight,
                                        true,
                                        "stretch",
                                        config);
                                }
                            }
                            else
                            {
                                var remainingCrossDim = containerCrossAxis - DimWithMargin(child, crossAxis, availableInnerWidth);

                                if (child.GetMarginLeadingValue(crossAxis).Unit == YogaUnit.Auto && child.GetMarginTrailingValue(crossAxis).Unit == YogaUnit.Auto)
                                {
                                    leadingCrossDim += YogaMath.Max(0.0f, remainingCrossDim / 2);
                                }
                                else if (child.GetMarginTrailingValue(crossAxis).Unit == YogaUnit.Auto)
                                {
                                    // No-Op
                                }
                                else if (child.GetMarginLeadingValue(crossAxis).Unit == YogaUnit.Auto)
                                {
                                    leadingCrossDim += YogaMath.Max(0.0f, remainingCrossDim);
                                }
                                else if (alignItem == YogaAlign.FlexStart)
                                {
                                    // No-Op
                                }
                                else if (alignItem == YogaAlign.Center)
                                {
                                    leadingCrossDim += (remainingCrossDim ?? 0) / 2;
                                }
                                else
                                {
                                    leadingCrossDim += (remainingCrossDim ?? 0);
                                }
                            }

                            // And we apply the position
                            child.SetLayoutPosition(child.Layout.Position[Position[crossAxis]] + totalLineCrossDim + leadingCrossDim, Position[crossAxis]);
                        }
                    }
                }

                totalLineCrossDim += collectedFlexItemsValues.CrossDimension;
                maxLineMainDim = YogaMath.Max(maxLineMainDim, collectedFlexItemsValues.MainDimension);
            }

            // STEP 8: MULTI-LINE CONTENT ALIGNMENT
            if (performLayout && (lineCount > 1 || IsBaselineLayout(node)) && availableInnerCrossDim != null)
            {
                var remainingAlignContentDim = availableInnerCrossDim.Value - totalLineCrossDim;

                var crossDimLead = (float?)0F;
                var currentLead = (float?)leadingPaddingAndBorderCross;

                switch (node.Style.AlignContent)
                {
                    case YogaAlign.FlexEnd:
                        currentLead += remainingAlignContentDim;
                        break;
                    case YogaAlign.Center:
                        currentLead += remainingAlignContentDim / 2;
                        break;
                    case YogaAlign.Stretch:
                        if (availableInnerCrossDim > totalLineCrossDim)
                        {
                            crossDimLead = remainingAlignContentDim / lineCount;
                        }
                        break;
                    case YogaAlign.SpaceAround:
                        if (availableInnerCrossDim > totalLineCrossDim)
                        {
                            currentLead += remainingAlignContentDim / (2 * lineCount);
                            if (lineCount > 1)
                            {
                                crossDimLead = remainingAlignContentDim / lineCount;
                            }
                        }
                        else
                        {
                            currentLead += remainingAlignContentDim / 2;
                        }
                        break;
                    case YogaAlign.SpaceBetween:
                        if (availableInnerCrossDim > totalLineCrossDim && lineCount > 1)
                        {
                            crossDimLead = remainingAlignContentDim / (lineCount - 1);
                        }
                        break;
                    case YogaAlign.Auto:
                    case YogaAlign.FlexStart:
                    case YogaAlign.Baseline:
                        break;
                }

                var endIndex = 0;
                for (var i = 0; i < lineCount; i++)
                {
                    var startIndex = endIndex;
                    var ii = default(int);

                    // compute the line's height and find the endIndex
                    var lineHeight = (float?)0F;
                    var maxAscentForCurrentLine = 0F;
                    var maxDescentForCurrentLine = 0F;
                    for (ii = startIndex; ii < childCount; ii++)
                    {
                        var child = node.GetChild(ii);
                        if (child.Style.Display == YogaDisplay.None)
                            continue;

                        if (child.Style.PositionType == YogaPositionType.Relative)
                        {
                            if (child.LineIndex != i)
                                break;

                            if (IsLayoutDimDefined(child, crossAxis))
                            {
                                lineHeight = YogaMath.Max(lineHeight, child.Layout.MeasuredDimensions[Dimension[crossAxis]] + child.GetMarginForAxis(crossAxis, availableInnerWidth));
                            }

                            if (AlignItem(node, child) == YogaAlign.Baseline)
                            {
                                var ascent = CalculateBaseline(child) + child.GetLeadingMargin(YogaFlexDirection.Column, availableInnerWidth);
                                var descent = child.Layout.MeasuredDimensions[YogaDimension.Height] + child.GetMarginForAxis(YogaFlexDirection.Column, availableInnerWidth) - ascent;

                                maxAscentForCurrentLine = YogaMath.Max(maxAscentForCurrentLine, ascent);
                                maxDescentForCurrentLine = YogaMath.Max(maxDescentForCurrentLine, descent);
                                lineHeight = YogaMath.Max(lineHeight, maxAscentForCurrentLine + maxDescentForCurrentLine);
                            }
                        }
                    }
                    endIndex = ii;
                    lineHeight += crossDimLead;

                    if (performLayout)
                    {
                        for (ii = startIndex; ii < endIndex; ii++)
                        {
                            var child = node.GetChild(ii);
                            if (child.Style.Display == YogaDisplay.None)
                                continue;

                            if (child.Style.PositionType == YogaPositionType.Relative)
                            {
                                switch (AlignItem(node, child))
                                {
                                    case YogaAlign.FlexStart:
                                        {
                                            child.SetLayoutPosition(currentLead + child.GetLeadingMargin(crossAxis, availableInnerWidth), Position[crossAxis]);
                                            break;
                                        }
                                    case YogaAlign.FlexEnd:
                                        {
                                            child.SetLayoutPosition(currentLead + lineHeight - child.GetTrailingMargin(crossAxis, availableInnerWidth) - child.Layout.MeasuredDimensions[Dimension[crossAxis]], Position[crossAxis]);
                                            break;
                                        }
                                    case YogaAlign.Center:
                                        {
                                            var childHeight = child.Layout.MeasuredDimensions[Dimension[crossAxis]];
                                            child.SetLayoutPosition(currentLead + (lineHeight - childHeight) / 2, Position[crossAxis]);
                                            break;
                                        }
                                    case YogaAlign.Stretch:
                                        {
                                            child.SetLayoutPosition(currentLead + child.GetLeadingMargin(crossAxis, availableInnerWidth), Position[crossAxis]);

                                            // Remeasure child with the line height as it as been only measured with the
                                            // owners height yet.
                                            if (!IsStyleDimDefined(child, crossAxis, availableInnerCrossDim))
                                            {
                                                var childWidth = isMainAxisRow
                                                   ? (child.Layout.MeasuredDimensions[YogaDimension.Width] + child.GetMarginForAxis(mainAxis, availableInnerWidth))
                                                   : lineHeight;

                                                var childHeight = !isMainAxisRow
                                                   ? (child.Layout.MeasuredDimensions[YogaDimension.Height] + child.GetMarginForAxis(crossAxis, availableInnerWidth))
                                                   : lineHeight;

                                                if (!(FloatsEqual(childWidth, child.Layout.MeasuredDimensions[YogaDimension.Width])
                                                    && FloatsEqual(childHeight, child.Layout.MeasuredDimensions[YogaDimension.Height])))
                                                {
                                                    LayoutNode(child,
                                                                         childWidth,
                                                                         childHeight,
                                                                         direction,
                                                                         YogaMeasureMode.Exactly,
                                                                         YogaMeasureMode.Exactly,
                                                                         availableInnerWidth,
                                                                         availableInnerHeight,
                                                                         true,
                                                                         "multiline-stretch",
                                                                         config);
                                                }
                                            }
                                            break;
                                        }
                                    case YogaAlign.Baseline:
                                        {
                                            child.SetLayoutPosition(currentLead + maxAscentForCurrentLine - CalculateBaseline(child) + child.GetLeadingPosition(YogaFlexDirection.Column, availableInnerCrossDim), YogaEdge.Top);

                                            break;
                                        }
                                    case YogaAlign.Auto:
                                    case YogaAlign.SpaceBetween:
                                    case YogaAlign.SpaceAround:
                                        break;
                                }
                            }
                        }
                    }

                    currentLead += lineHeight;
                }
            }

            // STEP 9: COMPUTING FINAL DIMENSIONS

            node.SetLayoutMeasuredDimension(
                BoundAxis(
                    node,
                    YogaFlexDirection.Row,
                    availableWidth - marginAxisRow,
                    ownerWidth,
                    ownerWidth),
                YogaDimension.Width);

            node.SetLayoutMeasuredDimension(
                BoundAxis(
                    node,
                    YogaFlexDirection.Column,
                    availableHeight - marginAxisColumn,
                    ownerHeight,
                    ownerWidth),
                YogaDimension.Height);

            // If the user didn't specify a width or height for the node, set the
            // dimensions based on the children.
            if (measureModeMainDim == YogaMeasureMode.Undefined
                || (node.Style.Overflow != YogaOverflow.Scroll
                && measureModeMainDim == YogaMeasureMode.AtMost))
            {
                // Clamp the size to the min/max size, if specified, and make sure it
                // doesn't go below the padding and border amount.
                node.SetLayoutMeasuredDimension(BoundAxis(node, mainAxis, maxLineMainDim, mainAxisOwnerSize, ownerWidth), Dimension[mainAxis]);

            }
            else if (measureModeMainDim == YogaMeasureMode.AtMost
                && node.Style.Overflow == YogaOverflow.Scroll)
            {
                node.SetLayoutMeasuredDimension(YogaMath.Max(YogaMath.Min(availableInnerMainDim + paddingAndBorderAxisMain, BoundAxisWithinMinAndMax(node, mainAxis, maxLineMainDim, mainAxisOwnerSize)), paddingAndBorderAxisMain), Dimension[mainAxis]);
            }

            if (measureModeCrossDim == YogaMeasureMode.Undefined
                || (node.Style.Overflow != YogaOverflow.Scroll
                && measureModeCrossDim == YogaMeasureMode.AtMost))
            {
                // Clamp the size to the min/max size, if specified, and make sure it
                // doesn't go below the padding and border amount.

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        crossAxis,
                        totalLineCrossDim + paddingAndBorderAxisCross,
                        crossAxisOwnerSize,
                        ownerWidth),
                    Dimension[crossAxis]);

            }
            else if (measureModeCrossDim == YogaMeasureMode.AtMost
                && node.Style.Overflow == YogaOverflow.Scroll)
            {
                node.SetLayoutMeasuredDimension(
                    YogaMath.Max(
                        YogaMath.Min(
                            availableInnerCrossDim + paddingAndBorderAxisCross,
                            BoundAxisWithinMinAndMax(
                                node,
                                crossAxis,
                                totalLineCrossDim + paddingAndBorderAxisCross,
                                crossAxisOwnerSize)),
                        paddingAndBorderAxisCross),
                    Dimension[crossAxis]);
            }

            // As we only wrapped in normal direction yet, we need to reverse the positions on wrap-reverse.
            if (performLayout && node.Style.FlexWrap == YogaWrap.WrapReverse)
            {
                for (var i = 0; i < childCount; i++)
                {
                    var child = GetChild(node, i);
                    if (child.Style.PositionType == YogaPositionType.Relative)
                    {
                        child.SetLayoutPosition(
                            node.Layout.MeasuredDimensions[Dimension[crossAxis]] -
                                child.Layout.Position[Position[crossAxis]] -
                                child.Layout.MeasuredDimensions[Dimension[crossAxis]],
                            Position[crossAxis]);
                    }
                }
            }

            if (performLayout)
            {
                // STEP 10: SIZING AND POSITIONING ABSOLUTE CHILDREN
                foreach (var child in node.Children)
                {
                    if (child.Style.PositionType != YogaPositionType.Absolute)
                        continue;

                    AbsoluteLayoutChild(
                        node,
                        child,
                        availableInnerWidth,
                        isMainAxisRow ? measureModeMainDim : measureModeCrossDim,
                        availableInnerHeight,
                        direction,
                        config);
                }

                // STEP 11: SETTING TRAILING POSITIONS FOR CHILDREN
                var needsMainTrailingPos = mainAxis == YogaFlexDirection.RowReverse || mainAxis == YogaFlexDirection.ColumnReverse;
                var needsCrossTrailingPos = crossAxis == YogaFlexDirection.RowReverse || crossAxis == YogaFlexDirection.ColumnReverse;

                // Set trailing position if necessary.
                if (needsMainTrailingPos || needsCrossTrailingPos)
                {
                    for (var i = 0; i < childCount; i++)
                    {
                        var child = node.GetChild(i);
                        if (child.Style.Display == YogaDisplay.None)
                            continue;

                        if (needsMainTrailingPos)
                            SetChildTrailingPosition(node, child, mainAxis);

                        if (needsCrossTrailingPos)
                            SetChildTrailingPosition(node, child, crossAxis);
                    }
                }
            }
        }

        private static void WithMeasureFuncSetMeasuredDimensions(YogaNode node,
                                                            float? availableWidth,
                                                            float? availableHeight,
                                                            YogaMeasureMode widthMeasureMode,
                                                            YogaMeasureMode heightMeasureMode,
                                                            float? ownerWidth,
                                                            float? ownerHeight)
        {
            if (node.Measure == null)
                throw new InvalidOperationException("Measure must not be null");

            var paddingAndBorderAxisRow = PaddingAndBorderForAxis(node, YogaFlexDirection.Row, availableWidth);
            var paddingAndBorderAxisColumn = PaddingAndBorderForAxis(node, YogaFlexDirection.Column, availableWidth);
            var marginAxisRow = node.GetMarginForAxis(YogaFlexDirection.Row, availableWidth);
            var marginAxisColumn = node.GetMarginForAxis(YogaFlexDirection.Column, availableWidth);

            // We want to make sure we don't call measure with negative size
            var innerWidth = availableWidth == null ? availableWidth : YogaMath.Max(0, availableWidth - marginAxisRow - paddingAndBorderAxisRow);
            var innerHeight = availableHeight == null ? availableHeight : YogaMath.Max(0, availableHeight - marginAxisColumn - paddingAndBorderAxisColumn);

            if (widthMeasureMode == YogaMeasureMode.Exactly
                && heightMeasureMode == YogaMeasureMode.Exactly)
            {
                // Don't bother sizing the text if both dimensions are already defined.
                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Row,
                        availableWidth - marginAxisRow,
                        ownerWidth,
                        ownerWidth),
                    YogaDimension.Width);

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Column,
                        availableHeight - marginAxisColumn,
                        ownerHeight,
                        ownerWidth),
                    YogaDimension.Height);
            }
            else
            {
                // Measure the text under the current raints.
                var measuredSize = node.Measure(node, innerWidth, widthMeasureMode, innerHeight, heightMeasureMode);

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Row,
                        (widthMeasureMode == YogaMeasureMode.Undefined ||
                         widthMeasureMode == YogaMeasureMode.AtMost)
                            ? measuredSize.Width + paddingAndBorderAxisRow
                            : availableWidth - marginAxisRow,
                        ownerWidth,
                        ownerWidth),
                    YogaDimension.Width);

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Column,
                        (heightMeasureMode == YogaMeasureMode.Undefined ||
                         heightMeasureMode == YogaMeasureMode.AtMost)
                            ? measuredSize.Height + paddingAndBorderAxisColumn
                            : availableHeight - marginAxisColumn,
                        ownerHeight,
                        ownerWidth),
                    YogaDimension.Height);
            }
        }

        // For nodes with no children, use the available values if they were provided,
        // or the minimum size as indicated by the padding and border sizes.
        private static void EmptyContainerSetMeasuredDimensions(YogaNode node,
                                                      float? availableWidth,
                                                      float? availableHeight,
                                                      YogaMeasureMode widthMeasureMode,
                                                      YogaMeasureMode heightMeasureMode,
                                                      float? ownerWidth,
                                                      float? ownerHeight)
        {
            var paddingAndBorderAxisRow = PaddingAndBorderForAxis(node, YogaFlexDirection.Row, ownerWidth);
            var paddingAndBorderAxisColumn = PaddingAndBorderForAxis(node, YogaFlexDirection.Column, ownerWidth);
            var marginAxisRow = node.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);
            var marginAxisColumn = node.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);

            node.SetLayoutMeasuredDimension(
                BoundAxis(
                    node,
                    YogaFlexDirection.Row,
                    (widthMeasureMode == YogaMeasureMode.Undefined ||
                     widthMeasureMode == YogaMeasureMode.AtMost)
                  ? paddingAndBorderAxisRow
                  : availableWidth - marginAxisRow,
              ownerWidth,
              ownerWidth),
          YogaDimension.Width);

            node.SetLayoutMeasuredDimension(
                BoundAxis(
                    node,
                    YogaFlexDirection.Column,
                    (heightMeasureMode == YogaMeasureMode.Undefined ||
                     heightMeasureMode == YogaMeasureMode.AtMost)
                        ? paddingAndBorderAxisColumn
                        : availableHeight - marginAxisColumn,
                    ownerHeight,
                    ownerWidth),
                YogaDimension.Height);
        }

        private static bool FixedSizeSetMeasuredDimensions(YogaNode node,
                                                 float? availableWidth,
                                                 float? availableHeight,
                                                 YogaMeasureMode widthMeasureMode,
                                                 YogaMeasureMode heightMeasureMode,
                                                 float? ownerWidth,
                                                 float? ownerHeight)
        {
            if ((availableWidth != null && widthMeasureMode == YogaMeasureMode.AtMost && availableWidth <= 0.0f)
                || (availableHeight != null && heightMeasureMode == YogaMeasureMode.AtMost && availableHeight <= 0.0f)
                || (widthMeasureMode == YogaMeasureMode.Exactly && heightMeasureMode == YogaMeasureMode.Exactly))
            {
                var marginAxisColumn = node.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);
                var marginAxisRow = node.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Row,
                        availableWidth == null || (widthMeasureMode == YogaMeasureMode.AtMost && availableWidth < 0.0f)
                            ? 0.0f
                            : availableWidth - marginAxisRow,
                    ownerWidth,
                    ownerWidth),
                YogaDimension.Width);

                node.SetLayoutMeasuredDimension(
                    BoundAxis(
                        node,
                        YogaFlexDirection.Column,
                        availableHeight == null || (heightMeasureMode == YogaMeasureMode.AtMost && availableHeight < 0.0f)
                            ? 0.0f
                            : availableHeight - marginAxisColumn,
                        ownerHeight,
                        ownerWidth),
                    YogaDimension.Height);
                return true;
            }

            return false;
        }


        private static void ZeroOutLayoutRecursively(YogaNode node)
        {
            node.Layout.Clear();
            node.HasNewLayout = true;
            node.CloneChildrenIfNeeded();
            var childCount = GetChildCount(node);
            for (var i = 0; i < childCount; i++)
            {
                var child = node.GetChild(i);
                ZeroOutLayoutRecursively(child);
            }
        }

        private static float? CalculateAvailableInnerDim(YogaNode node, YogaFlexDirection axis, float? availableDim, float? ownerDim)
        {
            var direction = FlexDirectionIsRow(axis) ? YogaFlexDirection.Row : YogaFlexDirection.Column;
            var dimension = FlexDirectionIsRow(axis) ? YogaDimension.Width : YogaDimension.Height;

            var margin = node.GetMarginForAxis(direction, ownerDim);
            var paddingAndBorder = PaddingAndBorderForAxis(node, direction, ownerDim);

            var availableInnerDim = availableDim - margin - paddingAndBorder;
            // Max dimension overrides predefined dimension value; Min dimension in turn
            // overrides both of the above
            if (availableInnerDim != null)
            {
                // We want to make sure our available height does not violate min and max
                // constraints
                var minInnerResolved = ResolveValue(node.Style.MinDimensions[dimension], ownerDim);
                var minInnerDim = minInnerResolved == null ? 0.0F : minInnerResolved.Value - paddingAndBorder;

                var maxInnerResolved = ResolveValue(node.Style.MaxDimensions[dimension], ownerDim);
                var maxInnerDim = maxInnerResolved == null ? float.MaxValue : maxInnerResolved.Value - paddingAndBorder;

                availableInnerDim = YogaMath.Max(YogaMath.Min(availableInnerDim, maxInnerDim), minInnerDim);
            }

            return availableInnerDim;
        }

        private static void ComputeFlexBasisForChildren(YogaNode node, float? availableInnerWidth, float? availableInnerHeight, YogaMeasureMode widthMeasureMode, YogaMeasureMode heightMeasureMode, YogaDirection direction, YogaFlexDirection mainAxis, YogaConfig config, bool performLayout, ref float? totalOuterFlexBasis)
        {
            var singleFlexChild = default(YogaNode);
            var children = node.Children;
            var measureModeMainDim = FlexDirectionIsRow(mainAxis) ? widthMeasureMode : heightMeasureMode;

            // If there is only one child with flexGrow + flexShrink it means we can set
            // the computedFlexBasis to 0 instead of measuring and shrinking / flexing the
            // child to exactly match the remaining space
            if (measureModeMainDim == YogaMeasureMode.Exactly)
            {
                foreach (var child in children)
                {
                    if (singleFlexChild != null)
                    {
                        if (child.IsNodeFlexible())
                        {
                            // There is already a flexible child, abort
                            singleFlexChild = null;
                            break;
                        }
                    }
                    else if (child.ResolveFlexGrow() > 0.0f
                        && child.ResolveFlexShrink() > 0.0f)
                    {
                        singleFlexChild = child;
                    }
                }
            }

            foreach (var child in children)
            {
                child.ResolveDimension();
                if (child.Style.Display == YogaDisplay.None)
                {
                    ZeroOutLayoutRecursively(child);
                    child.HasNewLayout = true;
                    child.IsDirty = false;
                    continue;
                }

                if (performLayout)
                {
                    // Set the initial position (relative to the owner).
                    var childDirection = child.ResolveDirection(direction);
                    var mainDim = FlexDirectionIsRow(mainAxis)
                        ? availableInnerWidth
                        : availableInnerHeight;
                    var crossDim = FlexDirectionIsRow(mainAxis)
                        ? availableInnerHeight
                        : availableInnerWidth;

                    child.SetPosition(childDirection, mainDim, crossDim, availableInnerWidth);
                }

                if (child.Style.PositionType == YogaPositionType.Absolute)
                    continue;

                if (child == singleFlexChild)
                {
                    child.SetLayoutComputedFlexBasisGeneration(gCurrentGenerationCount);
                    child.SetLayoutComputedFlexBasis(0);
                }
                else
                {
                    ComputeFlexBasisForChild(
                        node,
                        child,
                        availableInnerWidth,
                        widthMeasureMode,
                        availableInnerHeight,
                        availableInnerWidth,
                        availableInnerHeight,
                        heightMeasureMode,
                        direction,
                        config);
                }

                totalOuterFlexBasis += (child.Layout.ComputedFlexBasis + child.GetMarginForAxis(mainAxis, availableInnerWidth));
            }
        }

        private static float? BoundAxisWithinMinAndMax(YogaNode node, YogaFlexDirection axis, float? value, float? axisSize)
        {
            var min = default(float?);
            var max = default(float?);

            if (FlexDirectionIsColumn(axis))
            {
                min = ResolveValue(node.Style.MinDimensions[YogaDimension.Height], axisSize);
                max = ResolveValue(node.Style.MaxDimensions[YogaDimension.Height], axisSize);
            }
            else if (FlexDirectionIsRow(axis))
            {
                min = ResolveValue(node.Style.MinDimensions[YogaDimension.Width], axisSize);
                max = ResolveValue(node.Style.MaxDimensions[YogaDimension.Width], axisSize);
            }

            var boundValue = value;
            if (max != null && max >= 0.0f && boundValue > max)
                boundValue = max;

            if (min != null && min >= 0.0f && boundValue < min)
                boundValue = min;

            return boundValue;
        }

        // Like YGNodeBoundAxisWithinMinAndMax but also ensures that the value doesn't go
        // below the
        // padding and border amount.
        private static float? BoundAxis(YogaNode node, YogaFlexDirection axis, float? value, float? axisSize, float? widthSize)
        {
            return YogaMath.Max(BoundAxisWithinMinAndMax(node, axis, value, axisSize), PaddingAndBorderForAxis(node, axis, widthSize));
        }

        private static void SetChildTrailingPosition(YogaNode node, YogaNode child, YogaFlexDirection axis)
        {
            var size = child.Layout.MeasuredDimensions[Dimension[axis]];
            child.SetLayoutPosition(node.Layout.MeasuredDimensions[Dimension[axis]] - size - child.Layout.Position[Position[axis]], Trailing[axis]);
        }

        private static void ConstrainMaxSizeForMode(YogaNode node, YogaFlexDirection axis, float? ownerAxisSize, float? ownerWidth, ref YogaMeasureMode mode, ref float? size)
        {
            var maxSize = ResolveValue(node.Style.MaxDimensions[Dimension[axis]], ownerAxisSize) + node.GetMarginForAxis(axis, ownerWidth);
            switch (mode)
            {
                case YogaMeasureMode.Exactly:
                case YogaMeasureMode.AtMost:
                    size = (maxSize == null || size < maxSize) ? size : maxSize.Value;
                    break;
                case YogaMeasureMode.Undefined:
                    if (maxSize != null)
                    {
                        mode = YogaMeasureMode.AtMost;
                        size = maxSize.Value;
                    }
                    break;
            }
        }

        private static void ComputeFlexBasisForChild(YogaNode node,
                                           YogaNode child,
                                           float? width,
                                           YogaMeasureMode widthMode,
                                           float? height,
                                           float? ownerWidth,
                                           float? ownerHeight,
                                           YogaMeasureMode heightMode,
                                           YogaDirection direction,
                                           YogaConfig config)
        {
            var mainAxis = ResolveFlexDirection(node.Style.FlexDirection, direction);
            var isMainAxisRow = FlexDirectionIsRow(mainAxis);
            var mainAxisSize = isMainAxisRow ? width : height;
            var mainAxisOwnerSize = isMainAxisRow ? ownerWidth : ownerHeight;

            var childWidth = default(float?);
            var childHeight = default(float?);
            var childWidthMeasureMode = default(YogaMeasureMode);
            var childHeightMeasureMode = default(YogaMeasureMode);

            var resolvedFlexBasis = ResolveValue(child.ResolveFlexBasis(), mainAxisOwnerSize);
            var isRowStyleDimDefined = IsStyleDimDefined(child, YogaFlexDirection.Row, ownerWidth);
            var isColumnStyleDimDefined = IsStyleDimDefined(child, YogaFlexDirection.Column, ownerHeight);

            if (resolvedFlexBasis != null && mainAxisSize != null)
            {
                if (child.Layout.ComputedFlexBasis == null ||
                    (ConfigIsExperimentalFeatureEnabled(child.Config, YogaExperimentalFeature.WebFlexBasis)
                    && child.Layout.ComputedFlexBasisGeneration != gCurrentGenerationCount))
                {
                    var paddingAndBorder = PaddingAndBorderForAxis(child, mainAxis, ownerWidth);
                    child.SetLayoutComputedFlexBasis(YogaMath.Max(resolvedFlexBasis, paddingAndBorder));
                }
            }
            else if (isMainAxisRow && isRowStyleDimDefined)
            {
                // The width is definite, so use that as the flex basis.
                child.SetLayoutComputedFlexBasis(YogaMath.Max(
                    ResolveValue(child.GetResolvedDimension(YogaDimension.Width), ownerWidth),
                    PaddingAndBorderForAxis(child, YogaFlexDirection.Row, ownerWidth)));
            }
            else if (!isMainAxisRow && isColumnStyleDimDefined)
            {
                // The height is definite, so use that as the flex basis.
                child.SetLayoutComputedFlexBasis(YogaMath.Max(
                    ResolveValue(child.GetResolvedDimension(YogaDimension.Height), ownerHeight),
                    PaddingAndBorderForAxis(child, YogaFlexDirection.Column, ownerWidth)));
            }
            else
            {
                // Compute the flex basis and hypothetical main size (i.e. the clamped
                // flex basis).
                childWidth = default(float?);
                childHeight = default(float?);
                childWidthMeasureMode = YogaMeasureMode.Undefined;
                childHeightMeasureMode = YogaMeasureMode.Undefined;

                var marginRow = child.GetMarginForAxis(YogaFlexDirection.Row, ownerWidth);
                var marginColumn = child.GetMarginForAxis(YogaFlexDirection.Column, ownerWidth);

                if (isRowStyleDimDefined)
                {
                    childWidth = (ResolveValue(child.GetResolvedDimension(YogaDimension.Width), ownerWidth) + marginRow);
                    childWidthMeasureMode = YogaMeasureMode.Exactly;
                }
                if (isColumnStyleDimDefined)
                {
                    childHeight = (ResolveValue(child.GetResolvedDimension(YogaDimension.Height), ownerHeight) + marginColumn);
                    childHeightMeasureMode = YogaMeasureMode.Exactly;
                }

                // The W3C spec doesn't say anything about the 'overflow' property,
                // but all major browsers appear to implement the following logic.
                if ((!isMainAxisRow && node.Style.Overflow == YogaOverflow.Scroll)
                    || node.Style.Overflow != YogaOverflow.Scroll)
                {
                    if (childWidth == null && width != null)
                    {
                        childWidth = width;
                        childWidthMeasureMode = YogaMeasureMode.AtMost;
                    }
                }

                if ((isMainAxisRow && node.Style.Overflow == YogaOverflow.Scroll)
                    || node.Style.Overflow != YogaOverflow.Scroll)
                {
                    if (childHeight == null && height != null)
                    {
                        childHeight = height;
                        childHeightMeasureMode = YogaMeasureMode.AtMost;
                    }
                }

                if (child.Style.AspectRatio != null)
                {
                    if (!isMainAxisRow && childWidthMeasureMode == YogaMeasureMode.Exactly)
                    {
                        childHeight = marginColumn + (childWidth - marginRow) / child.Style.AspectRatio;
                        childHeightMeasureMode = YogaMeasureMode.Exactly;
                    }
                    else if (isMainAxisRow && childHeightMeasureMode == YogaMeasureMode.Exactly)
                    {
                        childWidth = marginRow + (childHeight - marginColumn) * child.Style.AspectRatio;
                        childWidthMeasureMode = YogaMeasureMode.Exactly;
                    }
                }

                // If child has no defined size in the cross axis and is set to stretch,
                // set the cross
                // axis to be measured exactly with the available inner width

                var hasExactWidth = width != null && widthMode == YogaMeasureMode.Exactly;
                var childWidthStretch = AlignItem(node, child) == YogaAlign.Stretch && childWidthMeasureMode != YogaMeasureMode.Exactly;
                if (!isMainAxisRow && !isRowStyleDimDefined && hasExactWidth && childWidthStretch)
                {
                    childWidth = width;
                    childWidthMeasureMode = YogaMeasureMode.Exactly;
                    if (child.Style.AspectRatio != null)
                    {
                        childHeight = (childWidth - marginRow) / child.Style.AspectRatio;
                        childHeightMeasureMode = YogaMeasureMode.Exactly;
                    }
                }

                var hasExactHeight = height != null && heightMode == YogaMeasureMode.Exactly;
                var childHeightStretch = AlignItem(node, child) == YogaAlign.Stretch && childHeightMeasureMode != YogaMeasureMode.Exactly;
                if (isMainAxisRow && !isColumnStyleDimDefined && hasExactHeight && childHeightStretch)
                {
                    childHeight = height;
                    childHeightMeasureMode = YogaMeasureMode.Exactly;

                    if (child.Style.AspectRatio != null)
                    {
                        childWidth = (childHeight - marginColumn) * child.Style.AspectRatio;
                        childWidthMeasureMode = YogaMeasureMode.Exactly;
                    }
                }

                ConstrainMaxSizeForMode(child, YogaFlexDirection.Row, ownerWidth, ownerWidth, ref childWidthMeasureMode, ref childWidth);
                ConstrainMaxSizeForMode(child, YogaFlexDirection.Column, ownerHeight, ownerWidth, ref childHeightMeasureMode, ref childHeight);

                // Measure the child
                LayoutNode(child,
                                     childWidth,
                                     childHeight,
                                     direction,
                                     childWidthMeasureMode,
                                     childHeightMeasureMode,
                                     ownerWidth,
                                     ownerHeight,
                                     false,
                                     "measure",
                                     config);

                child.SetLayoutComputedFlexBasis(YogaMath.Max(child.Layout.MeasuredDimensions[Dimension[mainAxis]], PaddingAndBorderForAxis(child, mainAxis, ownerWidth)));
            }

            child.SetLayoutComputedFlexBasisGeneration(gCurrentGenerationCount);
        }

        private static float? CalculateBaseline(YogaNode node)
        {
            var baseline = default(float?);
            if (node.Baseline != null)
            {
                baseline = node.Baseline(
                   node,
                   node.Layout.MeasuredDimensions[YogaDimension.Width],
                   node.Layout.MeasuredDimensions[YogaDimension.Height]);
                return baseline;
            }

            var baselineChild = default(YogaNode);
            var childCount = GetChildCount(node);
            for (var i = 0; i < childCount; i++)
            {
                var child = GetChild(node, i);
                if (child.LineIndex > 0)
                    break;

                if (child.Style.PositionType == YogaPositionType.Absolute)
                    continue;

                if (AlignItem(node, child) == YogaAlign.Baseline)
                {
                    baselineChild = child;
                    break;
                }

                if (baselineChild == null)
                    baselineChild = child;
            }

            if (baselineChild == null)
                return node.Layout.MeasuredDimensions[YogaDimension.Height];

            baseline = CalculateBaseline(baselineChild);
            return baseline + baselineChild.Layout.Position[YogaEdge.Top];
        }

        // This function assumes that all the children of node have their
        // computedFlexBasis properly computed(To do this use
        // YGNodeComputeFlexBasisForChildren function).
        // This function calculates YGCollectFlexItemsRowMeasurement
        private static YogaCollectFlexItemsRowValues CalculateCollectFlexItemsRowValues(ref YogaNode node, YogaDirection ownerDirection, float? mainAxisOwnerSize, float? availableInnerWidth, float? availableInnerMainDim, int startOfLineIndex, int lineCount)
        {
            var flexAlgoRowMeasurement = new YogaCollectFlexItemsRowValues();
            flexAlgoRowMeasurement.RelativeChildren.Capacity = node.Children.Count;

            var sizeConsumedOnCurrentLineIncludingMinConstraint = (float?)0F;
            var mainAxis = ResolveFlexDirection(node.Style.FlexDirection, node.ResolveDirection(ownerDirection));
            var isNodeFlexWrap = node.Style.FlexWrap != YogaWrap.NoWrap;

            // Add items to the current line until it's full or we run out of items.
            var endOfLineIndex = startOfLineIndex;
            for (; endOfLineIndex < node.ChildrenCount; endOfLineIndex++)
            {
                var child = node.GetChild(endOfLineIndex);
                if (child.Style.Display == YogaDisplay.None || child.Style.PositionType == YogaPositionType.Absolute)
                    continue;

                child.LineIndex = lineCount;
                var childMarginMainAxis = child.GetMarginForAxis(mainAxis, availableInnerWidth);
                var flexBasisWithMinAndMaxConstraints = BoundAxisWithinMinAndMax(
                       child,
                       mainAxis,
                       child.Layout.ComputedFlexBasis,
                       mainAxisOwnerSize);

                // If this is a multi-line flow and this item pushes us over the
                // available size, we've
                // hit the end of the current line. Break out of the loop and lay out
                // the current line.
                if (sizeConsumedOnCurrentLineIncludingMinConstraint + flexBasisWithMinAndMaxConstraints + childMarginMainAxis > availableInnerMainDim
                    && isNodeFlexWrap && flexAlgoRowMeasurement.ItemsOnLine > 0)
                {
                    break;
                }

                sizeConsumedOnCurrentLineIncludingMinConstraint += flexBasisWithMinAndMaxConstraints + childMarginMainAxis;
                flexAlgoRowMeasurement.SizeConsumedOnCurrentLine += flexBasisWithMinAndMaxConstraints + childMarginMainAxis;
                flexAlgoRowMeasurement.ItemsOnLine++;

                if (child.IsNodeFlexible())
                {
                    flexAlgoRowMeasurement.TotalFlexGrowFactors += child.ResolveFlexGrow();

                    // Unlike the grow factor, the shrink factor is scaled relative to the
                    // child dimension.
                    flexAlgoRowMeasurement.TotalFlexShrinkScaledFactors += -child.ResolveFlexShrink() * (child.Layout.ComputedFlexBasis);
                }

                flexAlgoRowMeasurement.RelativeChildren.Add(child);
            }

            // The total flex factor needs to be floored to 1.
            if (flexAlgoRowMeasurement.TotalFlexGrowFactors > 0 && flexAlgoRowMeasurement.TotalFlexGrowFactors < 1)
                flexAlgoRowMeasurement.TotalFlexGrowFactors = 1;

            // The total flex shrink factor needs to be floored to 1.
            if (flexAlgoRowMeasurement.TotalFlexShrinkScaledFactors > 0 && flexAlgoRowMeasurement.TotalFlexShrinkScaledFactors < 1)
                flexAlgoRowMeasurement.TotalFlexShrinkScaledFactors = 1;

            flexAlgoRowMeasurement.EndOfLineIndex = endOfLineIndex;
            return flexAlgoRowMeasurement;
        }

        // Do two passes over the flex items to figure out how to distribute the
        // remaining space.
        // The first pass finds the items whose min/max constraints trigger,
        // freezes them at those
        // sizes, and excludes those sizes from the remaining space. The second
        // pass sets the size
        // of each flexible item. It distributes the remaining space amongst the
        // items whose min/max
        // constraints didn't trigger in pass 1. For the other items, it sets
        // their sizes by forcing
        // their min/max constraints to trigger again.
        //
        // This two pass approach for resolving min/max constraints deviates from
        // the spec. The
        // spec (https://www.w3.org/TR/YG-flexbox-1/#resolve-flexible-lengths)
        // describes a process
        // that needs to be repeated a variable number of times. The algorithm
        // implemented here
        // won't handle all cases but it was simpler to implement and it mitigates
        // performance
        // concerns because we know exactly how many passes it'll do.
        //
        // At the end of this function the child nodes would have the proper size
        // assigned to them.
        //
        private static void ResolveFlexibleLength(
                YogaNode node,
                ref YogaCollectFlexItemsRowValues collectedFlexItemsValues,
                YogaFlexDirection mainAxis,
                YogaFlexDirection crossAxis,
                float? mainAxisOwnerSize,
                float? availableInnerMainDim,
                float? availableInnerCrossDim,
                float? availableInnerWidth,
                float? availableInnerHeight,
                bool flexBasisOverflows,
                YogaMeasureMode measureModeCrossDim,
                bool performLayout,
                YogaConfig config)
        {
            var originalFreeSpace = collectedFlexItemsValues.RemainingFreeSpace;
            // First pass: detect the flex items whose min/max constraints trigger
            DistributeFreeSpaceFirstPass(
                ref collectedFlexItemsValues,
                mainAxis,
                mainAxisOwnerSize,
                availableInnerMainDim,
                availableInnerWidth);

            // Second pass: resolve the sizes of the flexible items
            var distributedFreeSpace = DistributeFreeSpaceSecondPass(
                ref collectedFlexItemsValues,
                node,
                mainAxis,
                crossAxis,
                mainAxisOwnerSize,
                availableInnerMainDim,
                availableInnerCrossDim,
                availableInnerWidth,
                availableInnerHeight,
                flexBasisOverflows,
                measureModeCrossDim,
                performLayout,
                config);

            collectedFlexItemsValues.RemainingFreeSpace = originalFreeSpace - distributedFreeSpace;
        }


        // It distributes the free space to the flexible items.For those flexible items
        // whose min and max constraints are triggered, those flex item's clamped size
        // is removed from the remaingfreespace.
        private static void DistributeFreeSpaceFirstPass(
            ref YogaCollectFlexItemsRowValues collectedFlexItemsValues,
            YogaFlexDirection mainAxis,
            float? mainAxisOwnerSize,
            float? availableInnerMainDim,
            float? availableInnerWidth)
        {
            var flexShrinkScaledFactor = (float?)0F;
            var flexGrowFactor = 0F;
            var baseMainSize = (float?)0F;
            var boundMainSize = (float?)0F;
            var deltaFreeSpace = (float?)0F;

            foreach (var currentRelativeChild in collectedFlexItemsValues.RelativeChildren)
            {
                var childFlexBasis = BoundAxisWithinMinAndMax(
                    currentRelativeChild,
                    mainAxis,
                    currentRelativeChild.Layout.ComputedFlexBasis,
                    mainAxisOwnerSize);

                if (collectedFlexItemsValues.RemainingFreeSpace < 0)
                {
                    flexShrinkScaledFactor = -currentRelativeChild.ResolveFlexShrink() * childFlexBasis;

                    // Is this child able to shrink?
                    if (flexShrinkScaledFactor != null && flexShrinkScaledFactor != 0)
                    {
                        baseMainSize = childFlexBasis + collectedFlexItemsValues.RemainingFreeSpace / collectedFlexItemsValues.TotalFlexShrinkScaledFactors * flexShrinkScaledFactor;
                        boundMainSize = BoundAxis(
                            currentRelativeChild,
                            mainAxis,
                            baseMainSize,
                            availableInnerMainDim,
                            availableInnerWidth);

                        if (baseMainSize != null && boundMainSize != null && baseMainSize != boundMainSize)
                        {
                            // By excluding this item's size and flex factor from remaining,
                            // this item's
                            // min/max constraints should also trigger in the second pass
                            // resulting in the
                            // item's size calculation being identical in the first and second
                            // passes.
                            deltaFreeSpace += boundMainSize - childFlexBasis;
                            collectedFlexItemsValues.TotalFlexShrinkScaledFactors -= flexShrinkScaledFactor;
                        }
                    }
                }
                else if (collectedFlexItemsValues.RemainingFreeSpace != null && collectedFlexItemsValues.RemainingFreeSpace > 0)
                {
                    flexGrowFactor = currentRelativeChild.ResolveFlexGrow();

                    // Is this child able to grow?
                    if (flexGrowFactor != 0)
                    {
                        baseMainSize = childFlexBasis + collectedFlexItemsValues.RemainingFreeSpace / collectedFlexItemsValues.TotalFlexGrowFactors * flexGrowFactor;
                        boundMainSize = BoundAxis(
                            currentRelativeChild,
                            mainAxis,
                            baseMainSize,
                            availableInnerMainDim,
                            availableInnerWidth);

                        if (baseMainSize != null && boundMainSize != null && baseMainSize != boundMainSize)
                        {
                            // By excluding this item's size and flex factor from remaining,
                            // this item's
                            // min/max constraints should also trigger in the second pass
                            // resulting in the
                            // item's size calculation being identical in the first and second
                            // passes.
                            deltaFreeSpace += boundMainSize - childFlexBasis;
                            collectedFlexItemsValues.TotalFlexGrowFactors -= flexGrowFactor;
                        }
                    }
                }
            }

            collectedFlexItemsValues.RemainingFreeSpace -= deltaFreeSpace;
        }


        // It distributes the free space to the flexible items and ensures that the size
        // of the flex items abide the min and max constraints. At the end of this
        // function the child nodes would have proper size. Prior using this function
        // please ensure that YGDistributeFreeSpaceFirstPass is called.
        private static float? DistributeFreeSpaceSecondPass(
            ref YogaCollectFlexItemsRowValues collectedFlexItemsValues,
            YogaNode node,
            YogaFlexDirection mainAxis,
            YogaFlexDirection crossAxis,
            float? mainAxisOwnerSize,
            float? availableInnerMainDim,
            float? availableInnerCrossDim,
            float? availableInnerWidth,
            float? availableInnerHeight,
            bool flexBasisOverflows,
            YogaMeasureMode measureModeCrossDim,
            bool performLayout,
            YogaConfig config)
        {
            var childFlexBasis = (float?)0F;
            var flexShrinkScaledFactor = (float?)0F;
            var flexGrowFactor = 0F;
            var deltaFreeSpace = (float?)0F;
            var isMainAxisRow = FlexDirectionIsRow(mainAxis);
            var isNodeFlexWrap = node.Style.FlexWrap != YogaWrap.NoWrap;

            foreach (var currentRelativeChild in collectedFlexItemsValues.RelativeChildren)
            {
                childFlexBasis = BoundAxisWithinMinAndMax(
                    currentRelativeChild,
                    mainAxis,
                    currentRelativeChild.Layout.ComputedFlexBasis,
                    mainAxisOwnerSize);
                var updatedMainSize = childFlexBasis;

                if (collectedFlexItemsValues.RemainingFreeSpace != null && collectedFlexItemsValues.RemainingFreeSpace < 0)
                {
                    flexShrinkScaledFactor = -currentRelativeChild.ResolveFlexShrink() * childFlexBasis;
                    // Is this child able to shrink?
                    if (flexShrinkScaledFactor != 0)
                    {
                        var childSize = (float?)0F;
                        if (collectedFlexItemsValues.TotalFlexShrinkScaledFactors != null && collectedFlexItemsValues.TotalFlexShrinkScaledFactors == 0)
                        {
                            childSize = childFlexBasis + flexShrinkScaledFactor;
                        }
                        else
                        {
                            childSize = childFlexBasis + (collectedFlexItemsValues.RemainingFreeSpace / collectedFlexItemsValues.TotalFlexShrinkScaledFactors) * flexShrinkScaledFactor;
                        }

                        updatedMainSize = BoundAxis(
                            currentRelativeChild,
                            mainAxis,
                            childSize,
                            availableInnerMainDim,
                            availableInnerWidth);
                    }
                }
                else if (collectedFlexItemsValues.RemainingFreeSpace != null && collectedFlexItemsValues.RemainingFreeSpace > 0)
                {
                    flexGrowFactor = currentRelativeChild.ResolveFlexGrow();

                    // Is this child able to grow?
                    if (flexGrowFactor != 0)
                    {
                        updatedMainSize = BoundAxis(
                            currentRelativeChild,
                            mainAxis,
                            childFlexBasis + collectedFlexItemsValues.RemainingFreeSpace / collectedFlexItemsValues.TotalFlexGrowFactors * flexGrowFactor,
                            availableInnerMainDim,
                            availableInnerWidth);
                    }
                }

                deltaFreeSpace += updatedMainSize - childFlexBasis;

                var marginMain = currentRelativeChild.GetMarginForAxis(mainAxis, availableInnerWidth);
                var marginCross = currentRelativeChild.GetMarginForAxis(crossAxis, availableInnerWidth);

                var childCrossSize = default(float?);
                var childMainSize = updatedMainSize + marginMain;
                var childCrossMeasureMode = default(YogaMeasureMode);
                var childMainMeasureMode = YogaMeasureMode.Exactly;

                if (currentRelativeChild.Style.AspectRatio != null)
                {
                    childCrossSize = isMainAxisRow
                        ? (childMainSize - marginMain) / currentRelativeChild.Style.AspectRatio
                        : (childMainSize - marginMain) * currentRelativeChild.Style.AspectRatio;

                    childCrossMeasureMode = YogaMeasureMode.Exactly;
                    childCrossSize += marginCross;
                }
                else if (
                  availableInnerCrossDim != null
                  && !IsStyleDimDefined(currentRelativeChild, crossAxis, availableInnerCrossDim)
                  && measureModeCrossDim == YogaMeasureMode.Exactly
                  && !(isNodeFlexWrap && flexBasisOverflows)
                  && AlignItem(node, currentRelativeChild) == YogaAlign.Stretch
                  && currentRelativeChild.GetMarginLeadingValue(crossAxis).Unit != YogaUnit.Auto
                  && currentRelativeChild.GetMarginTrailingValue(crossAxis).Unit != YogaUnit.Auto)
                {
                    childCrossSize = availableInnerCrossDim;
                    childCrossMeasureMode = YogaMeasureMode.Exactly;
                }
                else if (!IsStyleDimDefined(currentRelativeChild, crossAxis, availableInnerCrossDim))
                {
                    childCrossSize = availableInnerCrossDim;
                    childCrossMeasureMode = childCrossSize == null
                        ? YogaMeasureMode.Undefined
                        : YogaMeasureMode.AtMost;
                }
                else
                {
                    childCrossSize = (ResolveValue(currentRelativeChild.GetResolvedDimension(Dimension[crossAxis]), availableInnerCrossDim)) + marginCross;
                    var isLoosePercentageMeasurement = currentRelativeChild.GetResolvedDimension(Dimension[crossAxis]).Unit == YogaUnit.Percent
                        && measureModeCrossDim != YogaMeasureMode.Exactly;

                    childCrossMeasureMode = childCrossSize == null || isLoosePercentageMeasurement
                              ? YogaMeasureMode.Undefined
                              : YogaMeasureMode.Exactly;
                }

                ConstrainMaxSizeForMode(
                    currentRelativeChild,
                    mainAxis,
                    availableInnerMainDim,
                    availableInnerWidth,
                    ref childMainMeasureMode,
                    ref childMainSize);
                ConstrainMaxSizeForMode(
                    currentRelativeChild,
                    crossAxis,
                    availableInnerCrossDim,
                    availableInnerWidth,
                    ref childCrossMeasureMode,
                    ref childCrossSize);

                var requiresStretchLayout = !IsStyleDimDefined(currentRelativeChild, crossAxis, availableInnerCrossDim)
                    && AlignItem(node, currentRelativeChild) == YogaAlign.Stretch
                    && currentRelativeChild.GetMarginLeadingValue(crossAxis).Unit != YogaUnit.Auto
                    && currentRelativeChild.GetMarginTrailingValue(crossAxis).Unit != YogaUnit.Auto;

                var childWidth = isMainAxisRow ? childMainSize : childCrossSize;
                var childHeight = !isMainAxisRow ? childMainSize : childCrossSize;

                var childWidthMeasureMode = isMainAxisRow ? childMainMeasureMode : childCrossMeasureMode;
                var childHeightMeasureMode = !isMainAxisRow ? childMainMeasureMode : childCrossMeasureMode;

                // Recursively call the layout algorithm for this child with the updated
                // main size.
                LayoutNode(
                    currentRelativeChild,
                    childWidth,
                    childHeight,
                    node.Layout.Direction,
                    childWidthMeasureMode,
                    childHeightMeasureMode,
                    availableInnerWidth,
                    availableInnerHeight,
                    performLayout && !requiresStretchLayout,
                    "flex",
                    config);

                node.SetLayoutHadOverflow(node.Layout.HadOverflow || currentRelativeChild.Layout.HadOverflow);
            }

            return deltaFreeSpace;
        }

        private static void JustifyMainAxis(
            YogaNode node,
            ref YogaCollectFlexItemsRowValues collectedFlexItemsValues,
            ref int startOfLineIndex,
            ref YogaFlexDirection mainAxis,
            ref YogaFlexDirection crossAxis,
            ref YogaMeasureMode measureModeMainDim,
            ref YogaMeasureMode measureModeCrossDim,
            ref float? mainAxisOwnerSize,
            ref float? ownerWidth,
            ref float? availableInnerMainDim,
            ref float? availableInnerCrossDim,
            ref float? availableInnerWidth,
            ref bool performLayout)
        {
            var style = node.Style;

            // If we are using "at most" rules in the main axis. Calculate the remaining
            // space when constraint by the min size defined for the main axis.
            if (measureModeMainDim == YogaMeasureMode.AtMost
                && collectedFlexItemsValues.RemainingFreeSpace > 0)
            {
                if (style.MinDimensions[Dimension[mainAxis]].Unit != YogaUnit.Undefined
                    && ResolveValue(style.MinDimensions[Dimension[mainAxis]], mainAxisOwnerSize) != null)
                {
                    collectedFlexItemsValues.RemainingFreeSpace = YogaMath.Max(
                        0,
                        (ResolveValue(style.MinDimensions[Dimension[mainAxis]], mainAxisOwnerSize)) - (availableInnerMainDim - collectedFlexItemsValues.RemainingFreeSpace));
                }
                else
                {
                    collectedFlexItemsValues.RemainingFreeSpace = 0;
                }
            }

            var numberOfAutoMarginsOnCurrentLine = 0;
            for (var i = startOfLineIndex; i < collectedFlexItemsValues.EndOfLineIndex; i++)
            {
                var child = node.GetChild(i);
                if (child.Style.PositionType == YogaPositionType.Relative)
                {
                    if (child.GetMarginLeadingValue(mainAxis).Unit == YogaUnit.Auto)
                        numberOfAutoMarginsOnCurrentLine++;

                    if (child.GetMarginTrailingValue(mainAxis).Unit == YogaUnit.Auto)
                        numberOfAutoMarginsOnCurrentLine++;
                }
            }

            // In order to position the elements in the main axis, we have two
            // controls. The space between the beginning and the first element
            // and the space between each two elements.
            var leadingMainDim = (float?)0F;
            var betweenMainDim = (float?)0F;
            var justifyContent = node.Style.JustifyContent;

            if (numberOfAutoMarginsOnCurrentLine == 0)
            {
                switch (justifyContent)
                {
                    case YogaJustify.Center:
                        leadingMainDim = collectedFlexItemsValues.RemainingFreeSpace / 2;
                        break;
                    case YogaJustify.FlexEnd:
                        leadingMainDim = collectedFlexItemsValues.RemainingFreeSpace;
                        break;
                    case YogaJustify.SpaceBetween:
                        if (collectedFlexItemsValues.ItemsOnLine > 1)
                        {
                            betweenMainDim = YogaMath.Max(collectedFlexItemsValues.RemainingFreeSpace, 0) / (collectedFlexItemsValues.ItemsOnLine - 1);
                        }
                        else
                        {
                            betweenMainDim = 0;
                        }
                        break;
                    case YogaJustify.SpaceEvenly:
                        // Space is distributed evenly across all elements
                        betweenMainDim = collectedFlexItemsValues.RemainingFreeSpace / (collectedFlexItemsValues.ItemsOnLine + 1);
                        leadingMainDim = betweenMainDim;
                        break;
                    case YogaJustify.SpaceAround:
                        // Space on the edges is half of the space between elements
                        betweenMainDim = collectedFlexItemsValues.RemainingFreeSpace / collectedFlexItemsValues.ItemsOnLine;
                        leadingMainDim = betweenMainDim / 2;
                        break;
                    case YogaJustify.FlexStart:
                        break;
                }
            }

            var leadingPaddingAndBorderMain = node.GetLeadingPaddingAndBorder(mainAxis, ownerWidth);
            collectedFlexItemsValues.MainDimension = leadingPaddingAndBorderMain + leadingMainDim;
            collectedFlexItemsValues.CrossDimension = 0;

            for (var i = startOfLineIndex; i < collectedFlexItemsValues.EndOfLineIndex; i++)
            {
                var child = node.GetChild(i);
                var childStyle = child.Style;
                var childLayout = child.Layout;
                if (childStyle.Display == YogaDisplay.None)
                    continue;

                if (childStyle.PositionType == YogaPositionType.Absolute && child.IsLeadingPositionDefined(mainAxis))
                {
                    if (performLayout)
                    {
                        // In case the child is position absolute and has left/top being
                        // defined, we override the position to whatever the user said
                        // (and margin/border).
                        child.SetLayoutPosition(child.GetLeadingPosition(mainAxis, availableInnerMainDim) + node.GetLeadingBorder(mainAxis) + child.GetLeadingMargin(mainAxis, availableInnerWidth), Position[mainAxis]);
                    }
                }
                else
                {
                    // Now that we placed the element, we need to update the variables.
                    // We need to do that only for relative elements. Absolute elements
                    // do not take part in that phase.
                    if (childStyle.PositionType == YogaPositionType.Relative)
                    {
                        if (child.GetMarginLeadingValue(mainAxis).Unit == YogaUnit.Auto)
                        {
                            collectedFlexItemsValues.MainDimension += collectedFlexItemsValues.RemainingFreeSpace / numberOfAutoMarginsOnCurrentLine;
                        }

                        if (performLayout)
                        {
                            child.SetLayoutPosition(childLayout.Position[Position[mainAxis]] + collectedFlexItemsValues.MainDimension, Position[mainAxis]);
                        }

                        if (child.GetMarginTrailingValue(mainAxis).Unit == YogaUnit.Auto)
                        {
                            collectedFlexItemsValues.MainDimension += collectedFlexItemsValues.RemainingFreeSpace / numberOfAutoMarginsOnCurrentLine;
                        }

                        var canSkipFlex = !performLayout && measureModeCrossDim == YogaMeasureMode.Exactly;
                        if (canSkipFlex)
                        {
                            // If we skipped the flex step, then we can't rely on the
                            // measuredDims because
                            // they weren't computed. This means we can't call
                            // YGNodeDimWithMargin.
                            collectedFlexItemsValues.MainDimension += betweenMainDim + child.GetMarginForAxis(mainAxis, availableInnerWidth) + (childLayout.ComputedFlexBasis);
                            collectedFlexItemsValues.CrossDimension = availableInnerCrossDim;
                        }
                        else
                        {
                            // The main dimension is the sum of all the elements dimension plus
                            // the spacing.
                            collectedFlexItemsValues.MainDimension += betweenMainDim + DimWithMargin(child, mainAxis, availableInnerWidth);

                            // The cross dimension is the max of the elements dimension since
                            // there can only be one element in that cross dimension.
                            collectedFlexItemsValues.CrossDimension = YogaMath.Max(collectedFlexItemsValues.CrossDimension, DimWithMargin(child, crossAxis, availableInnerWidth));
                        }
                    }
                    else if (performLayout)
                    {
                        child.SetLayoutPosition(childLayout.Position[Position[mainAxis]] + node.GetLeadingBorder(mainAxis) + leadingMainDim, Position[mainAxis]);
                    }
                }
            }

            collectedFlexItemsValues.MainDimension += node.GetTrailingPaddingAndBorder(mainAxis, ownerWidth);
        }

        private static void AbsoluteLayoutChild(YogaNode node,
                                      YogaNode child,
                                      float? width,
                                      YogaMeasureMode widthMode,
                                      float? height,
                                      YogaDirection direction,
                                      YogaConfig config)
        {
            var mainAxis = ResolveFlexDirection(node.Style.FlexDirection, direction);
            var crossAxis = FlexDirectionCross(mainAxis, direction);
            var isMainAxisRow = FlexDirectionIsRow(mainAxis);

            var childWidth = default(float?);
            var childHeight = default(float?);
            var childWidthMeasureMode = YogaMeasureMode.Undefined;
            var childHeightMeasureMode = YogaMeasureMode.Undefined;

            var marginRow = child.GetMarginForAxis(YogaFlexDirection.Row, width);
            var marginColumn = child.GetMarginForAxis(YogaFlexDirection.Column, width);

            if (IsStyleDimDefined(child, YogaFlexDirection.Row, width))
            {
                childWidth = (ResolveValue(child.GetResolvedDimension(YogaDimension.Width), width)) + marginRow;
            }
            else
            {
                // If the child doesn't have a specified width, compute the width based
                // on the left/right
                // offsets if they're defined.
                if (child.IsLeadingPositionDefined(YogaFlexDirection.Row) && child.IsTrailingPosDefined(YogaFlexDirection.Row))
                {
                    childWidth = node.Layout.MeasuredDimensions[YogaDimension.Width] -
                        (node.GetLeadingBorder(YogaFlexDirection.Row) +
                         node.GetTrailingBorder(YogaFlexDirection.Row)) -
                        (child.GetLeadingPosition(YogaFlexDirection.Row, width) +
                         child.GetTrailingPosition(YogaFlexDirection.Row, width));
                    childWidth = BoundAxis(child, YogaFlexDirection.Row, childWidth, width, width);
                }
            }

            if (IsStyleDimDefined(child, YogaFlexDirection.Column, height))
            {
                childHeight = (ResolveValue(child.GetResolvedDimension(YogaDimension.Height), height)) + marginColumn;
            }
            else
            {
                // If the child doesn't have a specified height, compute the height
                // based on the top/bottom
                // offsets if they're defined.
                if (child.IsLeadingPositionDefined(YogaFlexDirection.Column) && child.IsTrailingPosDefined(YogaFlexDirection.Column))
                {
                    childHeight = node.Layout.MeasuredDimensions[YogaDimension.Height] -
                        (node.GetLeadingBorder(YogaFlexDirection.Column) +
                         node.GetTrailingBorder(YogaFlexDirection.Column)) -
                        (child.GetLeadingPosition(YogaFlexDirection.Column, height) +
                         child.GetTrailingPosition(YogaFlexDirection.Column, height));
                    childHeight = BoundAxis(child, YogaFlexDirection.Column, childHeight, height, width);
                }
            }

            // Exactly one dimension needs to be defined for us to be able to do aspect ratio
            // calculation. One dimension being the anchor and the other being flexible.
            if (childWidth == null ^ childHeight == null)
            {
                if (child.Style.AspectRatio != null)
                {
                    if (childWidth == null)
                        childWidth = marginRow + (childHeight - marginColumn) * child.Style.AspectRatio;
                    else if (childHeight == null)
                        childHeight = marginColumn + (childWidth - marginRow) / child.Style.AspectRatio;
                }
            }

            // If we're still missing one or the other dimension, measure the content.
            if (childWidth == null || childHeight == null)
            {
                childWidthMeasureMode = childWidth == null ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;
                childHeightMeasureMode = childHeight == null ? YogaMeasureMode.Undefined : YogaMeasureMode.Exactly;

                // If the size of the owner is defined then try to constrain the absolute child to that size
                // as well. This allows text within the absolute child to wrap to the size of its owner.
                // This is the same behavior as many browsers implement.
                if (!isMainAxisRow && childWidth == null && widthMode != YogaMeasureMode.Undefined && width != null && width > 0)
                {
                    childWidth = width;
                    childWidthMeasureMode = YogaMeasureMode.AtMost;
                }

                LayoutNode(child,
                    childWidth,
                    childHeight,
                    direction,
                    childWidthMeasureMode,
                    childHeightMeasureMode,
                    childWidth,
                    childHeight,
                    false,
                    "abs-measure",
                    config);

                childWidth = child.Layout.MeasuredDimensions[YogaDimension.Width] + child.GetMarginForAxis(YogaFlexDirection.Row, width);
                childHeight = child.Layout.MeasuredDimensions[YogaDimension.Height] + child.GetMarginForAxis(YogaFlexDirection.Column, width);
            }

            LayoutNode(child,
                childWidth,
                childHeight,
                direction,
                YogaMeasureMode.Exactly,
                YogaMeasureMode.Exactly,
                childWidth,
                childHeight,
                true,
                "abs-layout",
                config);

            if (child.IsTrailingPosDefined(mainAxis) && !child.IsLeadingPositionDefined(mainAxis))
            {
                child.SetLayoutPosition(
                    node.Layout.MeasuredDimensions[Dimension[mainAxis]] -
                        child.Layout.MeasuredDimensions[Dimension[mainAxis]] -
                        node.GetTrailingBorder(mainAxis) -
                        child.GetTrailingMargin(mainAxis, width) -
                        child.GetTrailingPosition(mainAxis, isMainAxisRow ? width : height),
                    Leading[mainAxis]);
            }
            else if (!child.IsLeadingPositionDefined(mainAxis) && node.Style.JustifyContent == YogaJustify.Center)
            {
                child.SetLayoutPosition((node.Layout.MeasuredDimensions[Dimension[mainAxis]] - child.Layout.MeasuredDimensions[Dimension[mainAxis]]) / 2.0f, Leading[mainAxis]);
            }
            else if (!child.IsLeadingPositionDefined(mainAxis) && node.Style.JustifyContent == YogaJustify.FlexEnd)
            {
                child.SetLayoutPosition((node.Layout.MeasuredDimensions[Dimension[mainAxis]] - child.Layout.MeasuredDimensions[Dimension[mainAxis]]), Leading[mainAxis]);
            }

            if (child.IsTrailingPosDefined(crossAxis) && !child.IsLeadingPositionDefined(crossAxis))
            {
                child.SetLayoutPosition(
                    node.Layout.MeasuredDimensions[Dimension[crossAxis]] -
                        child.Layout.MeasuredDimensions[Dimension[crossAxis]] -
                        node.GetTrailingBorder(crossAxis) -
                        child.GetTrailingMargin(crossAxis, width) -
                        child.GetTrailingPosition(crossAxis, isMainAxisRow ? height : width),
                    Leading[crossAxis]);

            }
            else if (!child.IsLeadingPositionDefined(crossAxis) && AlignItem(node, child) == YogaAlign.Center)
            {
                child.SetLayoutPosition((node.Layout.MeasuredDimensions[Dimension[crossAxis]] - child.Layout.MeasuredDimensions[Dimension[crossAxis]]) / 2.0f, Leading[crossAxis]);
            }
            else if (!child.IsLeadingPositionDefined(crossAxis) && ((AlignItem(node, child) == YogaAlign.FlexEnd) ^ (node.Style.FlexWrap == YogaWrap.WrapReverse)))
            {
                child.SetLayoutPosition((node.Layout.MeasuredDimensions[Dimension[crossAxis]] - child.Layout.MeasuredDimensions[Dimension[crossAxis]]), Leading[crossAxis]);
            }
        }

        private static YogaNode GetChild(YogaNode node, int index)
        {
            if (index < node.Children.Count)
                return node.GetChild(index);
            return null;
        }

        private static int GetChildCount(YogaNode node)
        {
            return node.Children.Count;
        }

        private static float? DimWithMargin(YogaNode node, YogaFlexDirection axis, float? widthSize)
        {
            return node.Layout.MeasuredDimensions[Dimension[axis]] + node.GetLeadingMargin(axis, widthSize) + node.GetTrailingMargin(axis, widthSize);
        }

        private static bool IsStyleDimDefined(YogaNode node, YogaFlexDirection axis, float? ownerSize)
        {
            var isUndefined = node.GetResolvedDimension(Dimension[axis]).Value == null;
            return !(node.GetResolvedDimension(Dimension[axis]).Unit == YogaUnit.Auto
                || node.GetResolvedDimension(Dimension[axis]).Unit == YogaUnit.Undefined
                || (node.GetResolvedDimension(Dimension[axis]).Unit == YogaUnit.Point && !isUndefined && node.GetResolvedDimension(Dimension[axis]).Value < 0.0f)
                || (node.GetResolvedDimension(Dimension[axis]).Unit == YogaUnit.Percent && !isUndefined && (node.GetResolvedDimension(Dimension[axis]).Value < 0.0f || (ownerSize == null))));
        }

        private static bool IsLayoutDimDefined(YogaNode node, YogaFlexDirection axis)
        {
            var value = node.Layout.MeasuredDimensions[Dimension[axis]];
            return value != null && value >= 0.0f;
        }

        private static bool IsBaselineLayout(YogaNode node)
        {
            if (FlexDirectionIsColumn(node.Style.FlexDirection))
                return false;

            if (node.Style.AlignItems == YogaAlign.Baseline)
                return true;

            var childCount = GetChildCount(node);
            for (var i = 0; i < childCount; i++)
            {
                var child = GetChild(node, i);
                if (child.Style.PositionType == YogaPositionType.Relative
                    && child.Style.AlignSelf == YogaAlign.Baseline)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ConfigIsExperimentalFeatureEnabled(YogaConfig config, YogaExperimentalFeature feature)
        {
            return config.ExperimentalFeatures[(int)feature];
        }

        private static bool FloatsEqual(float? a, float? b)
        {
            if (a == null)
                return b == null;

            if (b == null)
                return false;

            return Math.Abs(a.Value - b.Value) < 0.0001f;
        }

        private static YogaAlign AlignItem(YogaNode node, YogaNode child)
        {
            var align = child.Style.AlignSelf == YogaAlign.Auto
                ? node.Style.AlignItems
                : child.Style.AlignSelf;

            if (align == YogaAlign.Baseline
                && FlexDirectionIsColumn(node.Style.FlexDirection))
                return YogaAlign.FlexStart;

            return align;
        }

        private static float? PaddingAndBorderForAxis(YogaNode node, YogaFlexDirection axis, float? widthSize)
        {
            return node.GetLeadingPaddingAndBorder(axis, widthSize) + node.GetTrailingPaddingAndBorder(axis, widthSize);
        }

        private static YogaConfig ConfigClone(YogaConfig oldConfig)
        {
            return new YogaConfig(oldConfig);
        }

        private static YogaNode DeepClone(YogaNode oldNode)
        {
            var node = new YogaNode(oldNode, null);
            var vec = new List<YogaNode>(oldNode.Children.Count);

            var childNode = default(YogaNode);
            foreach (var item in oldNode.Children)
            {
                childNode = DeepClone(item);
                childNode.Owner = node;
                vec.Add(childNode);
            }

            node.Children = vec;

            if (oldNode.Config != null)
                node.Config = ConfigClone(oldNode.Config);

            if (oldNode.NextChild != null)
                node.NextChild = DeepClone(oldNode.NextChild);

            return node;
        }

        private static void RoundToPixelGrid(YogaNode node,
                                       float pointScaleFactor,
                                       float? absoluteLeft,
                                       float? absoluteTop)
        {
            if (pointScaleFactor == 0.0f)
                return;

            var nodeLeft = node.Layout.Position[YogaEdge.Left];
            var nodeTop = node.Layout.Position[YogaEdge.Top];

            var nodeWidth = node.Layout.Dimensions[YogaDimension.Width];
            var nodeHeight = node.Layout.Dimensions[YogaDimension.Height];

            var absoluteNodeLeft = absoluteLeft + nodeLeft;
            var absoluteNodeTop = absoluteTop + nodeTop;

            var absoluteNodeRight = absoluteNodeLeft + nodeWidth;
            var absoluteNodeBottom = absoluteNodeTop + nodeHeight;

            // If a node has a custom measure function we never want to round down its size as this could
            // lead to unwanted text truncation.
            var textRounding = node.NodeType == YogaNodeType.Text;

            node.SetLayoutPosition(RoundValueToPixelGrid(nodeLeft, pointScaleFactor, false, textRounding), YogaEdge.Left);
            node.SetLayoutPosition(RoundValueToPixelGrid(nodeTop, pointScaleFactor, false, textRounding), YogaEdge.Top);

            // We multiply dimension by scale factor and if the result is close to the whole number, we don't
            // have any fraction
            // To verify if the result is close to whole number we want to check both floor and ceil numbers
            var hasFractionalWidth = !FloatsEqual(((nodeWidth * pointScaleFactor) % 1.0F), 0) && !FloatsEqual(((nodeWidth * pointScaleFactor) % 1.0F), 1.0F);
            var hasFractionalHeight = !FloatsEqual(((nodeHeight * pointScaleFactor) % 1.0F), 0) && !FloatsEqual(((nodeHeight * pointScaleFactor) % 1.0F), 1.0F);

            node.SetLayoutDimension(
                RoundValueToPixelGrid(
                    absoluteNodeRight,
                    pointScaleFactor,
                    (textRounding && hasFractionalWidth),
                    (textRounding && !hasFractionalWidth)) - RoundValueToPixelGrid(absoluteNodeLeft, pointScaleFactor, false, textRounding),
                YogaDimension.Width);

            node.SetLayoutDimension(
                RoundValueToPixelGrid(
                    absoluteNodeBottom,
                    pointScaleFactor,
                    (textRounding && hasFractionalHeight),
                    (textRounding && !hasFractionalHeight)) - RoundValueToPixelGrid(absoluteNodeTop, pointScaleFactor, false, textRounding),
                YogaDimension.Height);

            var childCount = GetChildCount(node);
            for (var i = 0; i < childCount; i++)
            {
                RoundToPixelGrid(GetChild(node, i), pointScaleFactor, absoluteNodeLeft, absoluteNodeTop);
            }
        }
    }
}
