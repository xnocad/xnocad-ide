﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using IDE.Controls.WPF.Docking.Commands;
using IDE.Controls.WPF.Docking.Converters;
using IDE.Controls.WPF.Docking.Layout;

namespace IDE.Controls.WPF.Docking.Controls;

public class LayoutAnchorableFloatingWindowControl : LayoutFloatingWindowControl, IOverlayWindowHost
{
    #region Members

    private LayoutAnchorableFloatingWindow _model;
    private OverlayWindow _overlayWindow = null;
    private List<IDropArea> _dropAreas = null;

    #endregion

    #region Constructors

    static LayoutAnchorableFloatingWindowControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LayoutAnchorableFloatingWindowControl), new FrameworkPropertyMetadata(typeof(LayoutAnchorableFloatingWindowControl)));
    }

    internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model, bool isContentImmutable)
       : base(model, isContentImmutable)
    {
        _model = model;
        HideWindowCommand = new RelayCommand((p) => OnExecuteHideWindowCommand(p), (p) => CanExecuteHideWindowCommand(p));
        CloseWindowCommand = new RelayCommand((p) => OnExecuteCloseWindowCommand(p), (p) => CanExecuteCloseWindowCommand(p));
        Activated += LayoutAnchorableFloatingWindowControl_Activated;
        MinWidth = _model.RootPanel.CalculatedDockMinWidth();
        MinHeight = _model.RootPanel.CalculatedDockMinHeight();

        LayoutRoot root = _model.Root as LayoutRoot;
        if (root != null)
        {
            root.Updated += OnRootUpdated;
        }
    }

    private void OnRootUpdated(object sender, EventArgs e)
    {
        if (_model?.RootPanel != null)
        {
            MinWidth = _model.RootPanel.CalculatedDockMinWidth();
            MinHeight = _model.RootPanel.CalculatedDockMinHeight();
        }
    }

    private void LayoutAnchorableFloatingWindowControl_Activated(object sender, EventArgs e)
    {
        var visibilityBinding = GetBindingExpression(VisibilityProperty);

        if (visibilityBinding == null && Visibility == Visibility.Visible)
        {
            SetVisibilityBinding();
        }
    }

    internal LayoutAnchorableFloatingWindowControl(LayoutAnchorableFloatingWindow model)
        : this(model, false)
    {
    }

    #endregion

    #region Properties

    #region SingleContentLayoutItem

    /// <summary>
    /// SingleContentLayoutItem Dependency Property
    /// </summary>
    public static readonly DependencyProperty SingleContentLayoutItemProperty = DependencyProperty.Register("SingleContentLayoutItem", typeof(LayoutItem), typeof(LayoutAnchorableFloatingWindowControl),
            new FrameworkPropertyMetadata((LayoutItem)null, new PropertyChangedCallback(OnSingleContentLayoutItemChanged)));

    /// <summary>
    /// Gets or sets the SingleContentLayoutItem property.  This dependency property 
    /// indicates the layout item of the selected content when is shown a single anchorable pane.
    /// </summary>
    public LayoutItem SingleContentLayoutItem
    {
        get
        {
            return (LayoutItem)GetValue(SingleContentLayoutItemProperty);
        }
        set
        {
            SetValue(SingleContentLayoutItemProperty, value);
        }
    }

    /// <summary>
    /// Handles changes to the SingleContentLayoutItem property.
    /// </summary>
    private static void OnSingleContentLayoutItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ( (LayoutAnchorableFloatingWindowControl)d ).OnSingleContentLayoutItemChanged(e);
    }

    /// <summary>
    /// Provides derived classes an opportunity to handle changes to the SingleContentLayoutItem property.
    /// </summary>
    protected virtual void OnSingleContentLayoutItemChanged(DependencyPropertyChangedEventArgs e)
    {
    }

    #endregion

    #endregion

    #region Overrides

    public override ILayoutElement Model
    {
        get
        {
            return _model;
        }
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        var manager = _model.Root.Manager;

        Content = manager.CreateUIElementForModel(_model.RootPanel);

        //SetBinding(VisibilityProperty, new Binding("IsVisible") { Source = _model, Converter = new BoolToVisibilityConverter(), Mode = BindingMode.OneWay, ConverterParameter = Visibility.Hidden });

        IsVisibleChanged += (s, args) =>
        {
            var visibilityBinding = GetBindingExpression(VisibilityProperty);
            if (visibilityBinding == null && IsVisible)
            {
                SetVisibilityBinding();
            }
        };

        SetBinding(SingleContentLayoutItemProperty, new Binding("Model.SinglePane.SelectedContent") { Source = this, Converter = new LayoutItemFromLayoutModelConverter() });

        _model.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_model_PropertyChanged);
    }

    protected override void OnClosed(EventArgs e)
    {
        var root = Model.Root;
        if (root != null)
        {
            LayoutRoot layoutRoot = root as LayoutRoot;
            if (layoutRoot != null)
            {
                layoutRoot.Updated -= OnRootUpdated;
            }

            root.Manager.RemoveFloatingWindow(this);
            root.CollectGarbage();
        }
        if (_overlayWindow != null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }

        base.OnClosed(e);

        if (!CloseInitiatedByUser && ( root != null ))
        {
            root.FloatingWindows.Remove(_model);
        }

        // We have to clear binding instead of creating a new empty binding.
        BindingOperations.ClearBinding(_model, VisibilityProperty);

        _model.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(_model_PropertyChanged);

        Activated -= LayoutAnchorableFloatingWindowControl_Activated;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        bool CanHide = HideWindowCommand.CanExecute(null);

        if (CloseInitiatedByUser && !KeepContentVisibleOnClose && !CanHide)
        {
            e.Cancel = true;

            if (CanHide == true)
                _model.Descendents().OfType<LayoutAnchorable>().ToArray().ForEach<LayoutAnchorable>((a) => a.Hide());
        }

        base.OnClosing(e);
    }

    protected override IntPtr FilterMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Win32Helper.WM_NCLBUTTONDOWN: //Left button down on title -> start dragging over docking manager
                if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                {
                    var anchorablePane = _model.Descendents().OfType<LayoutAnchorablePane>()
                        .FirstOrDefault(p => p.ChildrenCount > 0 && p.SelectedContent != null);
                    if (anchorablePane != null)
                    {
                        anchorablePane.SelectedContent.IsActive = true;
                    }
                    handled = true;
                }
                break;
            case Win32Helper.WM_NCRBUTTONUP:
                if (wParam.ToInt32() == Win32Helper.HT_CAPTION)
                {
                    if (OpenContextMenu())
                        handled = true;

                    //if (_model.Root.Manager.ShowSystemMenu)
                    //    WindowChrome.GetWindowChrome(this).ShowSystemMenu = !handled;
                    //else
                    //    WindowChrome.GetWindowChrome(this).ShowSystemMenu = false;
                }
                break;

        }

        return base.FilterMessage(hwnd, msg, wParam, lParam, ref handled);
    }

    #endregion

    #region Private Methods

    private void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case "RootPanel":
                if (_model.RootPanel == null)
                {
                    InternalClose();
                }

                break;

            case "IsVisible":
                if (_model.IsVisible != IsVisible)
                {
                    Visibility = _model.IsVisible ? Visibility.Visible : Visibility.Hidden;
                }

                break;
        }
    }

    private void CreateOverlayWindow()
    {
        if (_overlayWindow == null)
            _overlayWindow = new OverlayWindow(this);
        Rect rectWindow = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()), this.TransformActualSizeToAncestor());
        _overlayWindow.Left = rectWindow.Left;
        _overlayWindow.Top = rectWindow.Top;
        _overlayWindow.Width = rectWindow.Width;
        _overlayWindow.Height = rectWindow.Height;
    }

    private bool OpenContextMenu()
    {
        var ctxMenu = _model.Root.Manager.AnchorableContextMenu;
        if (ctxMenu != null && SingleContentLayoutItem != null)
        {
            ctxMenu.PlacementTarget = null;
            ctxMenu.Placement = PlacementMode.MousePoint;
            ctxMenu.DataContext = SingleContentLayoutItem;
            ctxMenu.IsOpen = true;
            return true;
        }

        return false;
    }

    private bool IsContextMenuOpen()
    {
        var ctxMenu = _model.Root.Manager.AnchorableContextMenu;
        if (ctxMenu != null && SingleContentLayoutItem != null)
        {
            return ctxMenu.IsOpen;
        }

        return false;
    }

    private void SetVisibilityBinding()
    {
        SetBinding(
          VisibilityProperty,
          new Binding("IsVisible")
          {
              Source = _model,
              Converter = new BooleanToVisibilityConverter(),
              Mode = BindingMode.OneWay,
              ConverterParameter = Visibility.Hidden
          }
        );
    }

    #endregion

    #region Public Methods

    public override void EnableBindings()
    {
        _model.PropertyChanged += _model_PropertyChanged;
        SetVisibilityBinding();
        var root = Model.Root;
        if (root != null)
        {
            LayoutRoot layoutRoot = root as LayoutRoot;
            if (layoutRoot != null)
            {
                layoutRoot.Updated += OnRootUpdated;
            }
        }

        base.EnableBindings();
    }

    public override void DisableBindings()
    {
        var root = Model.Root;
        if (root != null)
        {
            LayoutRoot layoutRoot = root as LayoutRoot;
            if (layoutRoot != null)
            {
                layoutRoot.Updated -= OnRootUpdated;
            }
        }

        BindingOperations.ClearBinding(_model, VisibilityProperty);
        _model.PropertyChanged -= _model_PropertyChanged;

        base.DisableBindings();
    }

    #endregion

    #region Commands

    #region HideWindowCommand

    public ICommand HideWindowCommand
    {
        get;
        private set;
    }

    private bool CanExecuteHideWindowCommand(object parameter)
    {
        if (Model == null)
            return false;

        var root = Model.Root;
        if (root == null)
            return false;

        var manager = root.Manager;
        if (manager == null)
            return false;

        bool canExecute = false;
        foreach (var anchorable in this.Model.Descendents().OfType<LayoutAnchorable>().ToArray())
        {
            if (!anchorable.CanHide)
            {
                canExecute = false;
                break;
            }

            var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
            if (anchorableLayoutItem == null ||
                anchorableLayoutItem.HideCommand == null ||
                !anchorableLayoutItem.HideCommand.CanExecute(parameter))
            {
                canExecute = false;
                break;
            }

            canExecute = true;
        }

        return canExecute;
    }

    private void OnExecuteHideWindowCommand(object parameter)
    {
        var manager = Model.Root.Manager;
        foreach (var anchorable in this.Model.Descendents().OfType<LayoutAnchorable>().ToArray())
        {
            var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
            anchorableLayoutItem.HideCommand.Execute(parameter);
        }

        Hide(); // Bring toolwindows inside hidden FloatingWindow back requires restart of app
    }
    #endregion

    #region CloseWindowCommand
    public ICommand CloseWindowCommand
    {
        get;
        private set;
    }

    private bool CanExecuteCloseWindowCommand(object parameter)
    {
        if (Model == null)
            return false;

        var root = Model.Root;
        if (root == null)
            return false;

        var manager = root.Manager;
        if (manager == null)
            return false;

        bool canExecute = false;
        foreach (var anchorable in this.Model.Descendents().OfType<LayoutAnchorable>().ToArray())
        {
            if (!anchorable.CanClose)
            {
                canExecute = false;
                break;
            }

            var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
            if (anchorableLayoutItem == null ||
                anchorableLayoutItem.CloseCommand == null ||
                !anchorableLayoutItem.CloseCommand.CanExecute(parameter))
            {
                canExecute = false;
                break;
            }

            canExecute = true;
        }

        return canExecute;
    }

    private void OnExecuteCloseWindowCommand(object parameter)
    {
        var manager = Model.Root.Manager;
        foreach (var anchorable in this.Model.Descendents().OfType<LayoutAnchorable>().ToArray())
        {
            var anchorableLayoutItem = manager.GetLayoutItemFromModel(anchorable) as LayoutAnchorableItem;
            anchorableLayoutItem.CloseCommand.Execute(parameter);
        }
    }

    #endregion

    #endregion

    #region IOverlayWindowHost

    bool IOverlayWindowHost.HitTest(Point dragPoint)
    {
        Rect detectionRect = new Rect(this.PointToScreenDPIWithoutFlowDirection(new Point()), this.TransformActualSizeToAncestor());
        return detectionRect.Contains(dragPoint);
    }

    DockingManager IOverlayWindowHost.Manager
    {
        get
        {
            return _model.Root.Manager;
        }
    }

    IOverlayWindow IOverlayWindowHost.ShowOverlayWindow(LayoutFloatingWindowControl draggingWindow)
    {
        CreateOverlayWindow();
        _overlayWindow.Owner = draggingWindow;
        _overlayWindow.EnableDropTargets();
        _overlayWindow.Show();

        return _overlayWindow;
    }

    void IOverlayWindowHost.HideOverlayWindow()
    {
        _dropAreas = null;
        _overlayWindow.Owner = null;
        _overlayWindow.HideDropTargets();
    }

    IEnumerable<IDropArea> IOverlayWindowHost.GetDropAreas(LayoutFloatingWindowControl draggingWindow)
    {
        if (_dropAreas != null)
            return _dropAreas;

        _dropAreas = new List<IDropArea>();

        if (draggingWindow.Model is LayoutDocumentFloatingWindow)
            return _dropAreas;

        var rootVisual = ( Content as FloatingWindowContentHost ).RootVisual;

        foreach (var areaHost in rootVisual.FindVisualChildren<LayoutAnchorablePaneControl>())
        {
            _dropAreas.Add(new DropArea<LayoutAnchorablePaneControl>(
                areaHost,
                DropAreaType.AnchorablePane));
        }
        foreach (var areaHost in rootVisual.FindVisualChildren<LayoutDocumentPaneControl>())
        {
            _dropAreas.Add(new DropArea<LayoutDocumentPaneControl>(
                areaHost,
                DropAreaType.DocumentPane));
        }

        return _dropAreas;
    }

    #endregion
}
