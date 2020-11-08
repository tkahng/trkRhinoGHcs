
using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

// Non-default includes.

/// Unique namespace, so visual studio won't throw any errors about duplicate definitions.
namespace ns7755f
{
    /// <summary>
    /// This class will be instantiated on demand by the Script component.
    /// </summary>
    public class Script_Instance : GH_ScriptInstance
    {
	    /// This method is added to prevent compiler errors when opening this file in visual studio (code) or rider.
	    public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
        {
            throw new NotImplementedException();
        }

        #region Utility functions
        /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
        /// <param name="text">String to print.</param>
        private void Print(string text) { /* Implementation hidden. */ }
        /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
        /// <param name="format">String format.</param>
        /// <param name="args">Formatting parameters.</param>
        private void Print(string format, params object[] args) { /* Implementation hidden. */ }
        /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
        /// <param name="obj">Object instance to parse.</param>
        private void Reflect(object obj) { /* Implementation hidden. */ }
        /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
        /// <param name="obj">Object instance to parse.</param>
        private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
        #endregion
        #region Members
        /// <summary>Gets the current Rhino document.</summary>
        private readonly RhinoDoc RhinoDocument;
        /// <summary>Gets the Grasshopper document that owns this script.</summary>
        private readonly GH_Document GrasshopperDocument;
        /// <summary>Gets the Grasshopper script component that owns this script.</summary>
        private readonly IGH_Component Component;
        /// <summary>
        /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
        /// Any subsequent call within the same solution will increment the Iteration count.
        /// </summary>
        private readonly int Iteration;
        #endregion
        /// <summary>
        /// This procedure contains the user code. Input parameters are provided as regular arguments,
        /// Output parameters as ref arguments. You don't have to assign output parameters,
        /// they will have a default value.
        /// </summary>
        #region Runscript
        private void RunScript(List<Polyline> polylines, List<int> types, ref object roofs)
        {
            //Medial Axis
            //Laurent Delrieu
            //30 / 12 / 2017
            //l.delrieu@neuf.fr

            //Fit the number of roof types with the number of roofs (polylines)
            if (types.Count < polylines.Count)
            {
                if (types.Count > 0)
                {
                    for (int i = (types.Count - 1); i < polylines.Count; i++)
                    {
                        types.Add(types[types.Count - 1]);
                    }
                    Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "All types are set to last type");
                }
                else
                {
                    for (int i = 0; i < polylines.Count; i++)
                    {
                        types.Add(0);
                    }
                    Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "All types are set to round");
                }
            }
            //Datatree to store roofs brep/surfaces
            DataTree<Brep> roofsDT = new   DataTree<Brep>();

            //Parallel for
            System.Threading.Tasks.Parallel.For(0, polylines.Count,
                index => {
                bool parallel = true;
                roofsDT.AddRange(SandDune(polylines[index], types[index], false, parallel), new GH_Path(index));
                });

            roofs = roofsDT;
        }
        #endregion

        #region Additional

        //Calculation of 45° roof with differents valley
        //Type = 0 => Round : Constant slope (sand)
        //Type = 1 => Butt  : Angular (roof)
        //Type = 2 => Square : Simple
        public List<Brep> SandDune(Polyline polyline, int type, bool isV6, bool parallel)
        {
            List<Brep> output = new List<Brep>();

            //By default round type
            if (type < 0) type = 0;
            if (type > 2) type = 0;

            if (polyline != null)
            {
                double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
                bool isClosed = polyline.IsClosedWithinTolerance(tol);
                //Polyline is closed if not closed
                if (!isClosed)
                {
                    polyline.Add(polyline[0]);
                }
                //Just in case
                polyline.CollapseShortSegments(tol * 2);
                Curve polylineCurve = polyline.ToNurbsCurve();

                //Definition of orientation of polyline
                int orientation = 1;
                if (isV6)
                {
                    //CurveOrientation (inverted in RH5 vs RH6
                    // Undefined          0 Orientation is undefined.
                    // Clockwise         -1 The curve's orientation is clockwise in the xy plane.
                    // CounterClockwise   1 The curve's orientation is counter clockwise in the xy plane
                    CurveOrientation curveOrientation = polylineCurve.ClosedCurveOrientation(Plane.WorldXY);
                    if (curveOrientation == CurveOrientation.Clockwise) orientation = -1;
                    else orientation = 1;
                }
                else
                {
                    //Use of classical surface calculation of polyline
                    if (SurfaceOnXYplane(polyline) > 0)  orientation = -1;
                    else orientation = 1;
                }

                //Bounding box calculation in order to have the max horizontal distances for the roofs surfaces (before cut)
                BoundingBox bb = polylineCurve.GetBoundingBox(false);
                double maxLength = bb.Diagonal.Length;

                //List of brep representing the roof before cut
                List<Brep> breps = new List<Brep>();
                //Side of roof
                for (int i = 0; i < (polyline.Count - 1); i++)
                {
                    //Calculate direction of line
                    Point3d p1 = polyline[i];
                    Point3d p2 = polyline[(i + 1)];
                    Vector3d direction12 = (p2 - p1) * orientation;
                    direction12.Unitize();
                    //Calculate a perpendicular
                    Vector3d perp12 = Vector3d.CrossProduct(direction12, Vector3d.ZAxis);
                    perp12.Unitize();
                    //Because move is one unit horizontal (per12) + one unit in Z => 45°
                    //if you want other angle put a coefficient on perp12 (0 => 90°)
                    Vector3d move = perp12 + Vector3d.ZAxis;
                    move *= maxLength;

                    //Make a sweep
                    var sweep = new Rhino.Geometry.SweepOneRail();
                    sweep.AngleToleranceRadians = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
                    sweep.ClosedSweep = false;
                    sweep.SweepTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                    Brep[] sideBreps = sweep.PerformSweep(new LineCurve(p1, p1 + move), new LineCurve(p1, p2));
                    if (sideBreps.Length > 0) breps.Add(sideBreps[0]);
                }

                //Valley parts of the roof
                for (int i = 0; i < (polyline.Count - 1); i++)
                {
                    //Calculate direction of line
                    int index;
                    if (i == 0)
                    {
                        index = polyline.Count - 2;
                    }
                    else
                    {
                        index = i - 1;
                    }
                    Point3d p1 = polyline[index];
                    Point3d p2 = polyline[i];
                    Point3d p3 = polyline[i + 1];

                    Vector3d direction12 = (p2 - p1);
                    direction12.Unitize();
                    Vector3d direction23 = (p3 - p2);
                    direction23.Unitize();

                    Brep rp = new Brep();
                    //Round type
                    if (type == 0)
                    {
                        rp = ValleyRound(p2, direction12, direction23, maxLength, orientation);
                        if (rp.IsValid) breps.Add(rp);
                    }
                    //Butt type
                    if (type == 1)
                    {
                        Brep[] vv = ValleyButt(p2, direction12, direction23, maxLength, orientation);
                        if (vv.Length >= 2)
                        {
                            if (vv[0].IsValid) breps.Add(vv[0]);
                            if (vv[1].IsValid) breps.Add(vv[1]);
                        }
                    }
                    //Square type
                    if (type == 2)
                    {
                        rp = ValleySquare(p2, direction12, direction23, maxLength, orientation);
                        if (rp.IsValid) breps.Add(rp);
                    }
                }
                output = CutBreps(breps, tol, polylineCurve, parallel);
            }
            return output;
        }
        public Brep ValleyRound(Point3d p2, Vector3d v12, Vector3d v23, double maxLength, int orientation)
        {
            Brep output = new Brep();
            Vector3d vv = Vector3d.CrossProduct(v12, v23);
            if (vv.Z * orientation > 0)
            {
                Vector3d perp12 = Vector3d.CrossProduct(v12, Vector3d.ZAxis * orientation);
                perp12.Unitize();
                Vector3d move12 = perp12 + Vector3d.ZAxis;
                move12 *= maxLength;

                Vector3d perp23 = Vector3d.CrossProduct(v23, Vector3d.ZAxis * orientation);
                perp23.Unitize();
                Vector3d move23 = perp23 + Vector3d.ZAxis;
                move23 *= maxLength;

                Arc arc = new Arc(p2 + move12, v12, p2 + move23);

                var sweep = new Rhino.Geometry.SweepTwoRail();
                sweep.AngleToleranceRadians = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
                sweep.ClosedSweep = false;
                sweep.SweepTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                Brep[] sideBreps = sweep.PerformSweep(new LineCurve(p2 + move12, p2), new LineCurve(p2 + move23, p2), arc.ToNurbsCurve());
                if (sideBreps.Length > 0) output = sideBreps[0];
            }
            return output;
        }

        //Butt valley
        public Brep[] ValleyButt(Point3d p2, Vector3d v12, Vector3d v23, double maxLength, int orientation)
        {
            Brep[] output = new Brep[2];
            output[0] = new Brep();
            output[1] = new Brep();

            Vector3d vv = Vector3d.CrossProduct(v12, v23);
            if (vv.Z * orientation > 0)
            {
                Vector3d perp12 = Vector3d.CrossProduct(v12, Vector3d.ZAxis * orientation);
                perp12.Unitize();
                Vector3d move12 = perp12 + Vector3d.ZAxis;
                move12 *= maxLength;

                Vector3d perp23 = Vector3d.CrossProduct(v23, Vector3d.ZAxis * orientation);
                perp23.Unitize();
                Vector3d move23 = perp23 + Vector3d.ZAxis;
                move23 *= maxLength;

                Line l12 = new Line(p2 + move12, v12);
                Line l23 = new Line(p2 + move23, v23);

                Point3d pmid = p2 + (move23 + move12) / 2;
                double a, b;
                if (Rhino.Geometry.Intersect.Intersection.LineLine(l12, l23, out a, out b, 0.01, false))
                {
                    pmid = l12.PointAt(a);
                }
                var sweep = new Rhino.Geometry.SweepTwoRail();
                sweep.AngleToleranceRadians = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
                sweep.ClosedSweep = false;
                sweep.SweepTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                Brep[] sideBreps = sweep.PerformSweep(new LineCurve(p2 + move12, p2), new LineCurve(pmid, p2), new LineCurve(p2 + move12, pmid));
                if (sideBreps.Length > 0) output[0] = sideBreps[0];

                sideBreps = sweep.PerformSweep(new LineCurve(pmid, p2), new LineCurve(p2 + move23, p2), new LineCurve(pmid, p2 + move23));
                if (sideBreps.Length > 0) output[1] = sideBreps[0];
            }
            return output;
        }

        //Square Valley
        public Brep ValleySquare(Point3d p2, Vector3d v12, Vector3d v23, double maxLength, int orientation)
        {
            Brep output = new Brep();
            Vector3d vv = Vector3d.CrossProduct(v12, v23);
            if (vv.Z * orientation > 0)
            {
                Vector3d perp12 = Vector3d.CrossProduct(v12, Vector3d.ZAxis * orientation);
                perp12.Unitize();
                Vector3d move12 = perp12 + Vector3d.ZAxis;
                move12 *= maxLength;

                Vector3d perp23 = Vector3d.CrossProduct(v23, Vector3d.ZAxis * orientation);
                perp23.Unitize();
                Vector3d move23 = perp23 + Vector3d.ZAxis;
                move23 *= maxLength;


                var sweep = new Rhino.Geometry.SweepTwoRail();
                sweep.AngleToleranceRadians = RhinoDoc.ActiveDoc.ModelAngleToleranceRadians;
                sweep.ClosedSweep = false;
                sweep.SweepTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

                Brep[] sideBreps = sweep.PerformSweep(new LineCurve(p2 + move12, p2), new LineCurve(p2 + move23, p2), new LineCurve(p2 + move12, p2 + move23));
                if (sideBreps.Length > 0) output = sideBreps[0];
            }
            return output;
        }

        //Cut the roof complete surface with curves
        //Example here
        //  https://discourse.mcneel.com/t/splitting-a-brep-by-a-curve-rhinocommon/17435/2
        public List<Brep> CutBreps(List<Brep> breps, double tol, Curve curve, bool parallel)
        {
            List<Brep> output = new List<Brep>();

            if (parallel)
            {
                System.Threading.Tasks.Parallel.For(0, breps.Count,
                    i => {
                    List<Curve> curves = BrepsCurvesIntersection(breps, i, tol, parallel);

                    List<Brep> bb = new List<Brep>();
                    BrepFace bf = breps[i].Faces[0];
                    Brep split = bf.Split(curves, tol);
                    if (split != null)
                    {
                        foreach(BrepFace splitFace in split.Faces)
                        {
                            bb.Add(splitFace.DuplicateFace(false));
                        }
                    }
                    output.AddRange(LowerBrep(bb, curve, tol));
                    });
            }
            else
            {
                //Not parallel
                for (int i = 0; i < breps.Count; i++)
                {
                    List<Curve> curves = BrepsCurvesIntersection(breps, i, tol, parallel);

                    List<Brep> bb = new List<Brep>();
                    BrepFace bf = breps[i].Faces[0];
                    Brep split = bf.Split(curves, tol);
                    if (split != null)
                    {
                        foreach(BrepFace splitFace in split.Faces)
                        {
                            bb.Add(splitFace.DuplicateFace(false));
                        }
                    }
                    output.AddRange(LowerBrep(bb, curve, tol));
                }
            }
            return output;
        }

        //Determine the intersection between the roofs
        public List<Curve> BrepsCurvesIntersection(List<Brep> breps, int index, double tol, bool parallel)
        {
            List<Curve> output = new List<Curve> ();
            if (parallel)
            {
                System.Threading.Tasks.Parallel.For(0, breps.Count,
                    i => {
                    if (i != index)
                    {
                        Curve[] intersectionCurves;
                        Point3d[] intersectionPoints;
                        if (Rhino.Geometry.Intersect.Intersection.BrepBrep(breps[i], breps[index], tol, out intersectionCurves, out intersectionPoints))
                        {
                            foreach (Curve curve in intersectionCurves)
                            {
                                output.Add(curve);
                            }
                        }
                    }
                    });
            }
                //Not parallel
            else
            {
                for (int i = 0; i < (breps.Count - 0); i++)
                {
                    if (i != index)
                    {
                        Curve[] intersectionCurves;
                        Point3d[] intersectionPoints;
                        if (Rhino.Geometry.Intersect.Intersection.BrepBrep(breps[i], breps[index], tol, out intersectionCurves, out intersectionPoints))
                        {
                            foreach (Curve curve in intersectionCurves)
                            {
                                output.Add(curve);
                            }
                        }
                    }
                }
            }
            return output;
        }
        //Search for the lower breps, if brep touch the contour of the roof (roofCurve)
        public List<Brep> LowerBrep(List<Brep> breps, Curve roofCurve, double tol)
        {
            List<Brep> output = new List<Brep>();
            int index = -1;
            if (breps.Count == 1)
            {
                output.Add(breps[0]);
            }
            else
            {
                for (int i = 0; i < breps.Count; i++)
                {
                    Curve[] overlapCurves;
                    Point3d[] intersectionPoints;
                    bool test = Rhino.Geometry.Intersect.Intersection.CurveBrep(roofCurve, breps[i], tol, out overlapCurves, out  intersectionPoints);

                    if (test)
                    {
                        //If the overlap is on the curve it is the part we want
                        if (overlapCurves.Length >= 1)
                        {
                            output.Add(breps[i]);
                        }
                        if (intersectionPoints.Length >= 1)
                        {
                            index = i;
                        }
                    }
                }
                //If there is just a point of contact, it must be a Valley
                if ((output.Count == 0) && (index != -1))
                {
                    output.Add(breps[index]);
                }
            }
            return output;
        }
        //Calculate the surface of a flat closed polygon on XY plane
        //The polygon could be on whatever height (allowed by substraction of polyline[0])
        // Positive if CounterClockwise
        // Negative if Clockwise
        public double SurfaceOnXYplane(Polyline polyline)
        {
            Vector3d sum = Vector3d.Zero;
            for (int i = 0; i < (polyline.Count - 1); i++)
            {
                Vector3d cross = Vector3d.CrossProduct((Vector3d) (polyline[i] - polyline[0]), (Vector3d) (polyline[i + 1] - polyline[0]));
                sum = sum + cross;
            }
            return sum.Z;
        }
        #endregion
    }
}
