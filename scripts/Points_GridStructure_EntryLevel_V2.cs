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
  private void RunScript(double x, int xCount, double xElimination, double y, int yCount, double yElimination, double z, int zCount, double zElimination, object void0, int rollTheBones, int result, ref object Elapsed, ref object resultTree)
  {
        // by The Lord of Darkness due to lack of wind (hope dies last);

    System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
    sw.Start();

    //------------------------------------------------------------------------ public
    vx = new Vector3d(x, 0, 0);
    vy = new Vector3d(0, y, 0);
    vz = new Vector3d(0, 0, z);
    // sampled = new bool[xCount, yCount, zCount]; not needed with lines or tubes
    rnd = new Random();
    Result = result;
    double[] R = new double[3]{x / 10.0, y / 10.0, z / 10.0};
    tubeR = R.Min();

    //------------------------------------------------------------- master antigravity
    connections = new DataTree<object> ();
    MakeConnections(xCount, yCount, zCount, xElimination, yElimination, zElimination);
    resultTree = connections;  //>> out

    Print("Done: {0} objects", connections.DataCount);

    sw.Stop();
    Elapsed = (string.Format("Elapsed: {0}ms", sw.Elapsed.TotalMilliseconds));
  }

  // <Custom additional code> 
    static Point3d orig = new Point3d(0, 0, 0);
  public Vector3d vx;
  public Vector3d vy;
  public Vector3d vz;

  // public bool [,,] sampled;  ---------------- if boxes are required
  public Random rnd;
  public int Result;
  public double tubeR;

  public DataTree<object> connections;

  public void MakeConnections(int xCount, int yCount, int zCount, double xElim, double yElim, double zElim){

    for(int i = 0; i < xCount; i++){
      Vector3d vx1 = i * vx;
      for(int j = 0; j < yCount; j++){
        Vector3d vy1 = j * vy;
        for(int k = 0; k < zCount; k++){
          Vector3d vz1 = k * vz;

          Point3d p = orig + vx1 + vy1 + vz1;
          Point3d px = p + vx; Point3d py = p + vy; Point3d pz = p + vz;

          if( i < xCount - 1 && rnd.Next(0, i + 10) < xElim * (i + 10)) Update(p, px, i, j, k);
          if( j < yCount - 1 && rnd.Next(0, j + 10) < yElim * (j + 10)) Update(p, py, i, j, k);
          if( k < zCount - 1 && rnd.Next(0, k + 10) < zElim * (k + 10)) Update(p, pz, i, j, k);

        }
      }
    }
  }

  public void Update(Point3d p1, Point3d p2, int i, int j, int k){
    LineCurve line = new LineCurve (p1, p2);
    if(Result == 1)connections.Add(line, new GH_Path(i, j, k));
    else{
      Brep tube = Brep.CreatePipe(line, tubeR, false, PipeCapMode.None, false, 0.01, 0.1)[0];
      connections.Add(tube, new GH_Path(i, j, k));
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
        double x = default(double);
    if (inputs[0] != null)
    {
      x = (double)(inputs[0]);
    }

    int xCount = default(int);
    if (inputs[1] != null)
    {
      xCount = (int)(inputs[1]);
    }

    double xElimination = default(double);
    if (inputs[2] != null)
    {
      xElimination = (double)(inputs[2]);
    }

    double y = default(double);
    if (inputs[3] != null)
    {
      y = (double)(inputs[3]);
    }

    int yCount = default(int);
    if (inputs[4] != null)
    {
      yCount = (int)(inputs[4]);
    }

    double yElimination = default(double);
    if (inputs[5] != null)
    {
      yElimination = (double)(inputs[5]);
    }

    double z = default(double);
    if (inputs[6] != null)
    {
      z = (double)(inputs[6]);
    }

    int zCount = default(int);
    if (inputs[7] != null)
    {
      zCount = (int)(inputs[7]);
    }

    double zElimination = default(double);
    if (inputs[8] != null)
    {
      zElimination = (double)(inputs[8]);
    }

    object void0 = default(object);
    if (inputs[9] != null)
    {
      void0 = (object)(inputs[9]);
    }

    int rollTheBones = default(int);
    if (inputs[10] != null)
    {
      rollTheBones = (int)(inputs[10]);
    }

    int result = default(int);
    if (inputs[11] != null)
    {
      result = (int)(inputs[11]);
    }



    //3. Declare output parameters
      object Elapsed = null;
  object resultTree = null;


    //4. Invoke RunScript
    RunScript(x, xCount, xElimination, y, yCount, yElimination, z, zCount, zElimination, void0, rollTheBones, result, ref Elapsed, ref resultTree);
      
    try
    {
      //5. Assign output parameters to component...
            if (Elapsed != null)
      {
        if (GH_Format.TreatAsCollection(Elapsed))
        {
          IEnumerable __enum_Elapsed = (IEnumerable)(Elapsed);
          DA.SetDataList(1, __enum_Elapsed);
        }
        else
        {
          if (Elapsed is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(Elapsed));
          }
          else
          {
            //assign direct
            DA.SetData(1, Elapsed);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (resultTree != null)
      {
        if (GH_Format.TreatAsCollection(resultTree))
        {
          IEnumerable __enum_resultTree = (IEnumerable)(resultTree);
          DA.SetDataList(2, __enum_resultTree);
        }
        else
        {
          if (resultTree is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(resultTree));
          }
          else
          {
            //assign direct
            DA.SetData(2, resultTree);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
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