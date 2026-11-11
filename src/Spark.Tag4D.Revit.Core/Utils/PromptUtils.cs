using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace Spark.Tag4D.Revit.Core.Utils
{
    public class PromptUtils<T>
    {
        public static List<T> GetObjects(ExternalCommandData commandData, string promptMessage, Type type)
        {
            UIApplication uiapp = commandData.Application;
            return GetObjects(uiapp, promptMessage, type);
        }

        public static List<T> GetObjects(UIApplication uiApp, string promptMessage, Type type)
        {
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            IList<Reference> selectedObjs = null;

            try
            {
                selectedObjs = uidoc.Selection.PickObjects(ObjectType.Element, promptMessage);
            }
            catch (Exception)
            {
                return new List<T>();
            }
            var elements = new List<T>();
            foreach (var selectedObj in selectedObjs)
            {
                var elem = doc.GetElement(selectedObj.ElementId);
                if (type == elem.GetType())
                {
                    elements.Add((T)(object)elem);
                }
            }
            return elements;
        }

        public static List<T> GetObjects(ExternalCommandData commandData, string promptMessage)
        {
            UIApplication uiapp = commandData.Application;
            return GetObjects(uiapp, promptMessage);
        }

        public static List<T> GetObjects(UIApplication uiApp, string promptMessage)
        {
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var elements = new List<T>();

            IList<Reference> selectedObjs = new List<Reference>();

            try
            {
                selectedObjs = uidoc.Selection.PickObjects(ObjectType.Element, promptMessage);
            }
            catch (Exception)
            {
                return elements;
            }

            if (selectedObjs == null)
                return null;
            foreach (var selectedObj in selectedObjs)
            {
                var elem = doc.GetElement(selectedObj.ElementId);
                if (typeof(T).Equals(elem.GetType()) ||
                    typeof(T).IsAssignableFrom(elem.GetType()))
                {
                    elements.Add((T)(object)elem);
                }
            }
            return elements;
        }


        public static List<XYZ> GetPoints(ExternalCommandData commandData, string promptMessage)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            List<XYZ> points = new List<XYZ>();

            while (true)
            {
                XYZ pickedPoint = null;
                try
                {
                    pickedPoint = uidoc.Selection.PickPoint(ObjectSnapTypes.None, promptMessage);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                {
                    break;
                }
                points.Add(pickedPoint);
            }

            return points;
        }


        public static List<XYZ> GetPoints(ExternalCommandData commandData, string promptMessage, ObjectSnapTypes objectSnapTypes)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            List<XYZ> points = new List<XYZ>();

            while (true)
            {
                XYZ pickedPoint = null;
                try
                {
                    pickedPoint = uidoc.Selection.PickPoint(objectSnapTypes, promptMessage);
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
                {
                    break;
                }
                points.Add(pickedPoint);
            }

            return points;
        }

        public static XYZ GetPoint(ExternalCommandData commandData, string promptMessage)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            XYZ pickedPoint = null;
            try
            {
                pickedPoint = uidoc.Selection.PickPoint(ObjectSnapTypes.None, promptMessage);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
            }

            return pickedPoint;
        }


        public static XYZ GetPoint(ExternalCommandData commandData, string promptMessage, ObjectSnapTypes objectSnapTypes)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;

            XYZ pickedPoint = null;
            try
            {
                pickedPoint = uidoc.Selection.PickPoint(objectSnapTypes, promptMessage);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException ex)
            {
            }

            return pickedPoint;
        }

        public static List<T> GetSelectedObjects(ExternalCommandData commandData)
        {
            return GetSelectedObjects(commandData.Application);
        }

        public static List<T> GetSelectedObjects(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var selectedIds = uidoc.Selection.GetElementIds();
            var elements = new List<T>();

            foreach (var selectedId in selectedIds)
            {
                var element = (T)(object)doc.GetElement(selectedId);
                elements.Add(element);
            }

            return elements;
        }

        public static T GetObject(ExternalCommandData commandData, string promptMessage, Type type)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Reference selectedObj = null;
            T elem;
            try
            {
                selectedObj = uidoc.Selection.PickObject(ObjectType.Element, promptMessage);
            }
            catch (Exception)
            {
            }
            elem = (T)(object)doc.GetElement(selectedObj.ElementId);
            if (type.Equals(elem.GetType()))
            {
                return elem;
            }
            return elem;
        }

        public static T GetObject(ExternalCommandData commandData, string promptMessage)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            Reference selectedObj = null;
            T elem;
            try
            {
                selectedObj = uidoc.Selection.PickObject(ObjectType.Element, promptMessage);
            }

            catch (Exception)
            {
            }
            if (selectedObj == null)
                return default;

            if (!(doc.GetElement(selectedObj.ElementId) is T))
                return default;

            elem = (T)(object)doc.GetElement(selectedObj.ElementId);
            return elem;
        }
    }
}
