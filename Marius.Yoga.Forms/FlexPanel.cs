using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace Marius.Yoga.Forms
{
    public class FlexPanel : Panel
    {
        private static readonly YogaLayoutEngine _layoutEngine = new YogaLayoutEngine();

        public YogaNode Node { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Bindable(false)]
        [Browsable(false)]
        public override LayoutEngine LayoutEngine => _layoutEngine;

        public YogaFlexDirection FlexDirection
        {
            get { return Node.FlexDirection; }
            set
            {
                Node.FlexDirection = value;
                if (Node.IsDirty)
                    PerformLayout();
            }
        }

        public FlexPanel()
        {
            Node = new YogaNode();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            var indexOf = Controls.IndexOf(e.Control);
            YogaLayoutEngine.AddControl(Node, indexOf, e.Control);
            base.OnControlAdded(e);
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            YogaLayoutEngine.RemoveControl(Node, e.Control);
            base.OnControlRemoved(e);
        }
    }

    public class YogaLayoutEngine : LayoutEngine
    {
        private static readonly ConditionalWeakTable<Control, YogaNode> _controlNodes = new ConditionalWeakTable<Control, YogaNode>();

        public override bool Layout(object container, LayoutEventArgs layoutEventArgs)
        {
            var panel = (FlexPanel)container;
            foreach (var childNode in panel.Node)
            {
                var control = (Control)childNode.Data;
                childNode.MinHeight = 100;
                childNode.MinWidth = 100;
                childNode.MarginTop = control.Margin.Top;
                childNode.MarginLeft = control.Margin.Left;
                childNode.MarginBottom = control.Margin.Bottom;
                childNode.MarginRight = control.Margin.Right;
            }

            var size = panel.ClientSize;
            panel.Node.CalculateLayout(size.Width, size.Height, YogaDirection.Inherit);
            foreach (var childNode in panel.Node)
            {
                var control = (Control)childNode.Data;
                control.SetBounds((int)childNode.LayoutX, (int)childNode.LayoutY, (int)childNode.LayoutWidth, (int)childNode.LayoutHeight);
            }

            return false;
        }

        internal static void AddControl(YogaNode parent, int index, Control control)
        {
            var controlNode = _controlNodes.GetValue(control, s => new YogaNode() { Data = s });
            parent.Insert(index, controlNode);
        }

        internal static void RemoveControl(YogaNode parent, Control control)
        {
            if (_controlNodes.TryGetValue(control, out var controlNode))
            {
                parent.Remove(controlNode);
                _controlNodes.Remove(control);
            }
        }
    }

    public interface IYogaElement
    {
        YogaNode Node { get; }
    }
}
