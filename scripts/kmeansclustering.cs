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
}