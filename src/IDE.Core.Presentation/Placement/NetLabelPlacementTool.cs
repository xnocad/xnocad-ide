﻿using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Interfaces.Geometries;
using IDE.Core.Storage;
using IDE.Core.Types.Media;
using System.Collections.Generic;
using System.Linq;

namespace IDE.Core.Presentation.Placement
{
    public  class NetLabelPlacementTool : PlacementTool<NetLabelCanvasItem>, INetLabelPlacementTool
    {
        public NetLabelPlacementTool()
        {
            GeometryHelper = ServiceProvider.Resolve<IGeometryOutlineHelper>();
        }

        private readonly IGeometryOutlineHelper GeometryHelper;

        public override void PlacementMouseMove(XPoint mousePosition)
        {
            var mp = CanvasModel.SnapToGrid(mousePosition);

            var item = GetItem();

            switch (PlacementStatus)
            {
                case PlacementStatus.Ready:
                    item.X = mp.X;
                    item.Y = mp.Y - 3;//3mm upper
                    break;
            }
        }

        public override void PlacementMouseUp(XPoint mousePosition)
        {
            var mp = CanvasModel.SnapToGrid(mousePosition);

            var item = GetItem();
            var circle = new CircleCanvasItem { Diameter = 0.5, X = mp.X, Y = mp.Y, BorderWidth = 0.0 };

            switch (PlacementStatus)
            {
                case PlacementStatus.Ready:
                    item.X = mp.X;
                    item.Y = mp.Y - 3;

                    //nets that intersect at this point
                    var netWires = CanvasModel.Items.OfType<NetWireCanvasItem>().ToList();
                    var intersectedNets = new List<NetWireCanvasItem>();
                    foreach (var netWire in netWires)
                    {
                        //var intersection = GetIntersectionWithLinePoint(mp, netWire); //GeometryHelper.GetIntersection(this, netWire);
                        //if (intersection.IsEmpty())
                        //    continue;

                        var intersects = GeometryHelper.Intersects(netWire, circle);
                        if (!intersects)
                            continue;

                        //add a net that wasn't added before
                        if (netWire.Net != null)
                        {
                            var net = intersectedNets.FirstOrDefault(n => n.Net != null && n.Net.Name == netWire.Net.Name);//n.Net.Id == netWire.Net.Id);
                            if (net == null)
                                intersectedNets.Add(netWire);
                        }
                    }

                    if (intersectedNets.Count > 0)
                    {
                        //we can select an item, but for now we take the 1st
                        var netRef = intersectedNets.FirstOrDefault().Net;

                        item.Net = netRef;
                        item.OnPropertyChanged(nameof(item.Net));
                        item.OnPropertyChanged(nameof(item.NetName));
                        item.IsPlaced = true;
                        CommitPlacement();

                        //create another text
                        var newItem = (NetLabelCanvasItem)canvasItem.Clone();

                        PlacementStatus = PlacementStatus.Ready;
                        canvasItem = newItem;

                        CanvasModel.AddItem(canvasItem);
                    }
                    else
                    {
                        return;
                    }
                    break;
            }
        }

    }
}
