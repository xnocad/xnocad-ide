﻿using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

using IDE.Core.ViewModels;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections;
using IDE.Core.Designers;
using IDE.Core.Storage;
using IDE.Core;
using IDE.Core.Commands;
using IDE.Core.Toolbars;
using IDE.Core.Interfaces;
using IDE.Core.Common;
using System.Threading.Tasks;
using IDE.Core.Types.Media;
using IDE.Core.Presentation.ObjectFinding;
using IDE.Core.Presentation.Placement;

namespace IDE.Documents.Views;

public class FootprintDesignerFileViewModel : CanvasDesignerFileViewModel, IFootprintDesigner
{

    public FootprintDesignerFileViewModel(
            IObjectFinder objectFinder,
            IDispatcherHelper dispatcher,
            IDebounceDispatcher drawingChangedDebouncer,
            IDebounceDispatcher selectionDebouncer,
            IDirtyMarkerTypePropertiesMapper dirtyMarkerTypePropertiesMapper,
            IPlacementToolFactory placementToolFactory)
        : base(dispatcher, drawingChangedDebouncer, selectionDebouncer, dirtyMarkerTypePropertiesMapper, placementToolFactory)
    {

        footprintDocument = new Footprint();

        Toolbar = new FootprintToolbar(this);

        PropertyChanged += FootprintDesignerFileViewModel_PropertyChanged;

        var docSize = 254;
        var halfSize = docSize * 0.5;
        DocumentWidth = docSize;
        DocumentHeight = docSize;
        Origin = new XPoint(halfSize, halfSize);

        canvasGrid.SetUnit(new Core.Units.MillimeterUnit(0.1));
        _objectFinder = objectFinder;
    }

    void FootprintDesignerFileViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SelectedLayer):
                if (IsPlacingItem())
                {
                    //var placingItems = canvasModel.PlacingObject.PlacingObjects.OfType<SingleLayerBoardCanvasItem>().ToList();
                    //if (placingItems != null)
                    //    placingItems.ForEach(c => c.Layer = SelectedLayer);
                    var placingitem = PlacementTool.CanvasItem as SingleLayerBoardCanvasItem;
                    if (placingitem != null)
                        placingitem.Layer = SelectedLayer;
                }
                HandleLayers();
                break;
            case nameof(SelectedLayerGroup):
            case nameof(MaskUnselectedLayer):
            case nameof(HideUnselectedLayer):
                {
                    HandleLayers();
                    break;
                }
        }
    }

    protected override bool CanZoomToFitCommand()
    {
        return Is2DMode();
    }

    protected override bool CanZoomToSelectedItemsCommand()
    {
        return Is2DMode();
    }

    bool Is2DMode()
    {
        return designerViewMode == DesignerViewMode.ViewMode2D;
    }

    public override IList<IDocumentToolWindow> GetToolWindowsWhenActive()
    {
        var tools = new List<IDocumentToolWindow>();

        tools.Add(ServiceProvider.GetToolWindow<LayersToolWindowViewModel>());
        tools.Add(ServiceProvider.GetToolWindow<DocumentOverviewViewModel>());
        tools.Add(ServiceProvider.GetToolWindow<SelectionFilterToolViewModel>());
        tools.Add(ServiceProvider.GetToolWindow<Preview3DWindowViewModel>());

        documentTools = tools;

        return tools;
    }

    void HandleLayers()
    {
        if (LayerItems != null)
            foreach (var layer in LayerItems)
                layer.HandleLayer();
    }

    #region Fields


    Footprint footprintDocument;

    #endregion Fields

    IList<IOverviewSelectNode> BuildCategories()
    {
        var list = new List<IOverviewSelectNode>();
        var primitivesCat = new OverviewFolderNode { Name = "Primitives" };
        list.Add(primitivesCat);
        var padsCat = new OverviewFolderNode { Name = "Pads" };
        list.Add(padsCat);
        var padItems = GetItems().OfType<IPadCanvasItem>().ToList();
        var pads = padItems.Select(p => new OverviewSelectNode
        {
            DataItem = p,
        });
        padsCat.Children.AddRange(pads);

        primitivesCat.Children.AddRange(GetItems().Except(padItems).Select(p => new OverviewSelectNode
        {
            DataItem = p
        }));

        return list;
    }

    IList<IOverviewSelectNode> categories;
    public IList<IOverviewSelectNode> Categories
    {
        get
        {
            return categories;
        }
        set
        {
            categories = value;
            OnPropertyChanged(nameof(Categories));
        }
    }

    public Task RefreshOverview()
    {
        return Task.Run(() =>
        {
            var nodes = BuildCategories();

            _dispatcher.RunOnDispatcher(() => Categories = nodes);
        });
    }

    #region Layer selection

    ILayerDesignerItem selectedLayer;

    public ILayerDesignerItem SelectedLayer
    {
        get { return selectedLayer; }
        set
        {
            selectedLayer = value;
            OnPropertyChanged(nameof(SelectedLayer));
        }
    }

    ILayerGroupDesignerItem selectedLayerGroup;
    public ILayerGroupDesignerItem SelectedLayerGroup
    {
        get { return selectedLayerGroup; }
        set
        {
            if (selectedLayerGroup == value) return;
            selectedLayerGroup = value;

            if (selectedLayerGroup != null)
            {
                SelectedLayer = selectedLayerGroup.Layers.FirstOrDefault(l => l.LayerType == LayerType.Signal);
            }


            OnPropertyChanged(nameof(SelectedLayerGroup));
        }
    }

    bool maskUnselectedLayer;
    public bool MaskUnselectedLayer
    {
        get { return maskUnselectedLayer; }
        set
        {
            if (maskUnselectedLayer == value) return;
            maskUnselectedLayer = value;
            OnPropertyChanged(nameof(MaskUnselectedLayer));
        }
    }


    bool hideUnselectedLayer;
    public bool HideUnselectedLayer
    {
        get { return hideUnselectedLayer; }
        set
        {
            if (hideUnselectedLayer == value) return;
            hideUnselectedLayer = value;
            OnPropertyChanged(nameof(HideUnselectedLayer));
        }
    }

    #endregion


    IList canSelectList;
    public override IList CanSelectList
    {
        get
        {
            if (canSelectList == null)
                canSelectList = new List<SelectionFilterItemViewModel>
            {
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Lines",
                    Type = typeof(LineBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Texts",
                    Type = typeof(TextBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Small Text Monoline",
                    Type = typeof(TextSingleLineBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Rectangles",
                    Type = typeof(RectangleBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Polygons",
                    Type = typeof(PolygonBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Circles",
                    Type = typeof(CircleBoardCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Arcs",
                    Type = typeof(ArcBoardCanvasItem)
                },

                new SelectionFilterItemViewModel
                {
                    TooltipText = "Holes",
                    Type = typeof(HoleCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Pads",
                    Type = typeof(PadThtCanvasItem)
                },
                new SelectionFilterItemViewModel
                {
                    TooltipText = "Smds",
                    Type = typeof(PadSmdCanvasItem)
                }
        };

            return canSelectList;
        }
    }

    protected override void RefreshFromCache()
    {
        if (State != DocumentState.IsEditing)
            return;

        _dispatcher.RunOnDispatcher(() =>
        {
            UpdateFootprintModels();
        });
    }




    #region Toolbox

    protected override void InitToolbox()
    {
        base.InitToolbox();

        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Line",
            Type = typeof(LineBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Text",
            Type = typeof(TextBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Small Text Monoline",
            Type = typeof(TextSingleLineBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Rectangle",
            Type = typeof(RectangleBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Polygon",
            Type = typeof(PolygonBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Circle",
            Type = typeof(CircleBoardCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Arc",
            Type = typeof(ArcBoardCanvasItem)
        });

        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Hole",
            Type = typeof(HoleCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Pad",
            Type = typeof(PadThtCanvasItem)
        });
        Toolbox.Primitives.Add(new PrimitiveItem
        {
            TooltipText = "Smd",
            Type = typeof(PadSmdCanvasItem)
        });

    }

    #endregion Toolbox

    ObservableCollection<LayerGroupDesignerItem> layerGroups = new ObservableCollection<LayerGroupDesignerItem>();
    public IList LayerGroups
    {
        get
        {
            return layerGroups;
        }
    }

    public IList<ILayerDesignerItem> LayerItems { get; set; } = new ObservableCollection<ILayerDesignerItem>();

    DesignerViewMode designerViewMode;
    public DesignerViewMode DesignerViewMode
    {
        get { return designerViewMode; }
        set
        {
            designerViewMode = value;
            OnPropertyChanged(nameof(DesignerViewMode));
            RefreshToolWindowsVisibility();
        }
    }

    private IPreviewEditCanvasViewModel boardPreview3DViewModel;
    public IPreviewEditCanvasViewModel BoardPreview3DViewModel
    {
        get { return boardPreview3DViewModel; }
        set
        {
            if (boardPreview3DViewModel == value) return;

            //if (boardPreview3DViewModel != null)
            //{
            //    boardPreview3DViewModel.Model3DCanvasModel.DrawingChanged -= CanvasModel_DrawingChanged;
            //}

            boardPreview3DViewModel = value;
            //if (boardPreview3DViewModel != null)
            //{
            //    boardPreview3DViewModel.Model3DCanvasModel.DrawingChanged += CanvasModel_DrawingChanged;
            //}

            OnPropertyChanged(nameof(BoardPreview3DViewModel));
        }
    }

    List<IDocumentToolWindow> documentTools;

    void RefreshToolWindowsVisibility()
    {
        if (documentTools == null)
            return;

        ShowVisibleToolsFiltered(documentTools);
    }

    protected override bool IsToolWindowVisible(IDocumentToolWindow toolWindow)
    {
        var isView3D = designerViewMode == DesignerViewMode.ViewMode3D;

        if (toolWindow is Preview3DWindowViewModel)
            return isView3D;

        return !isView3D;
    }

    protected override void SaveDocumentInternal(string filePath)
    {
        //remove the currently adding item si that it won't be saved
        ISelectableItem placeObject = null;
        if (IsPlacingItem())
        {
            placeObject = PlacementTool.CanvasItem;
            RemoveItem(placeObject);
        }

        footprintDocument.Name = Path.GetFileNameWithoutExtension(filePath);
        var itemsToSave = (from l in Items.OfType<LayerDesignerItem>()
                           from s in l.Items
                           select (LayerPrimitive)(s as BaseCanvasItem).SaveToPrimitive())
                            .Union(Items.Cast<BaseCanvasItem>()
                                                    .Select(d => (LayerPrimitive)d.SaveToPrimitive()));
        footprintDocument.Items = itemsToSave.ToList();
        SaveLayers();
        SaveFootprintModel();

        XmlHelper.Save(footprintDocument, filePath);

        //PostSave
        if (placeObject != null)
            AddItem(placeObject);
    }

    void SaveFootprintModel()
    {
        footprintDocument.Models = modelsDictionary.Keys
        .Select(m => new ModelData
        {
            ModelId = modelsDictionary[m].Id,
            ModelName = modelsDictionary[m].Name,
            ModelLibrary = modelsDictionary[m].Library,
            CenterX = m.X,
            CenterY = m.Y,
            CenterZ = m.Z,
            RotationX = m.RotationX,
            RotationY = m.RotationY,
            RotationZ = m.RotationZ
        }).ToList();
    }


    #region LoadFile


    protected override Task LoadDocumentInternal(string filePath)
    {
        return Task.Run(() =>
        {
            footprintDocument = XmlHelper.Load<Footprint>(filePath);

            //assign a new id if needed
            if (string.IsNullOrEmpty(footprintDocument.Id))
            {
                footprintDocument.Id = LibraryItem.GetNextId();
                IsDirty = true;
            }

            LoadLayers();

            if (footprintDocument.Items != null)
            {
                foreach (var primitive in footprintDocument.Items)
                {
                    var canvasItem = (BoardCanvasItemViewModel)primitive.CreateDesignerItem();
                    canvasItem.LayerDocument = this;
                    canvasItem.LoadLayers();

                    AddItem(canvasItem);
                }
            }

            footprintDocument.Models.ForEach(m => LoadFootprintModel(m));
        });
    }

    protected override async Task AfterLoadDocumentInternal()
    {
        await base.AfterLoadDocumentInternal();
        await RefreshOverview();

        await RepourPolygons();
    }

    async Task RepourPolygons()
    {
        var polygons = from l in LayerItems
                       from p in l.Items.OfType<IRepourGeometry>()
                       select p;

        foreach (var p in polygons)
            await p.RepourPolygonAsync();
    }
    #endregion LoadFile

    #region Layers
    void LoadLayers()
    {
        IList<Layer> layers = null;
        if (footprintDocument.Layers != null && footprintDocument.Layers.Count > 0)
        {
            layers = footprintDocument.Layers;
        }
        else
        {
            layers = Footprint.CreateDefaultLayers();
        }
        footprintDocument.Layers = layers.ToList();
        var groups = LayerGroup.GetLayerGroupDefaults(layers);

        var layerOrder = 0;// layers.Count;
        var layerItems = layers.Select(l => new LayerDesignerItem(this)
        {
            LayerName = l.Name,
            LayerId = l.Id,
            StackOrder = l.StackOrder,
            LayerOrder = layerOrder--,
            LayerType = l.Type,
            LayerColor = XColor.FromHexString(l.Color),
            Thickness = l.Thickness,
        }).ToList();

        var groupItems = new List<LayerGroupDesignerItem>();
        foreach (var g in groups)
        {
            var filteredLayers = layerItems.Where(l => g.Layers.Any(gl => gl.Id == l.LayerId)).Cast<ILayerDesignerItem>().ToList();
            var newG = new LayerGroupDesignerItem { Name = g.Name };
            newG.LoadLayers(filteredLayers);
            groupItems.Add(newG);
        }

        _dispatcher.RunOnDispatcher(() =>
        {
            LayerItems.Clear();
            LayerItems.AddRange(layerItems);
            AddItems(layerItems);
            LayerGroups.Clear();
            layerGroups.AddRange(groupItems);

            SelectedLayerGroup = layerGroups[0];

        });

    }

    void SaveLayers()
    {
        footprintDocument.Layers = LayerItems.Cast<LayerDesignerItem>().Select(l => new Layer
        {
            Id = l.LayerId,
            Name = l.LayerName,
            Type = l.LayerType,
            //IsVisible = l.IsVisible,
            Color = l.LayerColor.ToHexString()
        })//.OrderBy(l => l.Id)
        .ToList();
    }

    #endregion Layers


    ICommand addFootprintCommand;

    public ICommand AddFootprintCommand
    {
        get
        {
            if (addFootprintCommand == null)
                addFootprintCommand = CreateCommand(
                    p =>
                {
                    ClearSelectedItems();

                    CancelPlacement();


                    var itemSelectDlg = new ItemSelectDialogViewModel(TemplateType.Footprint, GetCurrentProjectInfo());

                    if (itemSelectDlg.ShowDialog() == true)
                    {
                        var symbol = itemSelectDlg.SelectedItem.Document as Footprint;

                        if (symbol.Items != null)
                        {
                            var canvasItems = symbol.Items.Select(c => c.CreateDesignerItem()).ToList();

                            var rect = XRect.Empty;
                            foreach (var item in canvasItems)
                                rect.Union(item.GetBoundingRectangle());

                            var group = new VolatileGroupCanvasItem();
                            canvasItems.ForEach(c =>
                            {
                                var canvasItem = c as BoardCanvasItemViewModel;
                                canvasItem.LayerDocument = this;
                                canvasItem.ParentObject = group;
                                canvasItem.LoadLayers();
                                canvasItem.Translate(-rect.X, -rect.Y);
                            });

                            group.Items = canvasItems.ToList();

                            StartPlacement(group);
                        }
                    }
                },
                    p => Is2DMode()

                   );

            return addFootprintCommand;
        }
    }

    #region Footprint Generator

    public FootprintGeneratorViewModel FootprintGenerator { get; set; }

    ICommand showFootprintGeneratorCommand;
    public ICommand ShowFootprintGeneratorCommand
    {
        get
        {
            if (showFootprintGeneratorCommand == null)
            {
                showFootprintGeneratorCommand = CreateCommand(p =>
                {
                    ClearSelectedItems();
                    CancelPlacement();

                    FootprintGenerator = new FootprintGeneratorViewModel(this);
                    FootprintGenerator.Close += () =>
                    {
                        FootprintGenerator = null;
                        ShowFootprintGenerator = false;
                    };
                    OnPropertyChanged(nameof(FootprintGenerator));
                    ShowFootprintGenerator = true;
                }
                  , p => Is2DMode()

                  );
            }

            return showFootprintGeneratorCommand;
        }
    }

    bool showFootprintGenerator;
    public bool ShowFootprintGenerator
    {
        get { return showFootprintGenerator; }
        set
        {
            showFootprintGenerator = value;
            OnPropertyChanged(nameof(ShowFootprintGenerator));
        }
    }

    #endregion


    ICommand show2DCommand;
    public ICommand Show2DCommand
    {
        get
        {
            if (show2DCommand == null)
            {
                show2DCommand = CreateCommand(p =>
                  {
                      Show2D();
                  },
                p => DesignerViewMode == DesignerViewMode.ViewMode3D);
            }

            return show2DCommand;
        }
    }

    void Show2D()
    {
        if (DesignerViewMode == DesignerViewMode.ViewMode2D)
            return;

        DesignerViewMode = DesignerViewMode.ViewMode2D;
    }

    ICommand show3DCommand;
    public ICommand Show3DCommand
    {
        get
        {
            if (show3DCommand == null)
            {
                show3DCommand = CreateCommand(async p =>
                {
                    await Show3D();
                },
                p => DesignerViewMode == DesignerViewMode.ViewMode2D);
            }

            return show3DCommand;
        }
    }

    async Task Show3D()
    {
        if (DesignerViewMode == DesignerViewMode.ViewMode3D)
            return;

        await ShowLayersGeometries();

        DesignerViewMode = DesignerViewMode.ViewMode3D;
    }

    private async Task ShowLayersGeometries()
    {
        var boardPreviewHelper = new Board3DPreviewGlobalHelper(_dispatcher);
        var model = await boardPreviewHelper.GeneratePreview(this);

        model.Items = modelsDictionary.Keys.Cast<ISelectableItem>().ToList();

        BoardPreview3DViewModel = model;
    }

    Dictionary<GroupMeshItem, ModelDocument> modelsDictionary = new Dictionary<GroupMeshItem, ModelDocument>();


    ICommand associate3DModelCommand;
    public ICommand Associate3DModelCommand
    {
        get
        {
            if (associate3DModelCommand == null)
            {
                associate3DModelCommand = CreateCommand(p =>
                {
                    var itemSelectDlg = new ItemSelectDialogViewModel(TemplateType.Model, GetCurrentProjectInfo());

                    if (itemSelectDlg.ShowDialog() == true)
                    {
                        if (itemSelectDlg.SelectedItem != null)
                        {
                            var modelDoc = itemSelectDlg.SelectedItem.Document as ModelDocument;
                            var modelData = new ModelData
                            {
                                ModelId = modelDoc.Id,
                                ModelName = modelDoc.Name,
                                ModelLibrary = modelDoc.Library,
                            };
                            var modelItem = LoadFootprintModel(modelData);
                            AllignModelWithPads();
                            RefreshFootprintPreviewModels();

                            if (modelItem != null)
                            {
                                var cm = BoardPreview3DViewModel;
                                var modelDocument = modelsDictionary[modelItem];

                                RegisterUndoActionExecuted(
                                      undo: o =>
                                      {
                                          cm.RemoveItem(modelItem);
                                          modelsDictionary.Remove(modelItem);

                                          return modelItem;
                                      },
                                      redo: o =>
                                      {
                                          cm.AddItem(modelItem);
                                          modelsDictionary.Add(modelItem, modelDocument);
                                          return modelItem;
                                      },
                                      modelItem);
                            }


                            IsDirty = true;
                        }
                    }
                },
                p => DesignerViewMode == DesignerViewMode.ViewMode3D);
            }

            return associate3DModelCommand;
        }
    }


    ICommand remove3DModelCommand;
    private readonly IObjectFinder _objectFinder;

    public ICommand Remove3DModelCommand
    {
        get
        {
            if (remove3DModelCommand == null)
            {
                remove3DModelCommand = CreateCommand(p =>
                {
                    RemoveSelected3DModels();
                },
                p => DesignerViewMode == DesignerViewMode.ViewMode3D
                && BoardPreview3DViewModel?.SelectedItems.Count > 0);
            }

            return remove3DModelCommand;
        }
    }

    private void RemoveSelected3DModels()
    {
        var cm = BoardPreview3DViewModel;
        var selectedItems = cm.SelectedItems.ToList();
        if (selectedItems.Count > 0)
        {
            var groupMeshItems = selectedItems.OfType<GroupMeshItem>().ToList();
            var modelsCopy = modelsDictionary.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            cm.RemoveItems(selectedItems);

            foreach (var selectedItem in groupMeshItems)
            {
                modelsDictionary.Remove(selectedItem);
            }

            RegisterUndoActionExecuted(undo: o =>
            {
                cm.AddItems(selectedItems);
                foreach (var selectedItem in groupMeshItems)
                {
                    modelsDictionary.Add(selectedItem, modelsCopy[selectedItem]);
                }

                return null;
            },
            redo: o =>
            {
                cm.RemoveItems(selectedItems);
                foreach (var selectedItem in groupMeshItems)
                {
                    modelsDictionary.Remove(selectedItem);
                }

                return null;
            }, null);

            IsDirty = true;
        }
    }

    public override void DeleteSelectedItems()
    {
        if (DesignerViewMode == DesignerViewMode.ViewMode3D)
        {
            RemoveSelected3DModels();
            return;
        }
        base.DeleteSelectedItems();
    }

    private GroupMeshItem LoadFootprintModel(ModelData modelData)
    {
        if (modelData == null)
            return null;

        var project = GetCurrentProjectInfo();
        var modelDoc = _objectFinder.FindObject<ModelDocument>(project, modelData.ModelLibrary, modelData.ModelId);

        if (modelDoc != null && modelDoc.Items != null)
        {
            var groupItem = new GroupMeshItem();
            groupItem.X = modelData.CenterX;
            groupItem.Y = modelData.CenterY;
            groupItem.Z = modelData.CenterZ;
            groupItem.RotationX = modelData.RotationX;
            groupItem.RotationY = modelData.RotationY;
            groupItem.RotationZ = modelData.RotationZ;

            groupItem.IsPlaced = true;

            groupItem.Items.AddRange(modelDoc.Items.Select(item => (BaseMeshItem)item.CreateDesignerItem()));
            foreach (var item in groupItem.Items)
                item.ParentObject = groupItem;

            modelsDictionary.Add(groupItem, modelDoc);

            return groupItem;
        }

        return null;
    }

    void UpdateFootprintModels()
    {
        foreach (var kvp in modelsDictionary.ToList())
        {
            var model = kvp.Value;
            var lastRead = kvp.Value.LastAccessed;
            var project = GetCurrentProjectInfo();
            var modelDoc = _objectFinder.FindObject<ModelDocument>(project, model.Library, model.Id, lastRead);

            if (modelDoc != null)
            {
                var groupItem = kvp.Key;
                groupItem.Items.Clear();
                groupItem.Items.AddRange(modelDoc.Items.Select(item => (BaseMeshItem)item.CreateDesignerItem()));
                foreach (var item in groupItem.Items)
                    item.ParentObject = groupItem;

                modelsDictionary[groupItem] = modelDoc;
            }
        }
    }

    void RefreshFootprintPreviewModels()
    {
        if (boardPreview3DViewModel != null)
            boardPreview3DViewModel.Items = modelsDictionary.Keys.Cast<ISelectableItem>().ToList();
    }

    void AllignModelWithPads()
    {
        var padItems = Items.OfType<IPadCanvasItem>().ToList();
        if (padItems.Count == 0)
            return;

        var groupItem = modelsDictionary.Keys.FirstOrDefault();
        if (groupItem == null)
            return;

        var rect = new XRect();
        var xOffset = 1000.0;
        var yOffset = 1000.0;
        foreach (var pad in padItems)
        {
            var itemRect = new XRect(pad.X, pad.Y, pad.Width, pad.Height);
            if (xOffset > itemRect.X)
                xOffset = itemRect.X;
            if (yOffset > itemRect.Y)
                yOffset = itemRect.Y;

            rect.Union(itemRect);
        }
        if (rect.IsEmpty)
            return;
        rect = new XRect(xOffset, yOffset, rect.Width - xOffset, rect.Height - yOffset);
        groupItem.X = rect.X + rect.Width / 2;
        groupItem.Y = rect.Y + rect.Height / 2;

        if (padItems.Count < 2)
        {
            return;
        }

        var pad1 = padItems.FirstOrDefault(p => p.Number == "1");
        var pad3D1 = groupItem.Items.FirstOrDefault(p => p.PadNumber == 1);
        var pad2 = padItems.FirstOrDefault(p => p.Number == "2");
        var pad3D2 = groupItem.Items.FirstOrDefault(p => p.PadNumber == 2);
        if (pad1 != null && pad2 != null && pad3D1 != null && pad3D2 != null)
        {
            try
            {
                var v1 = new XVector(pad2.X - pad1.X, pad2.Y - pad1.Y);
                v1.Normalize();
                var v2 = new XVector(pad3D2.GetPropertyValue<double>("X") - pad3D1.GetPropertyValue<double>("X"),
                                     pad3D2.GetPropertyValue<double>("Y") - pad3D1.GetPropertyValue<double>("Y"));
                v2.Normalize();
                var rotZ = Math.Acos(XVector.Multiply(v1, v2)) * 180 / Math.PI; ;
                //snap to 90 deg
                var gridSize = 90.0d;
                var counts = Math.Truncate(rotZ / gridSize);
                var delta = Math.Abs(rotZ - counts * gridSize);

                if (delta <= (gridSize / 2))
                    rotZ = counts * gridSize;
                else
                    rotZ = (counts + 1) * gridSize;

                groupItem.RotationZ = rotZ;
            }
            catch { }

        }
    }

    public IList<ModelDocument> GetModels()
    {
        return modelsDictionary.Values.ToList();
    }
}
