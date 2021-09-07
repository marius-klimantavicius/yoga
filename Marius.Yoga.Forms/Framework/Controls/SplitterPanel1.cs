﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.MobileBlazorBindings.Core;

namespace BlinForms.Framework.Controls
{
#pragma warning disable CA1812 // Internal class that is apparently never instantiated; this class is instantiated generically
    internal class SplitterPanel1 : SplitterPanelBase
#pragma warning restore CA1812 // Internal class that is apparently never instantiated
    {
        static SplitterPanel1()
        {
            ElementHandlerRegistry.RegisterElementHandler<SplitterPanel1>(
                (_, parentControl) => GetSplitterPanel(((IWindowsFormsControlHandler)parentControl).Control, panelNumber: 1));
        }
    }
}
