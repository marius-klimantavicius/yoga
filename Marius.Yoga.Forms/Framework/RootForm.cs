// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace BlinForms.Framework
{
    public partial class RootForm : Form, IWindowsFormsControlHandler
    {
        public RootForm()
        {
            InitializeComponent();
        }

        public Control Control => this;
        public object TargetElement => this;

        public void ApplyAttribute(ulong attributeEventHandlerId, string attributeName, object attributeValue, string attributeEventUpdatesAttributeName)
        {
            global::BlinForms.Framework.Controls.FormsComponentBase.ApplyAttribute(this, attributeEventHandlerId, attributeName, attributeValue, attributeEventUpdatesAttributeName);
        }
    }
}
