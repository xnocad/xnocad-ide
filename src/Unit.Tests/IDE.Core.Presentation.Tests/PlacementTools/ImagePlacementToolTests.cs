﻿using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Presentation.Placement;
using IDE.Core.Types.Media;
using Xunit;
using IDE.Core.ViewModels;
using Moq;


namespace IDE.Core.Presentation.Tests.PlacementTools;

public class ImagePlacementToolTests : PlacementToolTest
{
    public ImagePlacementToolTests()
    {
        var dispatcherMock = new Mock<IDispatcherHelper>();
        var schMock = new Mock<ISchematicDesigner>();

        _canvasModel = CreateCanvasModel();
        _canvasModel.CanvasGrid.SetUnit(new Units.MilUnit(50));

        var canvasItemType = typeof(ImageCanvasItem);
        placementTool = new ImagePlacementTool();
        placementTool.CanvasModel = _canvasModel;
        placementTool.StartPlacement(canvasItemType);
    }

    private readonly ICanvasDesignerFileViewModel _canvasModel;


    [Fact]
    public void PlacementReady_MouseMoves()
    {

        var mp = new XPoint(10.16, 10.16);

        placementTool.PlacementStatus = PlacementStatus.Ready;
        //act
        MouseMove(mp.X, mp.Y);

        var item = placementTool.CanvasItem as IImageCanvasItem;

        //assert
        //item position is the same as mouse position
        Assert.Equal(mp.X, item.X);
        Assert.Equal(mp.Y, item.Y);
        //ensure placement status
        Assert.Equal(PlacementStatus.Ready, placementTool.PlacementStatus);
    }

    [Fact]
    public void PlacementStarted_MouseMoves()
    {
        var item = placementTool.CanvasItem as IImageCanvasItem;

        item.X = 0;
        item.Y = 0;

        var mp = new XPoint(10.16, 10.16);

        placementTool.PlacementStatus = PlacementStatus.Started;
        //act
        MouseMove(mp.X, mp.Y);


        //assert
        Assert.Equal(0, item.X);
        Assert.Equal(0, item.Y);
        var expectedWidth = (mp.X - item.X);
        var expectedHeight = (mp.Y - item.Y);
        Assert.Equal(expectedWidth, item.Width);
        Assert.Equal(expectedHeight, item.Height);
        //ensure placement status
        Assert.Equal(PlacementStatus.Started, placementTool.PlacementStatus);
    }


    [Fact]
    public void PlacementReady_MouseClick()
    {

        var mp = new XPoint(10.16, 10.16);

        placementTool.PlacementStatus = PlacementStatus.Ready;
        //act
        MouseClick(mp.X, mp.Y);

        var item = placementTool.CanvasItem as IImageCanvasItem;

        //assert
        //item position is the same as mouse position
        Assert.Equal(mp.X, item.X);
        Assert.Equal(mp.Y, item.Y);
        //ensure placement status
        Assert.Equal(PlacementStatus.Started, placementTool.PlacementStatus);
    }

    [Fact]
    public void PlacementStarted_MouseClick()
    {
        var item = placementTool.CanvasItem as IImageCanvasItem;

        item.X = 0;
        item.Y = 0;

        var mp = new XPoint(10.16, 10.16);

        placementTool.PlacementStatus = PlacementStatus.Started;
        //act
        MouseClick(mp.X, mp.Y);


        //assert
        Assert.Equal(0, item.X);
        Assert.Equal(0, item.Y);
        var expectedWidth = (mp.X - item.X);
        var expectedHeight = (mp.Y - item.Y);
        Assert.Equal(expectedWidth, item.Width);
        Assert.Equal(expectedHeight, item.Height);
        Assert.True(item.IsPlaced);
        //ensure placement status
        Assert.Equal(PlacementStatus.Ready, placementTool.PlacementStatus);

        //we have a cloned item
        var clonedItem = placementTool.CanvasItem as IImageCanvasItem;
        Assert.Equal(clonedItem.X, item.X);
        Assert.Equal(clonedItem.Y, item.Y);
        Assert.Equal(clonedItem.Width, item.Width);
        Assert.Equal(clonedItem.Height, item.Height);
        Assert.Equal(clonedItem.BorderWidth, item.BorderWidth);
    }
}
