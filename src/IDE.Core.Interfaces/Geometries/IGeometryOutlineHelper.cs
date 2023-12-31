﻿using IDE.Core.Errors;
using IDE.Core.Types.Media;

namespace IDE.Core.Interfaces.Geometries;

public interface IGeometryOutlineHelper
{

    /// <summary>
    /// Gets Geometry in the local space
    /// </summary>
    /// <param name="item"></param>
    /// <param name="applyTransform">true to apply world transform of the canvasItem to the local geometry</param>
    /// <param name="clearance">if clearance is positive geometry will be inflate by the offset
    /// <para>if clearance is negative geometry will be constricted with an offset of this value</para>
    /// </param>
    /// <returns></returns>
    IGeometryOutline GetGeometry(ICanvasItem item, bool applyTransform = false, double clearance = 0.0d);
    
    IGeometryOutline GetShapeGeometry(IShape item);

    bool Intersects(ICanvasItem item1, ICanvasItem item2);

    bool ItemIntersectsRectangle(ICanvasItem item, XRect rect);

    CanvasLocation CheckClearance(ICanvasItem item1, ICanvasItem item2, double clearance);
}
