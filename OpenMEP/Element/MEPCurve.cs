﻿using System.Collections;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Dynamo.Graph.Nodes;
using OpenMEP.Helpers;
using Revit.GeometryConversion;
using RevitServices.Transactions;

namespace OpenMEP.Element;

public class MEPCurve
{
   private MEPCurve()
   {
      
   }
    /// <summary>
    /// break mep curve at point
    /// </summary>
    /// <param name="mepCurve">A curve object for duct or pipe blend elements.</param>
    /// <param name="point">location to break on mep curve</param>
    /// <returns name="element">new element break from mep curve</returns>
    [NodeCategory("Action")]
    public static global::Revit.Elements.Element? BreakCurve(global::Revit.Elements.Element mepCurve, Autodesk.DesignScript.Geometry.Point point)
    {
        if (mepCurve == null) throw new ArgumentNullException(nameof(mepCurve));
        if (point == null) throw new ArgumentNullException(nameof(point));
        Autodesk.Revit.DB.Element internalElement = mepCurve.InternalElement;
        TransactionManager.Instance.ForceCloseTransaction();
        TransactionManager.Instance.EnsureInTransaction(internalElement.Document);
        if (internalElement is Autodesk.Revit.DB.MEPCurve mCurve)
        {
            XYZ location = point.ToXyz();
            ElementId id = BreakCurve(mCurve,location);
            if (id != ElementId.InvalidElementId)
            {
                return mCurve.Document.GetElement(id).ToDynamoType();
            }
        }
        TransactionManager.Instance.TransactionTaskDone();
        return null;
    }
   /// <summary>
   /// Break MEP curve at given point
   /// </summary>
   /// <param name="mepCurve">Autodesk.Revit.DB.MEPCurve</param>
   /// <param name="ptBreak">point to break</param>
   /// <returns></returns>
   private static ElementId BreakCurve(Autodesk.Revit.DB.MEPCurve? mepCurve, XYZ? ptBreak)
   {
       if (mepCurve is Pipe || mepCurve is FlexPipe)
       {
           return PlumbingUtils.BreakCurve(mepCurve.Document, mepCurve.Id, ptBreak);
       }
       else if (mepCurve is Duct || mepCurve is FlexDuct)
       {
           return MechanicalUtils.BreakCurve(mepCurve.Document, mepCurve.Id, ptBreak);
       }
       else if (mepCurve is Conduit || mepCurve is CableTray)
       {
           ElementId elementId = BreakConduitCableTray(mepCurve.Document, mepCurve.Id, ptBreak);
           return elementId;
       }
       return ElementId.InvalidElementId;
   } 
   private static ElementId BreakConduitCableTray(Autodesk.Revit.DB.Document doc, ElementId conduitId, XYZ breakPoint)
   {
       var conduit = doc.GetElement(conduitId);
       //copy mepCurveToOptimize as newPipe and move to brkPoint
       var location = conduit.Location as LocationCurve;
       var start = location.Curve.GetEndPoint(0);
       var end = location.Curve.GetEndPoint(1);
       var copiedEls = ElementTransformUtils.CopyElement(doc, conduit.Id, breakPoint - start);
       var newId = copiedEls.First();

       //shorten mepCurveToOptimize and newPipe (adjust endpoints)
       AdjustMepCurve(conduit, start, breakPoint);
       AdjustMepCurve(doc.GetElement(newId), breakPoint, end);

       return newId;
   }

   private static void AdjustMepCurve(Autodesk.Revit.DB.Element mepCurve, XYZ p1, XYZ p2)
   {
       // if (disconnect)
       //     Disconnect(mepCurve);

       var location = mepCurve.Location as LocationCurve;

       location.Curve = Line.CreateBound(p1, p2);
   }
   /// <summary>
    /// create a union fitting from two mepcurve
    /// </summary>
    /// <param name="mepCurve1">A curve object for duct or pipe blend first elements.</param>
    /// <param name="mepCurve2">A curve object for duct or pipe blend second elements.</param>
    /// <returns name="family instance">union fitting</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewUnionFitting(global::Revit.Elements.Element mepCurve1,
        global::Revit.Elements.Element mepCurve2)
    {
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve1.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Connector? c1 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve1, mepCurve2);
        Connector? c2 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve2,mepCurve1);
        Autodesk.Revit.DB.FamilyInstance newUnionFitting = doc.Create.NewUnionFitting(c2, c1);
        TransactionManager.Instance.TransactionTaskDone();
        if (newUnionFitting == null)
        {
            List<Connector?> connectors = OpenMEP.ConnectorManager.Connector.GetConnectors(mepCurve1);
            Connector? c11 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(c1,connectors);
            ConnectorSet connectorSet = c11!.AllRefs;
            IEnumerator enumerator = connectorSet.GetEnumerator();
            while (enumerator.MoveNext())
            {
                Connector? current = enumerator.Current as Autodesk.Revit.DB.Connector;
                if (current == null) continue;
                if (current.Owner.Id.IntegerValue == mepCurve1.Id) continue;
                if (current.Owner is Autodesk.Revit.DB.Plumbing.PipingSystem) continue;
                global::Revit.Elements.Element? dynamoType = current.Owner.ToDynamoType();
                return dynamoType;
            }
        }
        return newUnionFitting.ToDynamoType();
    }

    /// <summary>
    /// create a elbow fitting from two mep curve
    /// </summary>
    /// <param name="mepCurve1">A curve object for duct or pipe blend first elements.</param>
    /// <param name="mepCurve2">A curve object for duct or pipe second elements.</param>
    /// <returns name="family instance">elbow fitting</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewElbowFitting(global::Revit.Elements.Element mepCurve1,
        global::Revit.Elements.Element mepCurve2)
    {
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve1.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Connector? c1 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve1,mepCurve2);
        Connector? c2 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve2,mepCurve1);
        Autodesk.Revit.DB.FamilyInstance newElbowFitting = doc.Create.NewElbowFitting(c2, c1);
        TransactionManager.Instance.TransactionTaskDone();
        if (newElbowFitting == null) return null;
        return newElbowFitting.ToDynamoType();
    }

    /// <summary>
    /// Add a new family instance of a tee fitting into the Autodesk Revit document,
    /// using three mep curves.
    /// </summary>
    /// <param name="mepCurve1">A curve object for duct or pipe blend first elements.</param>
    /// <param name="mepCurve2">A curve object for duct or pipe blend second elements.</param>
    /// <param name="mepCurve3">A curve object for duct or pipe blend three elements.</param>
    /// <returns name="familyinstance">new tee fitting</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewTeeFitting(global::Revit.Elements.Element mepCurve1,global::Revit.Elements.Element mepCurve2,global::Revit.Elements.Element mepCurve3)
    {
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve1.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Connector? c1 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve1,mepCurve2);
        Connector? c2 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve2,mepCurve1);
        Connector? c3 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve3,mepCurve1);
        Autodesk.Revit.DB.FamilyInstance newTeeFitting = doc.Create.NewTeeFitting(c2, c1,c3);
        TransactionManager.Instance.TransactionTaskDone();
        if (newTeeFitting == null) return null;
        return newTeeFitting.ToDynamoType();
    }

    /// <summary>
    /// Add a new family instance of a tee fitting into the Autodesk Revit document,
    /// using four mep curves.
    /// </summary>
    /// <param name="mepCurve1">the first mepCurve(Pipe/Duct/CableTray)</param>
    /// <param name="mepCurve2">the second mepCurve(Pipe/Duct/CableTray)</param>
    /// <param name="mepCurve3">the three mepCurve(Pipe/Duct/CableTray)</param>
    /// <param name="mepCurve4">the four mepCurve(Pipe/Duct/CableTray)</param>
    /// <returns name="familyinstance">new cross fitting</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewCrossFitting(global::Revit.Elements.Element mepCurve1,global::Revit.Elements.Element mepCurve2,global::Revit.Elements.Element mepCurve3,global::Revit.Elements.Element mepCurve4)
    {
        
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve1.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Connector? c1 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve1,mepCurve2);
        Connector? c2 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve2,mepCurve1);
        Connector? c3 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve3,mepCurve1);
        Connector? c4 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve4,mepCurve3);
        Autodesk.Revit.DB.FamilyInstance newCrossFitting = doc.Create.NewCrossFitting(c2, c1,c3,c4);
        TransactionManager.Instance.TransactionTaskDone();
        if (newCrossFitting == null) return null;
        return newCrossFitting.ToDynamoType();
    }
    
    /// <summary>
    /// Add a new family instance of an transition fitting into the Autodesk Revit document,
    /// using two connectors.
    /// </summary>
    /// <param name="mepCurve1">the first mepCurve(Pipe/Duct/CableTray)</param>
    /// <param name="mepCurve2">the second mepCurve(Pipe/Duct/CableTray)</param>
    /// <returns name="family instance">new transition</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewTransitionFitting(global::Revit.Elements.Element mepCurve1,global::Revit.Elements.Element mepCurve2)
    {
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve1.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Connector? c1 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve1,mepCurve2);
        Connector? c2 = OpenMEP.ConnectorManager.Connector.GetConnectorClosest(mepCurve2,mepCurve1);
        Autodesk.Revit.DB.FamilyInstance newTransitionFitting = doc.Create.NewTransitionFitting(c2, c1);
        TransactionManager.Instance.TransactionTaskDone();
        if (newTransitionFitting == null) return null;
        return newTransitionFitting.ToDynamoType();
    }
    
    /// <summary>
    /// Add a new family instance of an takeoff fitting into the Autodesk Revit document,
    /// using one connector and one MEP curve.
    /// </summary>
    /// <param name="connector">connector to be connector</param>
    /// <param name="mepCurve">mepCurve connect to create Takeoff</param>
    /// <returns name="familyinstance">new takeoff fitting</returns>
    [NodeCategory("Create")]
    public static global::Revit.Elements.Element? NewTakeoffFitting(Autodesk.Revit.DB.Connector connector,global::Revit.Elements.Element mepCurve)
    {
        TransactionManager.Instance.ForceCloseTransaction();
        Autodesk.Revit.DB.Document doc = mepCurve.InternalElement.Document;
        TransactionManager.Instance.EnsureInTransaction(doc);
        Autodesk.Revit.DB.MEPCurve? internalElement = mepCurve.InternalElement as Autodesk.Revit.DB.MEPCurve;
        Autodesk.Revit.DB.FamilyInstance newTakeoffFitting = doc.Create.NewTakeoffFitting(connector, internalElement);
        TransactionManager.Instance.TransactionTaskDone();
        if (newTakeoffFitting == null) return null;
        return newTakeoffFitting.ToDynamoType();
    }
}