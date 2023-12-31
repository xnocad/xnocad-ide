﻿using System.Collections.Generic;
using System.Linq;
using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Interfaces.Geometries;
using IDE.Core.Types.Media;

namespace IDE.Core.Presentation.Placement;

public class JunctionPlacementTool : PlacementTool<JunctionCanvasItem>, IJunctionPlacementTool
{
    public JunctionPlacementTool()
    {
        GeometryHelper = ServiceProvider.Resolve<IGeometryOutlineHelper>();
    }

    private readonly IGeometryOutlineHelper GeometryHelper;
    private List<NetChanges> _netChanges = new List<NetChanges>();

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

                //a junction can be placed on the intersection on at least 2 NetWireSegmentItem
                item.X = mp.X;
                item.Y = mp.Y;

                //if we already have a junction on the same position, exit
                var junction = CanvasModel.Items.OfType<JunctionCanvasItem>()
                                           .Where(j => canvasItem != j && j.X == item.X && j.Y == item.Y)
                                           .FirstOrDefault();
                if (junction != null)
                    return;

                //nets that intersect at this point
                var netWires = CanvasModel.Items.OfType<NetWireCanvasItem>().ToList();
                var intersectedNets = new List<NetWireCanvasItem>();
                foreach (var netWire in netWires)
                {
                    if (!GeometryHelper.Intersects(item, netWire))
                        continue;

                    //add a net that wasn't added before
                    if (netWire.Net != null)
                    {
                        var net = intersectedNets.FirstOrDefault(n => n.Net != null && n.Net.Name == netWire.Net.Name);//n.Net.Id == netWire.Net.Id);
                        if (net == null)
                            intersectedNets.Add(netWire);
                    }
                }

                var netNames = intersectedNets.Select(n => n.Net.Name).ToArray();
                if (intersectedNets.Count >= 1)
                {
                    SchematicNet netRef = null;
                    if (intersectedNets.Count > 1)
                    {
                        var candidateNets = intersectedNets.Select(n => n.Net).OrderBy(n => n.Name).ToList();

                        var itemPickerDialog = ServiceProvider.Resolve<IItemPickerDialog>();
                        itemPickerDialog.LoadData(candidateNets);
                        var res = itemPickerDialog.ShowDialog();

                        if (res.GetValueOrDefault() == false)
                            return;
                        netRef = itemPickerDialog.SelectedItem as SchematicNet;
                    }
                    else
                    {
                        netRef = intersectedNets.FirstOrDefault().Net;
                    }
                    if (netRef == null)
                        return;

                    foreach (var net in intersectedNets)
                    {
                        var netElements = CanvasModel.Items.OfType<NetSegmentCanvasItem>().ToList();

                        //all old elements under the old net should be under the new net
                        var oldElements = netElements.Where(n => n.Net != null && n.Net.Name == net.Net.Name)//n.Net.Id == net.Net.Id)
                                                    .ToList();

                        _netChanges.Add(new NetChanges
                        {
                            OldNet = net.Net,
                            NewNet = netRef,
                            OldNetItems = oldElements
                        });

                        foreach (var oldElem in oldElements)
                        {
                            oldElem.Net = netRef;
                        }

                        
                    }
                    item.Net = netRef;
                    item.IsPlaced = true;

                    CommitPlacement();

                    //unhighlight/highlight
                    CanvasModel.ClearSelectedItems();
                    item.HighlightOwnedNet(true);

                    //create another junction
                    var newItem = (JunctionCanvasItem)canvasItem.Clone();

                    PlacementStatus = PlacementStatus.Ready;
                    canvasItem = newItem;

                    CanvasModel.AddItem(canvasItem);
                }

                else
                    return;

                break;


        }
    }

    protected override void RegisterUndoActionExecuted()
    {
        var item = GetItem();
        var itemNet = item.Net;
        var currentChanges = _netChanges.ToList();

        Func<object, object> undo = (i) =>
        {
            CanvasModel.RemoveItem(item);
            foreach (var netChange in currentChanges)
            {
                netChange.OldNetItems.ForEach(n => n.Net = netChange.OldNet);
            }
            return item;
        };
        Func<object, object> redo = (i) =>
        {
            item.Net = itemNet;
            CanvasModel.AddItem(item);
            foreach (var netChange in currentChanges)
            {
                netChange.OldNetItems.ForEach(n => n.Net = netChange.NewNet);
            }
            return item;
        };

        CanvasModel.RegisterUndoActionExecuted(undo, redo, item);
    }
}

internal class NetChanges
{
    public SchematicNet OldNet { get; set; }
    public SchematicNet NewNet { get; set; }
    public List<NetSegmentCanvasItem> OldNetItems { get; set; }
}
