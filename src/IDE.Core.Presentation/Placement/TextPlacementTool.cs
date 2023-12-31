﻿using IDE.Core.Interfaces;
using IDE.Core.Types.Media;

namespace IDE.Core.Presentation.Placement;

public class TextPlacementTool : PlacementTool<ITextBaseCanvasItem>, ITextPlacementTool
{
    public override void PlacementMouseMove(XPoint mousePosition)
    {
        var mp = CanvasModel.SnapToGrid(mousePosition);

        var item = GetItem();

        switch (PlacementStatus)
        {
            case PlacementStatus.Ready:
                item.X = mp.X;
                item.Y = mp.Y;
                break;
        }
    }

    public override void PlacementMouseUp(XPoint mousePosition)
    {
        var mp = CanvasModel.SnapToGrid(mousePosition);

        var item = GetItem();

        switch (PlacementStatus)
        {
            case PlacementStatus.Ready:
                item.X = mp.X;
                item.Y = mp.Y;
                item.IsPlaced = true;
                CommitPlacement();

                var newItem = (ITextBaseCanvasItem)canvasItem.Clone();

                PlacementStatus = PlacementStatus.Ready;
                canvasItem = newItem;

                SetupCanvasItem();
                CanvasModel.AddItem(canvasItem);
                break;
        }
    }
}
