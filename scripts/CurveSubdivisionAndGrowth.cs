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
  private void RunScript(bool iReset, List<Point3d> iStartingPositions, int iMaxCenterCount, ref object oCenters)
  {
        if (iReset || centers == null)
      centers = new List<Point3d>(iStartingPositions);

    List<Vector3d> totalMoves = new List<Vector3d>();
    List<double> collisionCounts = new List<double>();

    for (int i = 0; i < centers.Count; i++)
    {
      totalMoves.Add(new Vector3d(0.0, 0.0, 0.0));
      collisionCounts.Add(0.0);
    }

    double collisionDistance = 2.0;

    for (int i = 0; i < centers.Count; i++)
      for (int j = i + 1; j < centers.Count; j++)
      {
        double d = centers[i].DistanceTo(centers[j]);
        if (d > collisionDistance) continue;
        Vector3d move = centers[i] - centers[j];
        if (move.Length < 0.001) continue;
        move.Unitize();
        move *= 0.5 * (collisionDistance - d);
        totalMoves[i] += move;
        totalMoves[j] -= move;
        collisionCounts[i] += 1.0;
        collisionCounts[j] += 1.0;
      }

    for (int i = 0; i < centers.Count; i++)
      if (collisionCounts[i] != 0.0)
        centers[i] += totalMoves[i] / collisionCounts[i];

    if (centers.Count < iMaxCenterCount)
    {
      List<int> splitIndices = new List<int>();

      for (int i = 0; i < centers.Count - 1; i++)
        if (centers[i].DistanceTo(centers[i + 1]) > 1.99)
          splitIndices.Add(i + 1 + splitIndices.Count);

      foreach (int splitIndex in splitIndices)
      {
        Point3d newCenter = 0.5 * (centers[splitIndex - 1] + centers[splitIndex]);
        centers.Insert(splitIndex, newCenter);
      }
    }


    oCenters = centers;
  }

  // <Custom additional code> 
  

  List<Point3d> centers;






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
        bool iReset = default(bool);
    if (inputs[0] != null)
    {
      iReset = (bool)(inputs[0]);
    }

    List<Point3d> iStartingPositions = null;
    if (inputs[1] != null)
    {
      iStartingPositions = GH_DirtyCaster.CastToList<Point3d>(inputs[1]);
    }
    int iMaxCenterCount = default(int);
    if (inputs[2] != null)
    {
      iMaxCenterCount = (int)(inputs[2]);
    }



    //3. Declare output parameters
      object oCenters = null;


    //4. Invoke RunScript
    RunScript(iReset, iStartingPositions, iMaxCenterCount, ref oCenters);
      
    try
    {
      //5. Assign output parameters to component...
            if (oCenters != null)
      {
        if (GH_Format.TreatAsCollection(oCenters))
        {
          IEnumerable __enum_oCenters = (IEnumerable)(oCenters);
          DA.SetDataList(1, __enum_oCenters);
        }
        else
        {
          if (oCenters is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(oCenters));
          }
          else
          {
            //assign direct
            DA.SetData(1, oCenters);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
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