﻿using IDE.Core.Presentation.Placement;
using IDE.Core.Types.Media;
using System;
using System.Collections.Generic;
using System.Text;
using IDE.Core.Collision;
using IDE.Core.Interfaces;
using Moq;
using IDE.Core.ViewModels;
using IDE.Core.Designers;

namespace IDE.Core.Presentation.Tests.PlacementTools;

public abstract class PlacementToolTest
{
    static PlacementToolTest()
    {
        var meshHelperMock = new Mock<IMeshHelper>();

        var debounceMock = new Mock<IDebounceDispatcher>();

        var dispatcherMock = new Mock<IDispatcherHelper>();
        dispatcherMock.Setup(x => x.RunOnDispatcher(It.IsAny<Action>()))
                       .Callback((Action action) =>
                       {
                           action();
                       });

        var toolRegistryMock = new Mock<IToolWindowRegistry>();
        toolRegistryMock.SetupGet(x => x.Tools)
            .Returns(new List<IToolWindow>());

        ServiceProvider.RegisterResolver(t =>
        {
            if (t == typeof(IDebounceDispatcher))
                return debounceMock.Object;

            if (t == typeof(IDispatcherHelper))
                return dispatcherMock.Object;

            if (t == typeof(IToolWindowRegistry))
                return toolRegistryMock.Object;

            if (t == typeof(IPrimitiveToCanvasItemMapper))
                return new PrimitiveToCanvasItemMapper();

            if (t == typeof(IMeshHelper))
                return meshHelperMock.Object;

            throw new NotImplementedException();
        });
    }
    protected IPlacementTool placementTool;

    protected ICanvasDesignerFileViewModel CreateCanvasModel()
    {
        var dispatcherMock = new Mock<IDispatcherHelper>();
        var drawingDebounceMock = new Mock<IDebounceDispatcher>();
        var selectionDebounceMock = new Mock<IDebounceDispatcher>();
        var dirtyMarkerMock = new Mock<IDirtyMarkerTypePropertiesMapper>();
        var placementFactoryMock = new Mock<IPlacementToolFactory>();

        var canvasModel = new CanvasViewModelMock(dispatcherMock.Object, drawingDebounceMock.Object,
            selectionDebounceMock.Object, dirtyMarkerMock.Object, placementFactoryMock.Object);

        return canvasModel;
    }

    // Simulates mouse move
    protected void MouseMove(double x, double y)
    {
        placementTool.PlacementMouseMove(new XPoint(x, y));
    }

    protected void MouseClick(double x, double y)
    {
        placementTool.PlacementMouseUp(new XPoint(x, y));
    }
}
