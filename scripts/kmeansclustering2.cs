using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



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
  private void RunScript(List<Point3d> points, int groupsNumber, double precision, int seed, ref object G)
  {
        //Code by Dieter Toews
    //https://www.grasshopper3d.com/forum/topics/kmeans-1
    //Rewritten in January 2019 by Laurent Delrieu

    //***************** Initialize K-Means Algorithm ****************************
    Print("--------Initializing " + groupsNumber + " groupings-------");
    Component.Message = "K-Means Clustering";

    List<List<int>> groupings = new List<List<int>>();
    List<Point3d> oldCentroids = new  List<Point3d> ();
    List<Point3d> newCentroids = new  List<Point3d> ();
    for (int i = 0; i < groupsNumber; i++)
    {
      groupings.Add(new List<int>());
      newCentroids.Add(Point3d.Origin);
      oldCentroids.Add(Point3d.Origin);
      Print("creating grouping " + i);
    }
    //randomly assign points from the input list (intialze the k-mean groups)
    Print("--------Initializing group assignments (random)-------");
    Random r = new Random(seed);
    DataTree<Point3d> groupingsTree = new DataTree<Point3d>();
    for (int j = 0 ; j < points.Count; j++)
    {
      int groupNum = RandomNumber(r, groupsNumber - 1, 0);
      groupings[groupNum].Add(j);
      Print("Adding index value " + groupings[groupNum][groupings[groupNum].Count - 1] + " to group " + groupNum + ".");
    }
    //***************** Do K-Means Algorithm ************************************
    Print("--------Entering main k-means loop--------");
    bool isStillMoving = true;
    //Number of while counting
    int k = 0;
    double minDist, dist;
    Point3d XYZ = Point3d.Origin;
    Point3d pt = Point3d.Origin;

    while ( isStillMoving)
    {
      isStillMoving = false;
      //find new centroids
      Print(".....finding new centroids");
      for (int i = 0; i < groupsNumber; i++)
      {
        oldCentroids[i] = newCentroids[i];
        for (int j = 0; j < groupings[i].Count; j++)
        {
          XYZ = XYZ + points[groupings[i][j]];
        }
        XYZ = XYZ / ((double) groupings[i].Count + 1);

        Point3d newCentroid = XYZ;
        newCentroids[i] = newCentroid;
        Print("New centroid for group " + i + " is " + newCentroids[i].ToString());
        groupings[i].Clear();
        dist = newCentroid.DistanceTo(oldCentroids[i]);

        if (dist > precision)
        {
          Print("need to do more work");
          isStillMoving = true;
        }
      }
      //create new groupings
      Print(".....creating new groupings");
      for (int j = 0; j < points.Count; j++)
      {
        pt = points[j];
        minDist = pt.DistanceTo(newCentroids[0]);
        int groupNum = 0;
        for (int i = 0; i < groupsNumber; i++)
        {
          dist = pt.DistanceTo(newCentroids[i]);
          if( minDist >= dist )
          {
            groupNum = i;
            minDist = dist;
          }
        }
        groupings[groupNum].Add(j);
        if ( isStillMoving == false)
        {
          groupingsTree.Add(pt, new GH_Path(groupNum));
        }
      }
      if (isStillMoving == false)
      {
        Print("***************all groups setled exiting loop********************");
        Print("***************total loop count is " + k + "************************");
      }
      //a hard limit on the number of loops to execute....
      if(k > 1000)
      {
        isStillMoving = false;
      }
      k += 1;
    }//End while
    G = groupingsTree;
  }

  // <Custom additional code> 
    public int RandomNumber(Random r, int MaxNumber, int MinNumber)
  {
    //if passed incorrect arguments, swap them
    //can also throw exception or return 0
    if (MinNumber > MaxNumber)
    {
      int t = MinNumber;
      MinNumber = MaxNumber;
      MaxNumber = t;
    }
    return r.Next(MinNumber, MaxNumber);
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
        List<Point3d> points = null;
    if (inputs[0] != null)
    {
      points = GH_DirtyCaster.CastToList<Point3d>(inputs[0]);
    }
    int groupsNumber = default(int);
    if (inputs[1] != null)
    {
      groupsNumber = (int)(inputs[1]);
    }

    double precision = default(double);
    if (inputs[2] != null)
    {
      precision = (double)(inputs[2]);
    }

    int seed = default(int);
    if (inputs[3] != null)
    {
      seed = (int)(inputs[3]);
    }



    //3. Declare output parameters
      object G = null;


    //4. Invoke RunScript
    RunScript(points, groupsNumber, precision, seed, ref G);
      
    try
    {
      //5. Assign output parameters to component...
            if (G != null)
      {
        if (GH_Format.TreatAsCollection(G))
        {
          IEnumerable __enum_G = (IEnumerable)(G);
          DA.SetDataList(1, __enum_G);
        }
        else
        {
          if (G is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(G));
          }
          else
          {
            //assign direct
            DA.SetData(1, G);
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