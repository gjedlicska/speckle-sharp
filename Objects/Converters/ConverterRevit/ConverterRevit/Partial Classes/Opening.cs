﻿using Autodesk.Revit.DB;
using Objects.Geometry;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

using Level = Objects.BuiltElements.Level;
using Opening = Objects.BuiltElements.Opening;
using Point = Objects.Geometry.Point;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public DB.Opening OpeningToNative(IOpening speckleOpening)
    {
      var baseCurves = CurveToNative(speckleOpening.outline);

      var (docObj, stateObj) = GetExistingElementByApplicationId(((Opening)speckleOpening).applicationId, ((Opening)speckleOpening).speckle_type);
      if (docObj != null)
        Doc.Delete(docObj.Id);

      DB.Opening revitOpening = null;

      switch (speckleOpening)
      {
        case RevitWallOpening rwo:
          {
            var points = (rwo.outline as Polyline).points.Select(x => PointToNative(x)).ToList();
            var host = Doc.GetElement(new ElementId(rwo.revitHostId));
            revitOpening = Doc.Create.NewOpening(host as Wall, points[0], points[2]);
            break;
          }

        case RevitVerticalOpening rvo:
          {
            var host = Doc.GetElement(new ElementId(rvo.revitHostId));
            revitOpening = Doc.Create.NewOpening(host, baseCurves, true);
            break;
          }

        case RevitShaft rs:
          {
            var bottomLevel = GetLevelByName(rs.level);
            var topLevel = !string.IsNullOrEmpty(rs.topLevel) ? GetLevelByName(rs.topLevel) : null;
            revitOpening = Doc.Create.NewOpening(bottomLevel, topLevel, baseCurves);
            break;
          }

        default:
          ConversionErrors.Add(new Error("Cannot create Opening", "Opening type not supported"));
          throw new Exception("Opening type not supported");
      }


      if (speckleOpening is RevitOpening ro)
        SetElementParams(revitOpening, ro);

      return revitOpening;
    }

    public IOpening OpeningToSpeckle(DB.Opening revitOpening)
    {
      //REVIT PARAMS > SPECKLE PROPS
      var baseLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT);
      var topLevelParam = revitOpening.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);

      RevitOpening speckleOpening = null;

      if (revitOpening.IsRectBoundary)
      {
        speckleOpening = new RevitWallOpening();

        var poly = new Polyline();
        poly.value = new List<double>();

        //2 points: bottom left and top right
        var btmLeft = PointToSpeckle(revitOpening.BoundaryRect[0]);
        var topRight = PointToSpeckle(revitOpening.BoundaryRect[1]);
        poly.value.AddRange(btmLeft.value);
        poly.value.AddRange(new Point(btmLeft.value[0], btmLeft.value[1], topRight.value[2]).value);
        poly.value.AddRange(topRight.value);
        poly.value.AddRange(new Point(topRight.value[0], topRight.value[1], btmLeft.value[2]).value);
        poly.value.AddRange(btmLeft.value);
        speckleOpening.outline = poly;
      }
      else
      {
        //host id is actually set in NestHostedObjects
        if (revitOpening.Host != null)
          speckleOpening = new RevitVerticalOpening();
        else
        {
          speckleOpening = new RevitShaft();
          if (topLevelParam != null)
            ((RevitShaft)speckleOpening).topLevel = ConvertAndCacheLevel(topLevelParam);
        }


        var poly = new Polycurve();
        poly.segments = new List<ICurve>();
        foreach (DB.Curve curve in revitOpening.BoundaryCurves)
        {
          if (curve != null)
            poly.segments.Add(CurveToSpeckle(curve));
        }
        speckleOpening.outline = poly;
      }

      if (baseLevelParam != null)
        speckleOpening.level = ConvertAndCacheLevel(baseLevelParam);

      speckleOpening.type = revitOpening.Name;

      AddCommonRevitProps(speckleOpening, revitOpening);

      return speckleOpening;
    }
  }
}