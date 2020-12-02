
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
using System.Linq;
using StraightSkeletonNet;
using StraightSkeletonNet.Primitives;

/// Unique namespace, so visual studio won't throw any errors about duplicate definitions.
namespace ns8afd6
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
        private void RunScript(List<Point3d> Vertices, Polyline polygon, ref object Polygons, ref object Skeleton, ref object Spine)
        {
                            // PolyCurve polycurve = new PolyCurve;
                            var plane = new Plane(Rhino.Geometry.Plane.WorldXY);
                            var polycurve = polygon.ToPolylineCurve();

                            var vertices2d = new List<StraightSkeletonNet.Primitives.Vector2d>();

                            foreach (var seg in polygon.GetSegments())
                            {
                                    var vertex = seg.From;
                                    vertices2d.Add(new StraightSkeletonNet.Primitives.Vector2d(vertex.X, vertex.Y));
                            }

                            var skeleton = SkeletonBuilder.Build(vertices2d);

                            var polygons = new List<Polyline>();

                            foreach (var edgeResult in skeleton.Edges)
                            {
                                    var vertices = new List<Point3d>();
                                    foreach (var vertex in edgeResult.Polygon)
                                    {
                                            vertices.Add(new Point3d(vertex.X, vertex.Y, 0.0));
                                    }
                                    polygons.Add(new Polyline(vertices));
                            }

                            var skels = new List<Line>();
                            var isin = Rhino.Geometry.PointContainment.Inside;
                            foreach (var poly in polygons)
                            {
                                    foreach (var seg in poly.GetSegments())
                                    {
                                            if (polycurve.Contains(seg.From, plane, 0.01) == isin)
                                            {
                                                    skels.Add(seg);
                                            }
                                    }
                            }

                            var spine = new List<Line>();

                            foreach (var skel in skels)
                            {
                                    if ((polycurve.Contains(skel.From, plane, 0.01) == isin) && (polycurve.Contains(skel.To, plane, 0.01) == isin))
                                    {
                                            spine.Add(skel);
                                    }
                            }

                            Polygons = polygons;
                            Skeleton = skels;
                            Spine = spine;
        }
        #endregion

        #region Additional

        #endregion
    }
}
