﻿namespace IDE.Controls
{
    using System.Windows;
    using System.Windows.Controls;

    public class PinableListViewItem : ListViewItem
    {
        private static readonly DependencyProperty IsMouseOverListViewItemProperty =
            DependencyProperty.Register("IsMouseOverListViewItem",
                                        typeof(bool),
                                        typeof(PinableListViewItem),
                                        new FrameworkPropertyMetadata(IsMouseOverListViewItemChanged));

        public bool IsMouseOverListViewItem
        {
            get { return (bool)GetValue(IsMouseOverListViewItemProperty); }

            set { SetValue(IsMouseOverListViewItemProperty, value); }
        }

        protected override void OnMouseEnter(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            IsMouseOverListViewItem = true;
        }

        protected override void OnMouseLeave(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseEnter(e);

            IsMouseOverListViewItem = false;
        }

        private static void IsMouseOverListViewItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as PinableListViewItem;

            if (item != null)
            {
                item.IsMouseOverListViewItem = (bool)e.NewValue;
            }
        }
    }
}
