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
  private void RunScript(List<Line> lines_in, double tolerance, List<double> width_b, List<double> width_e, double deviation, double angle, int n_u, int n_v, bool zbool, ref object P, ref object LP, ref object PP, ref object PL, ref object lines, ref object ends, ref object connect, ref object brep, ref object M, ref object F, ref object U, ref object V)
  {
        //v02
    //Add variable width per lines
    //V03
    //Suppression of limitation in number of run of the while
    //Test of n_u and n_v in order to be no less than 2
    //

    //OUT data
    List<Point3d> points = new List<Point3d>();
    DataTree<int> lineVertex = new DataTree<int>();
    DataTree<int> vertexVertexes = new DataTree<int>();
    DataTree<int> vertexLine = new DataTree<int>();

    List<Line> linesOut = new List<Line>();
    DataTree<Line> linesBranch = new DataTree<Line>();
    bool isEnd;
    List<Line> linesEnd = new List<Line>();

    List<LDPanel> panels = new List<LDPanel>();
    double width_calc = 0.0;

    ///BEGIN TEST INPUTS
    if (lines_in == null) return;

    if (lines_in.Count < 1)
    {
      this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There must be more than 1 line");
      return;
    }
    LineTopology(lines_in, tolerance, ref points, ref  lineVertex, ref vertexVertexes, ref vertexLine);

    //v0.2
    if (width_b.Count < lines_in.Count)
    {
      int size_width = width_b.Count;

      for (int i = size_width; i <= lines_in.Count; i++)
      {
        width_b.Add(width_b[size_width - 1]);
      }
    }

    if (width_e.Count < lines_in.Count)
    {
      int size_width = width_e.Count;

      for (int i = size_width; i <= lines_in.Count; i++)
      {
        width_e.Add(width_e[size_width - 1]);
      }
    }
    //v0.2
    if (n_u < 2) n_u = 2;
    if (n_v < 2) n_v = 2;

    //For all lines
    for (int i = 0; i < lineVertex.BranchCount; i++)
    {
      Line lineOut = new Line();
      LDSide side1 = new LDSide();
      LDSide side2 = new LDSide();

      //One side of the line
      lineOut = LinePerp(points, lineVertex, vertexVertexes, true, true, i, width_b, width_e, out isEnd, out width_calc);
      if (isEnd) linesEnd.Add(lineOut);
      linesBranch.Add(lineOut, new GH_Path(i * 2));
      side1 = new LDSide(lineVertex.Branch(i)[1], points[lineVertex.Branch(i)[1]], lineOut.Direction, isEnd, width_calc);

      lineOut = LinePerp(points, lineVertex, vertexVertexes, false, false, i, width_b, width_e, out isEnd, out width_calc);
      if (isEnd) linesEnd.Add(lineOut);
      linesBranch.Add(lineOut, new GH_Path(i * 2));
      side2 = new LDSide(lineVertex.Branch(i)[0], points[lineVertex.Branch(i)[0]], lineOut.Direction, isEnd, width_calc);
      panels.Add(new LDPanel(side1, side2, i));

      //Other side of the line
      lineOut = LinePerp(points, lineVertex, vertexVertexes, false, true, i, width_b, width_e, out isEnd, out width_calc);
      if (isEnd) linesEnd.Add(lineOut);
      linesBranch.Add(lineOut, new GH_Path(i * 2 + 1));
      side1 = new LDSide(lineVertex.Branch(i)[0], points[lineVertex.Branch(i)[0]], lineOut.Direction, isEnd, width_calc);

      lineOut = LinePerp(points, lineVertex, vertexVertexes, true, false, i, width_b, width_e, out isEnd, out width_calc);
      if (isEnd) linesEnd.Add(lineOut);
      linesBranch.Add(lineOut, new GH_Path(i * 2 + 1));
      side2 = new LDSide(lineVertex.Branch(i)[1], points[lineVertex.Branch(i)[1]], lineOut.Direction, isEnd, width_calc);
      panels.Add(new LDPanel(side1, side2, i));
    }

    DataTree<LDPanel> treePanels = new DataTree<LDPanel>();
    DataTree<int> treeInt = new DataTree<int>();

    int test = 0;
    int branch = 0;
    while (panels.Count > 0)
    {
      treePanels.Add(panels[0], new GH_Path(branch));
      treeInt.Add(panels[0].lineNumber, new GH_Path(branch));
      panels.RemoveAt(0);

      int intersections = 0;
      do
      {
        List<LDPanel> intPanels = GetIntersections(treePanels.Branch(branch), panels);
        intersections = intPanels.Count;
        for (int i = (intPanels.Count - 1); i >= 0; i--)
        {
          treePanels.Add(intPanels[i], new GH_Path(branch));
          treeInt.Add(intPanels[i].lineNumber, new GH_Path(branch));
          panels.Remove(intPanels[i]);
        }
      } while (intersections > 0);
      test += 1;
      branch += 1;
    }
    DataTree<Brep> brepTree = new DataTree<Brep>();

    for (int i = 0; i < treePanels.BranchCount; i++)
    {
      for (int j = 0; j < treePanels.Branch(i).Count; j++)
      {
        brepTree.Add(treePanels.Branch(i)[j].ToBrep(), new GH_Path(i));
      }
    }
    DataTree<Mesh> meshTree = new DataTree<Mesh>();
    List<Point3d> fixedPoints = new List<Point3d>();
    DataTree<LineCurve> listUCurves = new DataTree<LineCurve> ();
    DataTree<LineCurve> listVCurves = new DataTree<LineCurve> ();

    for (int i = 0; i < treePanels.BranchCount; i++)
    {
      for (int j = 0; j < treePanels.Branch(i).Count; j++)
      {
        //meshTree.Add(treePanels.Branch(i)[j].ToMesh(n_u, n_v, angle, width_b[0], ref fixedPoints, ref listUCurves, ref  listVCurves, deviation, zbool), new GH_Path(i));
        meshTree.Add(treePanels.Branch(i)[j].ToMesh(n_u, n_v, angle, ref fixedPoints, ref listUCurves, ref  listVCurves, deviation, zbool), new GH_Path(i));
      }
    }
    P = points;
    LP = lineVertex;
    PP = vertexVertexes;
    PL = vertexLine;
    lines = linesBranch;
    ends = linesEnd;
    connect = treeInt;
    brep = brepTree;
    M = meshTree;
    F = fixedPoints;
    U = listUCurves;
    V = listVCurves;
  }

  // <Custom additional code> 
  
  //For points begin and end, representing a line, search the width
  public double getWidthAtBeginning(int begin, int end, DataTree<int> lineVertex, List<double> w_b, List<double> w_e)
  {
    double output = w_b[0];

    for (int i = 0; i < lineVertex.BranchCount; i++)
    {
      if ((lineVertex.Branches[i][0] == begin) && (lineVertex.Branches[i][1] == end))
      {
        output = w_b[i];
        break;
      }
      if ((lineVertex.Branches[i][0] == end) && (lineVertex.Branches[i][1] == begin))
      {
        output = w_e[i];
        break;
      }
    }
    return output;
  }

  //Calculate on the line i, some curve perpandicular if it is an end or mediane if connection with othe lines
  // bool one is used to know if we begin from the firs point (beginning) of line or second (end)
  public Line LinePerp(List<Point3d> points, DataTree<int> lineVertex, DataTree<int> vertexVertexes, bool one, bool two, int i, List<double> width_b, List<double> width_e, out bool isEnd, out double width_calc)
  {
    int i0, i1;
    double signe = 1.0;
    double width;
    if (one)
    {
      i0 = 0;
      i1 = 1;
      width = width_e[i];
    }
    else
    {
      i0 = 1;
      i1 = 0;
      width = width_b[i];
    }

    if (two)
    {
      signe = 1.0;
    }
    else
    {
      signe = -1.0;
    }

    Vector3d dirPerp2 = new Vector3d();
    //Direction of the line
    Vector3d dir = (Vector3d) (points[lineVertex.Branch(i)[i1]] - points[lineVertex.Branch(i)[i0]]);
    //One direction perpendicular of the line, depending on signe sign
    Vector3d dirPerp = Vector3d.CrossProduct(dir, signe * Vector3d.ZAxis);
    dirPerp.Unitize();

    double angleTampon;
    double angle = 10.0;
    double width_buffer = width;
    angle = angle * signe;
    //More than one branch
    if (vertexVertexes.Branch(lineVertex.Branch(i)[i1]).Count > 1)
    {
      isEnd = false;
      //For all points connected to the points at the end of the line (end could be a beginning !)
      for (int j = 0; j < vertexVertexes.Branch(lineVertex.Branch(i)[i1]).Count; j++)
      {
        //If it is not the same line
        if (vertexVertexes.Branch(lineVertex.Branch(i)[i1])[j] != lineVertex.Branch(i)[i0])
        {
          Vector3d dir2 = (Vector3d) (points[vertexVertexes.Branch(lineVertex.Branch(i)[i1])[j]] - points[lineVertex.Branch(i)[i1]]);
          Vector3d dirPerp2Tampon = Vector3d.CrossProduct(dir2, signe * Vector3d.ZAxis);
          dirPerp2Tampon.Unitize();

          //Line connected as 2 points :
          // vertexVertexes.Branch(lineVertex.Branch(i)[i1])[j]
          // lineVertex.Branch(i)[i1]
          angleTampon = Angle(dirPerp2Tampon, dirPerp);
          if (signe > 0)
          {
            if (angleTampon < angle)
            {
              angle = angleTampon;
              dirPerp2 = dirPerp2Tampon;
              width_buffer = getWidthAtBeginning(lineVertex.Branch(i)[i1], vertexVertexes.Branch(lineVertex.Branch(i)[i1])[j], lineVertex, width_b, width_e);
            }
          }
          else
          {
            if ( angleTampon > angle)
            {
              angle = angleTampon;
              dirPerp2 = dirPerp2Tampon;
              width_buffer = getWidthAtBeginning(lineVertex.Branch(i)[i1], vertexVertexes.Branch(lineVertex.Branch(i)[i1])[j], lineVertex, width_b, width_e);
            }
          }
        }
      }
    }
      //Line as an End
    else
    {
      isEnd = true;
      angle = 0.0;
      dirPerp2 = dirPerp;
    }
    width = width + width_buffer;
    width = width / 2;
    width_calc = width;
    if (width / (2.0 * Math.Cos(angle / 2) * Math.Cos(angle / 2)) > 100)
    {
      return new Line(points[lineVertex.Branch(i)[i1]], points[lineVertex.Branch(i)[i1]] - width * (dirPerp + dirPerp2));
    }
    else
      return new Line(points[lineVertex.Branch(i)[i1]], points[lineVertex.Branch(i)[i1]] - width * (dirPerp + dirPerp2) / (2.0 * Math.Cos(angle / 2) * Math.Cos(angle / 2)));
  }


  //This function replicate SandBox lines topology
  //As inputs there are lines
  //Tolerance is used to glued points thar are close to each others
  //Outputs are
  //points list of Point3d
  //linePoint dataTree containing the points index number for end and beginning of the line
  //pointPoint dataTree containing the points index number for each other point connected to this point
  public static void LineTopology(List < Line > lines, double tolerance, ref List<Point3d> points, ref DataTree<int> linePoint, ref DataTree<int> pointPoint, ref  DataTree<int> pointLine)
  {
    for (int n_line = 0; n_line < lines.Count; n_line++)
    {
      bool addPoint = true;
      int nPoint = 0;
      for (int i = 0; i < points.Count; i++)
      {
        if (points[i].DistanceTo(lines[n_line].From) < tolerance)
        {
          addPoint = false;
          nPoint = i;
        }
      }
      if (addPoint)
      {
        points.Add(lines[n_line].From);
        linePoint.Add(points.Count - 1, new GH_Path(n_line));
      }
      else
      {
        linePoint.Add(nPoint, new GH_Path(n_line));
      }

      addPoint = true;
      for (int i = 0; i < points.Count; i++)
      {
        if (points[i].DistanceTo(lines[n_line].To) < tolerance)
        {
          addPoint = false;
          nPoint = i;
        }
      }
      if (addPoint)
      {
        points.Add(lines[n_line].To);
        linePoint.Add(points.Count - 1, new GH_Path(n_line));
      }
      else
      {
        linePoint.Add(nPoint, new GH_Path(n_line));
      }
    }
    for (int n_line = 0; n_line < linePoint.BranchCount; n_line++)
    {
      pointPoint.Add(linePoint.Branches[n_line][0], new GH_Path(linePoint.Branches[n_line][1]));
      pointPoint.Add(linePoint.Branches[n_line][1], new GH_Path(linePoint.Branches[n_line][0]));
    }

    for (int j = 0; j < linePoint.BranchCount; j++)
    {
      pointLine.Add(j, new GH_Path(linePoint.Branch(j)[0]));
      pointLine.Add(j, new GH_Path(linePoint.Branch(j)[1]));
    }

  }
  ///End lineTopology

  public static double Angle(Vector3d v1, Vector3d v2)
  {
    double output = -10;
    v1.Unitize();
    v2.Unitize();
    double g1 = Math.Acos(Math.Max(Math.Min(v1 * v2, 1.0), -1.0));
    Vector3d cross = Vector3d.CrossProduct(v1, v2);
    double g2 = Math.Asin(Math.Max(Math.Min(cross.Z, 1.0), -1.0));

    if (g2 >= 0)
    {
      output = g1;
    }

    else
    {
      if (g1 >= 0)
      {
        output = 2 * Math.PI - g1;
      }
      else
      {
        output = g2 + Math.PI;
      }
    }
    if (output > Math.PI) output = output - 2 * Math.PI;
    return (output % Math.PI);
  }
  ///////////////////////////
  public class LDPanel
  {
    public LDSide side1;
    public LDSide side2;
    public int lineNumber;

    public List<int> ToList()
    {
      List<int> output = new List<int>();
      output.Add(side1.nodeNumber);
      output.Add(side2.nodeNumber);
      output.Add(lineNumber);
      return output;
    }
    public Brep ToBrep()
    {
      List<Curve> curves = new List<Curve>();
      curves.Add(side1.ToCurve());
      curves.Add(side2.ToCurve());
      // Brep[] output = Brep.CreateFromLoft(curves, side1.point, side2.point, LoftType.Normal, false);
      Brep[] output = Brep.CreateFromLoft(curves, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);

      if (output.Length > 0) return output[0];
      else return null;
    }

    //With LIST
    //public Mesh ToMesh(int nu, int nv, double angle, double radius, ref List<Point3d> fixedPoints, ref List<LineCurve> listUCurves, ref List<LineCurve> listVCurves, double deviation, bool zbool)
    public Mesh ToMesh(int nu, int nv, double angle, ref List<Point3d> fixedPoints, ref List<LineCurve> listUCurves, ref List<LineCurve> listVCurves, double deviation, bool zbool)
    {
      List < Point3d > pts1 = new  List < Point3d >();
      List < Point3d > pts2 = new  List < Point3d >();

      if (side1.isEnd)
      {
        pts1 = ToArcDeviation(side1.point, new Vector3d(side2.point - side1.point), side1.direction, angle, nv, deviation, zbool);
        fixedPoints.AddRange(pts1);
      }
      else
      {
        //pts1 = ToLine(side1.point, side1.direction, nv, angle, radius, zbool);
        pts1 = ToLine(side1.point, side1.direction, nv, angle, side1.width, zbool);
      }


      if (side2.isEnd)
      {
        pts2 = ToArcDeviation(side2.point, new Vector3d(side1.point - side2.point), side2.direction, angle, nv, deviation, zbool);
        fixedPoints.AddRange(pts2);
      }
      else
      {
        //pts2 = ToLine(side2.point, side2.direction, nv, angle, radius, zbool);
        pts2 = ToLine(side2.point, side2.direction, nv, angle, side2.width, zbool);
      }

      return PointsToMesh(pts1, pts2, nu, ref listUCurves, ref  listVCurves);
    }

    //With DATATREE
    // public Mesh ToMesh(int nu, int nv, double angle, double radius, ref List<Point3d> fixedPoints, ref DataTree<LineCurve> listUCurves, ref DataTree<LineCurve> listVCurves, double deviation, bool zbool)
    public Mesh ToMesh(int nu, int nv, double angle, ref List<Point3d> fixedPoints, ref DataTree<LineCurve> listUCurves, ref DataTree<LineCurve> listVCurves, double deviation, bool zbool)
    {
      List < Point3d > pts1 = new  List < Point3d >();
      List < Point3d > pts2 = new  List < Point3d >();

      if (side1.isEnd)
      {
        pts1 = ToArcDeviation(side1.point, new Vector3d(side2.point - side1.point), side1.direction, angle, nv, deviation, zbool);
        fixedPoints.AddRange(pts1);
      }
      else
      {
        //pts1 = ToLine(side1.point, side1.direction, nv, angle, radius, zbool);
        pts1 = ToLine(side1.point, side1.direction, nv, angle, side1.width, zbool);
      }

      if (side2.isEnd)
      {
        pts2 = ToArcDeviation(side2.point, new Vector3d(side1.point - side2.point), side2.direction, angle, nv, deviation, zbool);
        fixedPoints.AddRange(pts2);
      }
      else
      {
        //pts2 = ToLine(side2.point, side2.direction, nv, angle, radius, zbool);
        pts2 = ToLine(side2.point, side2.direction, nv, angle, side2.width, zbool);
      }

      return PointsToMesh(pts1, pts2, nu, ref listUCurves, ref  listVCurves);
    }


    public LDPanel(LDSide s1, LDSide s2, int line)
    {
      side1 = s1;
      side2 = s2;
      lineNumber = line;
    }
    public bool Touch(LDPanel other)
    {
      bool output = false;
      Vector3d vect = new Vector3d();
      if (other.side1.nodeNumber == side1.nodeNumber)
      {
        vect = other.side1.direction - side1.direction;
        if (vect.Length < 0.001) output = true;
      }

      if (other.side2.nodeNumber == side1.nodeNumber)
      {
        vect = other.side2.direction - side1.direction;
        if (vect.Length < 0.001) output = true;
      }

      if (other.side1.nodeNumber == side2.nodeNumber)
      {
        vect = other.side1.direction - side2.direction;
        if (vect.Length < 0.001) output = true;
      }

      if (other.side2.nodeNumber == side2.nodeNumber)
      {
        vect = other.side2.direction - side2.direction;
        if (vect.Length < 0.001) output = true;
      }

      return output;
    }



  }
  public static  List<LDPanel> GetIntersections(List < LDPanel > lines, List<LDPanel> source)
  {
    List<LDPanel> output = new List<LDPanel>();

    for (int i = 0; i < lines.Count; i++)
    {
      for (int j = 0; j < source.Count; j++)
      {
        if (lines[i].Touch(source[j]))
        {
          bool test = true;
          for (int k = 0; k < output.Count; k++)
          {
            if (source[j].lineNumber == output[k].lineNumber) test = false;
          }
          if (test) output.Add(source[j]);
        }
      }
    }
    return output;
  }
  public class LDSide
  {
    public int nodeNumber;
    public Point3d point;
    public Vector3d direction;
    public bool isEnd;
    public double width;

    public LDSide()
    {
      nodeNumber = -1;
      point = new Point3d(Point3d.Unset);
      direction = new Vector3d(Vector3d.Unset);
      isEnd = true;
    }

    public LDSide(LDSide side)
    {
      LDSideSet(side.nodeNumber, side.point, side.direction, side.isEnd, side.width);
    }
    public LDSide(int n, Point3d pt, Vector3d dir, bool end, double width_calc)
    {
      nodeNumber = n;
      point = new Point3d(pt);
      direction = new Vector3d(dir);
      isEnd = end;
      width = width_calc;
    }
    public void LDSideSet(int n, Point3d pt, Vector3d dir, bool end, double width_calc)
    {
      nodeNumber = n;
      point = new Point3d(pt);
      direction = new Vector3d(dir);
      isEnd = end;
      width = width_calc;
    }
    public Curve ToCurve()
    {
      return new LineCurve(point, (Point3d) (point + direction));
    }
  }
  /////////////////////////////////////////////////////
  public static List<Point3d> ToArc(Point3d zero, Vector3d x, Vector3d y, double angle, int n, bool zbool)
  {
    List<Point3d> output = new List<Point3d>();

    Vector3d x_unit = new Vector3d(x);
    x_unit.Unitize();
    if (zbool) x_unit = Vector3d.ZAxis;

    Vector3d y_unit = new Vector3d(y);
    y_unit.Unitize();

    double width = y.Length;
    double angle0 = 0.0;

    if (Math.Abs(angle) < 0.01)
    {
      for (int i = 0; i < n; i++)
      {
        angle0 = angle * (double) i / (double) (n - 1);
        Vector3d x1 = x_unit * ( width * angle0);
        Vector3d y1 = y_unit * width * (double) i / (double) (n - 1);
        output.Add(new Point3d(zero + x1 + y1));
      }
    }
    else
    {
      for (int i = 0; i < n; i++)
      {
        angle0 = angle * (double) i / (double) (n - 1);
        Vector3d x1 = x_unit * ( width / Math.Sin(angle) * (1.0 - Math.Cos(angle0)));
        Vector3d y1 = y_unit * ( width * Math.Sin(angle0) / Math.Sin(angle));
        output.Add(new Point3d(zero + x1 + y1));
      }
    }
    return output;
  }
  /////////////////////////////////////////////////////
  public static List<Point3d> ToArcDeviation(Point3d zero, Vector3d x, Vector3d y, double angle, int n, double maxDeviation, bool zbool)
  {
    List<Point3d> output = new List<Point3d>();
    Vector3d x_unit = new Vector3d(x);
    x_unit.Unitize();
    if (zbool) x_unit = Vector3d.ZAxis;

    Vector3d y_unit = new Vector3d(y);
    y_unit.Unitize();

    double width = y.Length;
    double angle0 = 0.0;
    double deviation = 1.2;
    if (Math.Abs(angle) < 0.01)
    {
      for (int i = 0; i < n; i++)
      {
        deviation = ((double) i % 2.0) * maxDeviation;
        angle0 = angle * (double) i / (double) (n - 1);
        Vector3d x1 = x_unit * (deviation * width * angle0);
        Vector3d y1 = y_unit * width * (double) i / (double) (n - 1);
        output.Add(new Point3d(zero + x1 + y1));
      }
    }
    else
    {
      double angle_max;
      if (angle > Math.PI / 2)
      {
        angle_max = Math.PI / 2;
      }
      else
      {
        if (angle < -Math.PI / 2) angle_max = -Math.PI / 2;
        else angle_max = angle;
      }

      for (int i = 0; i < n; i++)
      {
        deviation = ((double) i % 2.0) * maxDeviation;
        angle0 = angle * (double) i / (double) (n - 1);
        Vector3d x1 = x_unit * (width / Math.Sin(angle_max) * (1.0 - Math.Cos(angle0)) - deviation * Math.Cos(angle0));
        Vector3d y1 = y_unit * ( width / Math.Sin(angle_max) + deviation) * Math.Sin(angle0);
        output.Add(new Point3d(zero + x1 + y1));
      }
    }
    return output;
  }
  /////////////////////////////////////////////////////
  public static List<Point3d> ToLine(Point3d zero, Vector3d y, int n, double angle0, double radius, bool zbool)
  {
    List<Point3d> output = new List<Point3d>();
    if (zbool)
    {
      double angle;
      //y.Unitize();
      Vector3d z = Vector3d.ZAxis;

      for (int i = 0; i < n; i++)
      {
        angle = Math.Abs(angle0) * (double) i / (double) (n - 1);
        output.Add(new Point3d(zero + y * Math.Sin(angle) + z * Math.Sign(angle0) * radius * (1 - Math.Cos(angle))));
      }

    }
    else
    {
      for (int i = 0; i < n; i++)
      {
        output.Add(new Point3d(zero + y * (double) i / (double) (n - 1)));
      }
    }
    return output;

    //return ToArc(zero, new Vector3d(0.0, 0.0, 0.0), y, angle, n, zbool);
  }
  /////////////////////////////////////////////////////
  public static Mesh PointsToMesh(List < Point3d > pts1, List<Point3d> pts2, int nx, ref List<LineCurve> listUCurves, ref List<LineCurve> listVCurves)
  {
    int ny = pts1.Count;

    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();

    for (int iy = 0; iy < ny; iy++)
    {
      for (int ix = 0; ix < nx; ix++)
      {

        mesh.Vertices.Add(new Point3d(pts1[iy] + (pts2[iy] - pts1[iy]) * (double) ix / (double) (nx - 1)));
      }
    }
    int i0, i1, i2, i3;
    for (int ix = 0; ix < (nx - 1); ix++)
    {
      for (int iy = 0; iy < (ny - 1); iy++)
      {
        i0 = ix + iy * nx;
        i1 = (ix + 1) + iy * nx;
        i2 = (ix + 1) + (iy + 1) * nx;
        i3 = ix + (iy + 1) * nx;
        mesh.Faces.AddFace(i0, i1, i2, i3);
        listUCurves.Add(new LineCurve(mesh.Vertices[i0], mesh.Vertices[i1]));
        listVCurves.Add(new LineCurve(mesh.Vertices[i1], mesh.Vertices[i2]));
        listUCurves.Add(new LineCurve(mesh.Vertices[i2], mesh.Vertices[i3]));
        listVCurves.Add(new LineCurve(mesh.Vertices[i3], mesh.Vertices[i0]));
      }
    }
    mesh.Normals.ComputeNormals();
    mesh.Compact();
    return mesh;
  }
  /////////////////////////////////////////////////////
  public static Mesh PointsToMesh(List < Point3d > pts1, List<Point3d> pts2, int nx, ref DataTree<LineCurve> listUCurves, ref DataTree<LineCurve> listVCurves)
  {
    int ny = pts1.Count;

    Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();

    for (int iy = 0; iy < ny; iy++)
    {
      for (int ix = 0; ix < nx; ix++)
      {

        mesh.Vertices.Add(new Point3d(pts1[iy] + (pts2[iy] - pts1[iy]) * (double) ix / (double) (nx - 1)));
      }
    }
    int i0, i1, i2, i3;
    for (int ix = 0; ix < (nx - 1); ix++)
    {
      for (int iy = 0; iy < (ny - 1); iy++)
      {
        i0 = ix + iy * nx;
        i1 = (ix + 1) + iy * nx;
        i2 = (ix + 1) + (iy + 1) * nx;
        i3 = ix + (iy + 1) * nx;
        mesh.Faces.AddFace(i0, i1, i2, i3);
        listUCurves.Add(new LineCurve(mesh.Vertices[i0], mesh.Vertices[i1]), new GH_Path(iy));
        listVCurves.Add(new LineCurve(mesh.Vertices[i1], mesh.Vertices[i2]), new GH_Path(iy));
        listUCurves.Add(new LineCurve(mesh.Vertices[i2], mesh.Vertices[i3]), new GH_Path(iy + 1));
        listVCurves.Add(new LineCurve(mesh.Vertices[i3], mesh.Vertices[i0]), new GH_Path(iy));
      }
    }
    mesh.Normals.ComputeNormals();
    mesh.Compact();
    return mesh;
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
        List<Line> lines_in = null;
    if (inputs[0] != null)
    {
      lines_in = GH_DirtyCaster.CastToList<Line>(inputs[0]);
    }
    double tolerance = default(double);
    if (inputs[1] != null)
    {
      tolerance = (double)(inputs[1]);
    }

    List<double> width_b = null;
    if (inputs[2] != null)
    {
      width_b = GH_DirtyCaster.CastToList<double>(inputs[2]);
    }
    List<double> width_e = null;
    if (inputs[3] != null)
    {
      width_e = GH_DirtyCaster.CastToList<double>(inputs[3]);
    }
    double deviation = default(double);
    if (inputs[4] != null)
    {
      deviation = (double)(inputs[4]);
    }

    double angle = default(double);
    if (inputs[5] != null)
    {
      angle = (double)(inputs[5]);
    }

    int n_u = default(int);
    if (inputs[6] != null)
    {
      n_u = (int)(inputs[6]);
    }

    int n_v = default(int);
    if (inputs[7] != null)
    {
      n_v = (int)(inputs[7]);
    }

    bool zbool = default(bool);
    if (inputs[8] != null)
    {
      zbool = (bool)(inputs[8]);
    }



    //3. Declare output parameters
      object P = null;
  object LP = null;
  object PP = null;
  object PL = null;
  object lines = null;
  object ends = null;
  object connect = null;
  object brep = null;
  object M = null;
  object F = null;
  object U = null;
  object V = null;


    //4. Invoke RunScript
    RunScript(lines_in, tolerance, width_b, width_e, deviation, angle, n_u, n_v, zbool, ref P, ref LP, ref PP, ref PL, ref lines, ref ends, ref connect, ref brep, ref M, ref F, ref U, ref V);
      
    try
    {
      //5. Assign output parameters to component...
            if (P != null)
      {
        if (GH_Format.TreatAsCollection(P))
        {
          IEnumerable __enum_P = (IEnumerable)(P);
          DA.SetDataList(1, __enum_P);
        }
        else
        {
          if (P is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(1, (Grasshopper.Kernel.Data.IGH_DataTree)(P));
          }
          else
          {
            //assign direct
            DA.SetData(1, P);
          }
        }
      }
      else
      {
        DA.SetData(1, null);
      }
      if (LP != null)
      {
        if (GH_Format.TreatAsCollection(LP))
        {
          IEnumerable __enum_LP = (IEnumerable)(LP);
          DA.SetDataList(2, __enum_LP);
        }
        else
        {
          if (LP is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(2, (Grasshopper.Kernel.Data.IGH_DataTree)(LP));
          }
          else
          {
            //assign direct
            DA.SetData(2, LP);
          }
        }
      }
      else
      {
        DA.SetData(2, null);
      }
      if (PP != null)
      {
        if (GH_Format.TreatAsCollection(PP))
        {
          IEnumerable __enum_PP = (IEnumerable)(PP);
          DA.SetDataList(3, __enum_PP);
        }
        else
        {
          if (PP is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(3, (Grasshopper.Kernel.Data.IGH_DataTree)(PP));
          }
          else
          {
            //assign direct
            DA.SetData(3, PP);
          }
        }
      }
      else
      {
        DA.SetData(3, null);
      }
      if (PL != null)
      {
        if (GH_Format.TreatAsCollection(PL))
        {
          IEnumerable __enum_PL = (IEnumerable)(PL);
          DA.SetDataList(4, __enum_PL);
        }
        else
        {
          if (PL is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(4, (Grasshopper.Kernel.Data.IGH_DataTree)(PL));
          }
          else
          {
            //assign direct
            DA.SetData(4, PL);
          }
        }
      }
      else
      {
        DA.SetData(4, null);
      }
      if (lines != null)
      {
        if (GH_Format.TreatAsCollection(lines))
        {
          IEnumerable __enum_lines = (IEnumerable)(lines);
          DA.SetDataList(5, __enum_lines);
        }
        else
        {
          if (lines is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(5, (Grasshopper.Kernel.Data.IGH_DataTree)(lines));
          }
          else
          {
            //assign direct
            DA.SetData(5, lines);
          }
        }
      }
      else
      {
        DA.SetData(5, null);
      }
      if (ends != null)
      {
        if (GH_Format.TreatAsCollection(ends))
        {
          IEnumerable __enum_ends = (IEnumerable)(ends);
          DA.SetDataList(6, __enum_ends);
        }
        else
        {
          if (ends is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(6, (Grasshopper.Kernel.Data.IGH_DataTree)(ends));
          }
          else
          {
            //assign direct
            DA.SetData(6, ends);
          }
        }
      }
      else
      {
        DA.SetData(6, null);
      }
      if (connect != null)
      {
        if (GH_Format.TreatAsCollection(connect))
        {
          IEnumerable __enum_connect = (IEnumerable)(connect);
          DA.SetDataList(7, __enum_connect);
        }
        else
        {
          if (connect is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(7, (Grasshopper.Kernel.Data.IGH_DataTree)(connect));
          }
          else
          {
            //assign direct
            DA.SetData(7, connect);
          }
        }
      }
      else
      {
        DA.SetData(7, null);
      }
      if (brep != null)
      {
        if (GH_Format.TreatAsCollection(brep))
        {
          IEnumerable __enum_brep = (IEnumerable)(brep);
          DA.SetDataList(8, __enum_brep);
        }
        else
        {
          if (brep is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(8, (Grasshopper.Kernel.Data.IGH_DataTree)(brep));
          }
          else
          {
            //assign direct
            DA.SetData(8, brep);
          }
        }
      }
      else
      {
        DA.SetData(8, null);
      }
      if (M != null)
      {
        if (GH_Format.TreatAsCollection(M))
        {
          IEnumerable __enum_M = (IEnumerable)(M);
          DA.SetDataList(9, __enum_M);
        }
        else
        {
          if (M is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(9, (Grasshopper.Kernel.Data.IGH_DataTree)(M));
          }
          else
          {
            //assign direct
            DA.SetData(9, M);
          }
        }
      }
      else
      {
        DA.SetData(9, null);
      }
      if (F != null)
      {
        if (GH_Format.TreatAsCollection(F))
        {
          IEnumerable __enum_F = (IEnumerable)(F);
          DA.SetDataList(10, __enum_F);
        }
        else
        {
          if (F is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(10, (Grasshopper.Kernel.Data.IGH_DataTree)(F));
          }
          else
          {
            //assign direct
            DA.SetData(10, F);
          }
        }
      }
      else
      {
        DA.SetData(10, null);
      }
      if (U != null)
      {
        if (GH_Format.TreatAsCollection(U))
        {
          IEnumerable __enum_U = (IEnumerable)(U);
          DA.SetDataList(11, __enum_U);
        }
        else
        {
          if (U is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(11, (Grasshopper.Kernel.Data.IGH_DataTree)(U));
          }
          else
          {
            //assign direct
            DA.SetData(11, U);
          }
        }
      }
      else
      {
        DA.SetData(11, null);
      }
      if (V != null)
      {
        if (GH_Format.TreatAsCollection(V))
        {
          IEnumerable __enum_V = (IEnumerable)(V);
          DA.SetDataList(12, __enum_V);
        }
        else
        {
          if (V is Grasshopper.Kernel.Data.IGH_DataTree)
          {
            //merge tree
            DA.SetDataTree(12, (Grasshopper.Kernel.Data.IGH_DataTree)(V));
          }
          else
          {
            //assign direct
            DA.SetData(12, V);
          }
        }
      }
      else
      {
        DA.SetData(12, null);
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