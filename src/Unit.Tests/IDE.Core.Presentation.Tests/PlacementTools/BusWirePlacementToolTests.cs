﻿using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Presentation.Placement;
using IDE.Core.Types.Media;
using Xunit;
using IDE.Core.ViewModels;
using Moq;


namespace IDE.Core.Presentation.Tests.PlacementTools
{
    public class BusWirePlacementToolTests : PlacementToolTest
    {
        //todo: hit tests scenarios
        public BusWirePlacementToolTests()
        {
            //var dispatcherMock = new Mock<IDispatcherHelper>();
            //dispatcherMock.Setup(x => x.RunOnDispatcher(It.IsAny<Action>()))
            //               .Callback((Action action) =>
            //               {
            //                   action();
            //               });

            //var schMock = new Mock<ISchematicDesigner>();
            //schMock.SetupGet(x => x.BusManager)
            //        .Returns(new SchematicBusManager());//mock net manager?

            _canvasModel = CreateCanvasModel();
            _canvasModel.CanvasGrid.SetUnit(new Units.MilUnit(50));

            var canvasItemType = typeof(BusWireCanvasItem);
            placementTool = new BusWirePlacementTool();//PlacementTool.CreateTool(canvasItemType);
            placementTool.CanvasModel = _canvasModel;
            placementTool.StartPlacement(canvasItemType);

            BusWirePlacementTool.SetPlacementMode(NetPlacementMode.Single);
        }

        private readonly ICanvasDesignerFileViewModel _canvasModel;

        BusWirePlacementTool BusWirePlacementTool => (BusWirePlacementTool)placementTool;

        [Theory]
        [InlineData(NetPlacementMode.Single)]
        [InlineData(NetPlacementMode.HorizontalVertical)]
        [InlineData(NetPlacementMode.VerticalHorizontal)]
        public void PlacementStarted_MouseMoves(NetPlacementMode placementMode)
        {
            //arrange
            BusWirePlacementTool.SetPlacementMode(placementMode);

            var mp = new XPoint(10.16, 10.16);

            //act
            MouseMove(mp.X, mp.Y);

            var wire = placementTool.CanvasItem as BusWireCanvasItem;

            //assert
            //all points are same as mouse point
            for (int i = 0; i < wire.Points.Count; i++)
            {
                Assert.Equal(mp, wire.Points[i]);
            }
            //ensure placement status
            Assert.Equal(PlacementStatus.Ready, placementTool.PlacementStatus);
        }

        //1st mouse click

        [Fact]
        public void Placement_FirstMouseClickNoHits()
        {
            var mp = new XPoint(10.16, 10.16);
            MouseMove(mp.X, mp.Y);
            MouseClick(mp.X, mp.Y);

            var wire = placementTool.CanvasItem as BusWireCanvasItem;
            Assert.Equal(PlacementStatus.Started, placementTool.PlacementStatus);
            Assert.Null(wire.Bus);
            Assert.Equal(mp, wire.StartPoint);
        }

        //1st mouse click, second mouse move
        [Theory]
        [InlineData(NetPlacementMode.Single)]
        [InlineData(NetPlacementMode.HorizontalVertical)]
        [InlineData(NetPlacementMode.VerticalHorizontal)]
        public void Placement_MouseClickMouseMoveNoHits(NetPlacementMode placementMode)
        {
            BusWirePlacementTool.SetPlacementMode(placementMode);

            var sp = new XPoint(10.16, 10.16);
            var ep = new XPoint(20.32, 20.32);

            MouseMove(sp.X, sp.Y);
            MouseClick(sp.X, sp.Y);

            MouseMove(ep.X, ep.Y);


            var wire = placementTool.CanvasItem as BusWireCanvasItem;
            Assert.Equal(PlacementStatus.Started, placementTool.PlacementStatus);
            Assert.Null(wire.Bus);
            Assert.Equal(sp, wire.StartPoint);
            Assert.Equal(ep, wire.EndPoint);

            //assert middle point
            if (placementMode != NetPlacementMode.Single)
            {
                var mp = placementMode == NetPlacementMode.HorizontalVertical ? new XPoint(ep.X, sp.Y) : new XPoint(sp.X, ep.Y);

                Assert.Equal(mp, wire.Points[1]);
            }
        }

        #region Second mouse click
        //second mouse click
        [Fact]
        public void Placement_MouseClickMouseMoveMouseClickNoHits()
        {
            var sp = new XPoint(10.16, 10.16);
            var ep = new XPoint(20.32, 20.32);

            MouseMove(sp.X, sp.Y);
            MouseClick(sp.X, sp.Y);

            MouseMove(ep.X, ep.Y);
            MouseClick(ep.X, ep.Y);

            var wire = BusWirePlacementTool.CommitedPolyline;

            //assert
            Assert.Equal(PlacementStatus.Started, placementTool.PlacementStatus);

            Assert.NotNull(wire.Bus);
            Assert.False(wire.Bus.IsNamed());
            Assert.True(wire.IsPlaced);

            Assert.Equal(sp, wire.StartPoint);
            Assert.Equal(ep, wire.EndPoint);
        }

        #endregion
    }

}
