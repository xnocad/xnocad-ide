﻿using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Presentation.Placement;
using IDE.Core.Types.Media;
using Xunit;
using Moq;
using IDE.Core.ViewModels;

namespace IDE.Core.Presentation.Tests.PlacementTools;

public class BoxMeshItemPlacementToolTests : PlacementToolTest
{
    public BoxMeshItemPlacementToolTests()
    {
        var dispatcherMock = new Mock<IDispatcherHelper>();
        var schMock = new Mock<ISchematicDesigner>();

        _canvasModel = new DrawingViewModel(schMock.Object, dispatcherMock.Object);
        ((CanvasGrid)_canvasModel.CanvasGrid).GridSizeModel.SelectedItem = new Units.MilUnit(50);

        var canvasItemType = typeof(BoxMeshItem);
        placementTool = new BoxMeshItemPlacementTool();// PlacementTool.CreateTool(canvasItemType);
        placementTool.CanvasModel = _canvasModel;
        placementTool.StartPlacement(canvasItemType);
    }
    private readonly IDrawingViewModel _canvasModel;

    [Fact]
    public void PlacementStarted_MouseMoves()
    {

        var mp = new XPoint(10.16, 10.16);

        //act
        MouseMove(mp.X, mp.Y);

        var item = placementTool.CanvasItem as BoxMeshItem;

        //assert
        //item position is the same as mouse position
        Assert.Equal(mp.X, item.X);
        Assert.Equal(mp.Y, item.Y);
        //ensure placement status
        Assert.Equal(PlacementStatus.Ready, placementTool.PlacementStatus);
    }


    [Fact]
    public void Placement_FirstMouseClick()
    {
        var item = placementTool.CanvasItem as BoxMeshItem;

        var mp = new XPoint(10.16, 10.16);
        MouseMove(mp.X, mp.Y);
        MouseClick(mp.X, mp.Y);

        Assert.Equal(PlacementStatus.Ready, placementTool.PlacementStatus);
        Assert.Equal(mp.X, item.X);
        Assert.Equal(mp.Y, item.Y);
        Assert.True(item.IsPlaced);
        //placement finished
        Assert.NotNull(_canvasModel.PlacementTool);

    }
}
