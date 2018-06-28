using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marius.Layout
{
    public class FlexItem
    {
        private float _width = float.NaN;
        private float _height = float.NaN;

        private float _left = float.NaN;
        private float _right = float.NaN;
        private float _top = float.NaN;
        private float _bottom = float.NaN;

        private float _paddingLeft = 0;
        private float _paddingRight = 0;
        private float _paddingTop = 0;
        private float _paddingBottom = 0;

        private float _marginLeft = 0;
        private float _marginRight = 0;
        private float _marginTop = 0;
        private float _marginBottom = 0;

        private FlexAlign _justifyContent = FlexAlign.Start;
        private FlexAlign _alignContent = FlexAlign.Stretch;
        private FlexAlign _alignItems = FlexAlign.Stretch;
        private FlexAlign _alignSelf = FlexAlign.Auto;

        private FlexPosition _position = FlexPosition.Relative;
        private FlexDirection _direction = FlexDirection.Column;
        private FlexWrap _wrap = FlexWrap.NoWrap;

        private float _grow = 0;
        private float _shrink = 1;
        private int _order = 0;
        private float _basis = float.NaN;

        private FlexMeasure _measure = null;
        private IList<FlexItem> _children = new List<FlexItem>();

        public float Width { get { return _width; } set { _width = value; OnPropertyChanged(FlexProperty.Width); } }
        public float Height { get { return _height; } set { _height = value; OnPropertyChanged(FlexProperty.Height); } }

        public float Left { get { return _left; } set { _left = value; OnPropertyChanged(FlexProperty.Left); } }
        public float Right { get { return _right; } set { _right = value; OnPropertyChanged(FlexProperty.Right); } }
        public float Top { get { return _top; } set { _top = value; OnPropertyChanged(FlexProperty.Top); } }
        public float Bottom { get { return _bottom; } set { _bottom = value; OnPropertyChanged(FlexProperty.Bottom); } }

        public float PaddingLeft { get { return _paddingLeft; } set { _paddingLeft = value; OnPropertyChanged(FlexProperty.PaddingLeft); } }
        public float PaddingRight { get { return _paddingRight; } set { _paddingRight = value; OnPropertyChanged(FlexProperty.PaddingRight); } }
        public float PaddingTop { get { return _paddingTop; } set { _paddingTop = value; OnPropertyChanged(FlexProperty.PaddingTop); } }
        public float PaddingBottom { get { return _paddingBottom; } set { _paddingBottom = value; OnPropertyChanged(FlexProperty.PaddingBottom); } }

        public float MarginLeft { get { return _marginLeft; } set { _marginLeft = value; OnPropertyChanged(FlexProperty.MarginLeft); } }
        public float MarginRight { get { return _marginRight; } set { _marginRight = value; OnPropertyChanged(FlexProperty.MarginRight); } }
        public float MarginTop { get { return _marginTop; } set { _marginTop = value; OnPropertyChanged(FlexProperty.MarginTop); } }
        public float MarginBottom { get { return _marginBottom; } set { _marginBottom = value; OnPropertyChanged(FlexProperty.MarginBottom); } }

        public FlexAlign JustifyContent { get { return _justifyContent; } set { _justifyContent = value; OnPropertyChanged(FlexProperty.JustifyContent); } }
        public FlexAlign AlignContent { get { return _alignContent; } set { _alignContent = value; OnPropertyChanged(FlexProperty.AlignContent); } }
        public FlexAlign AlignItems { get { return _alignItems; } set { _alignItems = value; OnPropertyChanged(FlexProperty.AlignItems); } }
        public FlexAlign AlignSelf { get { return _alignSelf; } set { _alignSelf = value; OnPropertyChanged(FlexProperty.AlignSelf); } }

        public FlexPosition Position { get { return _position; } set { _position = value; OnPropertyChanged(FlexProperty.Position); } }
        public FlexDirection Direction { get { return _direction; } set { _direction = value; OnPropertyChanged(FlexProperty.Direction); } }
        public FlexWrap Wrap { get { return _wrap; } set { _wrap = value; OnPropertyChanged(FlexProperty.Wrap); } }

        public float Grow { get { return _grow; } set { _grow = value; OnPropertyChanged(FlexProperty.Grow); } }
        public float Shrink { get { return _shrink; } set { _shrink = value; OnPropertyChanged(FlexProperty.Shrink); } }
        public int Order { get { return _order; } set { _order = value; OnPropertyChanged(FlexProperty.Order); } }
        public float Basis { get { return _basis; } set { _basis = value; OnPropertyChanged(FlexProperty.Basis); } }

        public FlexMeasure Measure { get { return _measure; } set { _measure = value; OnPropertyChanged(FlexProperty.Measure); } }

        public int Count { get { return _children.Count; } }

        public FlexItem this[int index]
        {
            get { return _children[index]; }
        }

        public FlexItem Parent { get; private set; }

        public FlexItem Root
        {
            get
            {
                var current = this;
                while (current.Parent != null)
                    current = current.Parent;
                return current;
            }
        }

        public float LayoutX { get; set; }
        public float LayoutY { get; set; }
        public float LayoutWidth { get; set; }
        public float LayoutHeight { get; set; }

        public bool ShouldOrderChildren { get; set; }

        public void Add(FlexItem child)
        {
            if (child.Parent != null)
                throw new InvalidOperationException("Child already has a parent");

            child.Parent = this;
            _children.Add(child);
        }

        public void Insert(int index, FlexItem child)
        {
            if (child.Parent != null)
                throw new InvalidOperationException("Child already has a parent");

            child.Parent = this;
            _children.Insert(index, child);
        }

        public void Delete(int index)
        {
            var child = _children[index];
            child.Parent = null;

            _children.RemoveAt(index);
        }

        private void OnPropertyChanged(FlexProperty property)
        {
            if (property == FlexProperty.Order)
                UpdateShouldOrderChildren();
        }

        private void UpdateShouldOrderChildren()
        {
            if (_order != 0 && Parent != null)
                Parent.ShouldOrderChildren = true;
        }
    }
}
