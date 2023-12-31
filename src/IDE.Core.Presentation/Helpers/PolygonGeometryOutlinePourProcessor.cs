﻿using IDE.Core.Interfaces;
using IDE.Core.Common.Geometries;
using static System.Net.Mime.MediaTypeNames;

namespace IDE.Core.Designers;

public class PolygonGeometryOutlinePourProcessor : BaseGeometryOutlinePourProcessor, IPolygonGeometryOutlinePourProcessor
{
    protected override GeometryOutline GetGeometry(ISelectableItem item)
    {
        var thisPoly = item as IPolygonBoardCanvasItem;

        if (thisPoly == null)
            return null;

        return new PolygonGeometryOutline(thisPoly.PolygonPoints);
    }
}
