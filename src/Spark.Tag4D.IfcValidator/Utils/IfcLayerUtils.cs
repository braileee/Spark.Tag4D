using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace Spark.Tag4D.IfcValidator.Utils;

public static class IfcLayerUtils
{
    /// <summary>
    /// Returns a single presentation layer name (IfcPresentationLayerAssignment.Name)
    /// for the given IfcObject, by traversing its geometric representation.
    /// If multiple layers are found, the first one encountered by representation order is returned.
    /// Returns null if no layer assignment is found or if the object is not a product with geometry.
    /// </summary>
    public static string GetSingleLayerName(IIfcObject obj)
    {
        if (obj is not IIfcProduct product)
            return null;

        var rep = product?.Representation;
        if (rep == null) return null;

        // Iterate representations in a deterministic order
        foreach (var shapeRep in rep.Representations)
        {
            // Prefer items as declared in the file
            foreach (var item in shapeRep.Items)
            {
                var layer = TryGetLayerFromItemDeep(item);
                if (!string.IsNullOrWhiteSpace(layer))
                    return layer;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to get a layer name from a representation item by:
    /// 1) Direct LayerAssignments on the item
    /// 2) LayerAssignments on StyledItem(s)
    /// 3) IfcMappedItem -> MappingSource.MappedRepresentation.Items
    /// (Extend here if you need more deep geometry graph traversal)
    /// </summary>
    private static string TryGetLayerFromItemDeep(IIfcRepresentationItem item)
    {
        // 1) Direct layer assignments on the item
        var direct = FirstLayerOnItem(item);
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        // 2) Layers on styled items attached to this representation item
        var styledLayer = FirstLayerOnStyledItems(item);
        if (!string.IsNullOrWhiteSpace(styledLayer))
            return styledLayer;

        // 3) Resolve mapped items: walk to the source representation items
        if (item is IIfcMappedItem mapped)
        {
            var src = mapped.MappingSource;
            var mappedRep = src?.MappedRepresentation;
            if (mappedRep != null)
            {
                foreach (var subItem in mappedRep.Items)
                {
                    var l = TryGetLayerFromItemDeep(subItem);
                    if (!string.IsNullOrWhiteSpace(l))
                        return l;
                }
            }
        }

        // (Optional) You can extend here for boolean ops, CSG, tessellations if needed:
        // e.g., IfcBooleanResult (Operands), IfcFacetedBrep (Faces->Edges->… rarely have layers), etc.

        return null;
    }

    /// <summary>
    /// Returns the first layer assignment name directly on the representation item.
    /// </summary>
    private static string FirstLayerOnItem(IIfcRepresentationItem item)
    {
        // item.LayerAssignments is IEnumerable<IIfcPresentationLayerAssignment>
        foreach (var layer in item.LayerAssignment)
        {
            if (!string.IsNullOrWhiteSpace(layer?.Name))
                return layer.Name;
        }
        return null;
    }

    /// <summary>
    /// Returns the first layer assignment name on any styled item attached to the representation item.
    /// </summary>
    private static string FirstLayerOnStyledItems(IIfcRepresentationItem item)
    {
        var styled = item.StyledByItem;
        if (styled == null) return null;

        foreach (var si in styled)
        {
            // Layers sometimes are attached to StyledItem as well
            foreach (var layer in si.LayerAssignment)
            {
                if (!string.IsNullOrWhiteSpace(layer?.Name))
                    return layer.Name;
            }
        }
        return null;
    }
}