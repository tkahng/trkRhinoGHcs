using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;

/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public class Script_Instance : GH_ScriptInstance
{
#region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { __out.Add(text); }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { __out.Add(string.Format(format, args)); }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj)); }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { __out.Add(GH_ScriptComponentUtilities.ReflectType_CS(obj, method_name)); }
#endregion

#region Members
  /// <summary>Gets the current Rhino document.</summary>
  private RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private IGH_Component Component; 
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private int Iteration;
#endregion

  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments, 
  /// Output parameters as ref arguments. You don't have to assign output parameters, 
  /// they will have a default value.
  /// </summary>
  private void RunScript(Brep brep, object void0, int fate, int mode, double rot, object void1, int divX, int divY, double stepZ, int inX, int inY, ref object DucatiumPanigaleumAmamusDumSpiramus, ref object Elapsed, ref object baseBoxWire, ref object baseGrid, ref object baseColumns, ref object xStruts, ref object yStruts)
  {
        if(brep == null || !brep.IsValid)return;

    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    sw.Start();

    BrepFace baseFace = brep.Faces.Where(x => x.IsPlanar()).ToList()[0];
    Curve bound = baseFace.OuterLoop.To3dCurve();

    Plane plane = Plane.WorldXY;
    double rotPlane = RhinoMath.ToRadians(rot);
    plane.Rotate(rotPlane, plane.ZAxis);

    Box box = new Box(plane, bound);
    baseBoxWire = box.ToBrep().GetWireframe(-1);

    GetElements(box, divX, divY, baseFace, brep, stepZ, inX, inY);
    baseGrid = grid;
    baseColumns = columns;
    xStruts = xConn;
    yStruts = yConn;

    sw.Stop();
    Elapsed = (string.Format("Elapsed: {0}ms", sw.Elapsed.TotalMilliseconds));
  }

  // <Custom additional code> 
    public DataTree<Point3d> grid ;
  public DataTree<Line> columns;
  public DataTree<Line> xConn;
  public DataTree<Line> yConn;

  public Mesh BrepToMesh(Brep b){
    Mesh M = new Mesh();
    Mesh[] mset = Mesh.CreateFromBrep(b, MeshingParameters.Default);
    foreach(Mesh m in mset)M.Append(m);
    return M;
  }

  static Vector3d X = new Vector3d(1, 0, 0);
  static Vector3d Y = new Vector3d(0, 1, 0);
  static Vector3d Z = new Vector3d(0, 0, 1);
  // public bool[,,] sampled;

  public int GetMaxDivInZ(Mesh M, double stepZ){
    BoundingBox box = M.GetBoundingBox(true);
    Point3d[] p = box.GetCorners();
    int maxDivZ = (int) (p[0].DistanceTo(p[4]) / (double) stepZ);
    return maxDivZ;
  }

  public HashSet<Point3d> midPts;


  public void  GetElements(Box box, int divX, int divY, BrepFace face, Brep brep, double stepZ, int inX, int inY){

    midPts = new HashSet<Point3d>();
    Random rnd = new Random();

    grid = new DataTree<Point3d>();  columns = new DataTree<Line>();

    xConn = new DataTree<Line>();    yConn = new DataTree<Line>();

    Mesh M = BrepToMesh(brep);
    // int maxDivZ = GetMaxDivInZ(M, stepZ);
    // sampled = new bool[divX, divY, maxDivZ];

    Point3d[] p = box.GetCorners();
    Vector3d dx = (p[1] - p[0]) / (double) divX;
    Vector3d dy = (p[3] - p[0]) / (double) divY;

    for(int i = 0; i < divX;i++){
      for(int j = 0; j < divY;j++){

        Point3d pt = p[0] + i * dx + j * dy;
        double u,v; face.ClosestPoint(pt, out u, out v);
        PointFaceRelation pfr = face.IsPointOnFace(u, v);

        if(pfr == PointFaceRelation.Exterior) continue;
        grid.Add(pt, new GH_Path(i));

        Ray3d ray = new Ray3d(pt + new Vector3d(0, 0, 0.01), Z);
        double t = Rhino.Geometry.Intersect.Intersection.MeshRay(M, ray);

        Line column = new Line(pt, ray.PointAt(t));
        columns.Add(column, new GH_Path(i));

        int divZ = (int) (column.Length / (double) stepZ);
        for(int k = 1; k < divZ;k++){
          Point3d zPt = pt + new Vector3d(0, 0, 1) * k * stepZ;

          int posX = rnd.Next(0, inX + 1);
          if(posX != 0) UpdateConnections(zPt, dx, posX, i, j, k, 1, 0);

          int negX = rnd.Next(0, inX + 1);
          if(negX != 0) UpdateConnections(zPt, dx, negX, i, j, k, -1, 0);

          int posY = rnd.Next(0, inY + 1);
          if(posY != 0) UpdateConnections(zPt, dy, posY, i, j, k, 1, 1);

          int negY = rnd.Next(0, inY + 1);
          if(negY != 0) UpdateConnections(zPt, dy, negY, i, j, k, -1, 1);
        }
      }
    }
  }

  public Point3d Round(Point3d p){
    return new Point3d(Math.Round(p.X, 2), Math.Round(p.Y, 2), Math.Round(p.Z, 2));
  }

  public void UpdateConnections(Point3d zPt, Vector3d v, int count, int i, int j, int k, int sign, int what){

    for(int m = 0; m < count;m++){
      Point3d ps = zPt + sign * ( v * m);
      Point3d pe = zPt + sign * ( v * (m + 1));
      Point3d mid = Round((ps + pe) / 2);
      if(!midPts.Contains(mid)){
        Line conn = new Line(ps, pe);
        if(what == 0) xConn.Add(conn, new GH_Path(i, j, k));
        else yConn.Add(conn, new GH_Path(i, j, k));
        midPts.Add(mid);
      }
    }
  }

  RangedRandom rand = new RangedRandom();

  class RangedRandom : System.Random
  {

    public RangedRandom(): base(){}

    public RangedRandom(int seed): base(seed){}

    public double NextDouble(double max){
      return NextDouble() * max;
    }

    public double NextDouble(double min, double max){
      return (max - min) * NextDouble() + min;
    }
  }
  // </Custom additional code> 

  private List<string> __err = new List<string>(); //Do not modify this list directly.
  private List<string> __out = new List<string>(); //Do not modify this list directly.
  private RhinoDoc doc = RhinoDoc.ActiveDoc;       //Legacy field.
  private IGH_ActiveObject owner;                  //Legacy field.
  private int runCount;                            //Legacy field.
  
  public override void InvokeRunScript(IGH_Component owner, object rhinoDocument, int iteration, List<object> inputs, IGH_DataAccess DA)
  {
    //Prepare for a new run...
    //1. Reset lists
    this.__out.Clear();
    this.__err.Clear();

    this.Component = owner;
    this.Iteration = iteration;
    this.GrasshopperDocument = owner.OnPingDocument();
    this.RhinoDocument = rhinoDocument as Rhino.RhinoDoc;

    this.owner = this.Component;
    this.runCount = this.Iteration;
    this. doc = this.RhinoDocument;

    //2. Assign input parameters
        Brep brep = default(Brep);
    if (inputs[0] != null)
    {
      brep = (Brep)(inputs[0]);
    }

    object void0 = default(object);
    if (inputs[1] != null)
    {
      void0 = (object)(inputs[1]);
    }

    int fate = default(int);
    if (inputs[2] != null)
    {
      fate = (int)(inputs[2]);
    }

    int mode = default(int);
    if (inputs[3] != null)
    {
      mode = (int)(inputs[3]);
    }

    double rot = default(double);
    if (inputs[4] != null)
    {
      rot = (double)(inputs[4]);
    }

    object void1 = default(object);
    if (inputs[5] != null)
    {
      void1 = (object)(inputs[5]);
    }

    int divX = default(int);
    if (inputs[6] != null)
    {
      divX = (int)(inputs[6]);
    }

    int divY = default(int);
    if (inputs[7] != null)
    {
      divY = (int)(inputs[7]);
    }

    double stepZ = default(double);
    if (inputs[8] != null)
    {
      stepZ = (double)(inputs[8]);
    }

    int inX = default(int);
    if (inputs[9] != null)
    {
      inX = (int)(inputs[9]);
    }

    int inY = default(int);
    if (inputs[10] != null)
    {
      inY = (int)(inputs[10]);
    }



    //3. Declare output parameters
      object DucatiumPanigaleumAmamusDumSpiramus = null;
  object Elapsed = null;
  object baseBoxWire = null;
  object baseGrid = null;
  object baseColumns = null;
  object xStruts = null;
  object yStruts = null;


    //4. Invoke RunScript
    RunScript(brep, void0, fate, mode, rot, void1, divX, divY, stepZ, inX, inY, ref DucatiumPanigaleumAmamusDumSpiramus, ref Elapsed, ref baseBoxWire, ref baseGrid, ref baseColumns, ref xStruts, ref yStruts);
      
    try
    {
      //5. Assign output parameters to component...
            if (DucatiumPanigaleumAmamusDumSpiramus != null)
      {
        if (GH_Format.TreatAsCollection(DucatiumPanigaleumAmamusDumSpiramus))
        {
          IEnumerable __enum_DucatiumPanigaleumAmamusDumSpiramus = (IEnumerable)(DucatiumPanigaleumAmamusDumSpiramus);
          DA.SetDataList(1, __enum_DucatiumPanigaleumAmamusDumSpiramus);
        }
        else
        {
          if (DucatiumPanigaleumAmamusDumSpiramus is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(DucatiumPanigaleumAmamusDumSpiramus));
          }
          else
          {
            //assign direct
            DA.SetData(1, DucatiumPanigaleumAmamusDumSpiramus);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (Elapsed != null)
      {
        if (GH_Format.TreatAsCollection(Elapsed))
        {
          IEnumerable __enum_Elapsed = (IEnumerable)(Elapsed);
          DA.SetDataList(2, __enum_Elapsed);
        }
        else
        {
          if (Elapsed is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(Elapsed));
          }
          else
          {
            //assign direct
            DA.SetData(2, Elapsed);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (baseBoxWire != null)
      {
        if (GH_Format.TreatAsCollection(baseBoxWire))
        {
          IEnumerable __enum_baseBoxWire = (IEnumerable)(baseBoxWire);
          DA.SetDataList(3, __enum_baseBoxWire);
        }
        else
        {
          if (baseBoxWire is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(baseBoxWire));
          }
          else
          {
            //assign direct
            DA.SetData(3, baseBoxWire);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
      }
      if (baseGrid != null)
      {
        if (GH_Format.TreatAsCollection(baseGrid))
        {
          IEnumerable __enum_baseGrid = (IEnumerable)(baseGrid);
          DA.SetDataList(4, __enum_baseGrid);
        }
        else
        {
          if (baseGrid is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(baseGrid));
          }
          else
          {
            //assign direct
            DA.SetData(4, baseGrid);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }
      if (baseColumns != null)
      {
        if (GH_Format.TreatAsCollection(baseColumns))
        {
          IEnumerable __enum_baseColumns = (IEnumerable)(baseColumns);
          DA.SetDataList(5, __enum_baseColumns);
        }
        else
        {
          if (baseColumns is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(5, (Grasshopper.Kernel.Data.IGH_DataTree)(baseColumns));
          }
          else
          {
            //assign direct
            DA.SetData(5, baseColumns);
          }
        }
      }
      else
      {
        DA.SetData(5, null);
      }
      if (xStruts != null)
      {
        if (GH_Format.TreatAsCollection(xStruts))
        {
          IEnumerable __enum_xStruts = (IEnumerable)(xStruts);
          DA.SetDataList(6, __enum_xStruts);
        }
        else
        {
          if (xStruts is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(6, (Grasshopper.Kernel.Data.IGH_DataTree)(xStruts));
          }
          else
          {
            //assign direct
            DA.SetData(6, xStruts);
          }
        }
      }
      else
      {
        DA.SetData(6, null);
      }
      if (yStruts != null)
      {
        if (GH_Format.TreatAsCollection(yStruts))
        {
          IEnumerable __enum_yStruts = (IEnumerable)(yStruts);
          DA.SetDataList(7, __enum_yStruts);
        }
        else
        {
          if (yStruts is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(7, (Grasshopper.Kernel.Data.IGH_DataTree)(yStruts));
          }
          else
          {
            //assign direct
            DA.SetData(7, yStruts);
          }
        }
      }
      else
      {
        DA.SetData(7, null);
      }

    }
    catch (Exception ex)
    {
      this.__err.Add(string.Format("Script exception: {0}", ex.Message));
    }
    finally
    {
      //Add errors and messages... 
      if (owner.Params.Output.Count > 0)
      {
        if (owner.Params.Output[0] is Grasshopper.Kernel.Parameters.Param_String)
        {
          List<string> __errors_plus_messages = new List<string>();
          if (this.__err != null) { __errors_plus_messages.AddRange(this.__err); }
          if (this.__out != null) { __errors_plus_messages.AddRange(this.__out); }
          if (__errors_plus_messages.Count > 0) 
            DA.SetDataList(0, __errors_plus_messages);
        }
      }
    }
  }
}