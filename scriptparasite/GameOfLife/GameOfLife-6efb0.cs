
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
namespace ns6efb0
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
        private void RunScript(object ticker, List<bool> cells, int rows, int columns, List<int> birthRule, List<int> survivalRule, bool run, bool reset, ref object previous, ref object newIteration, ref object A)
        {
            //Sanity
            if (run == false)
            {
                return;
            }

            if (cells.Count != rows * columns)
            {
                Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "something weird with dimension of grid");
                return;
            }

            if (reset == true)
            {
                //turn list into 2d array
                prevCells = new bool[columns, rows];
                int it = 0;

                for (int j = 0; j < columns; j++)
                {
                    for (int i = 0; i < rows; i++)
                    {
                        prevCells[j, i] = cells[it];
                        it++;
                    }
                }

            }

            //new array of alive/dead cells for next iteration
            // bool[,] prevCells = new bool[columns, rows];
            // bool[,] newCells = cellArray;

            // bool[,] newCells = new bool[columns, rows];
            int[,] nCount = new int[columns, rows];
            // for (int k = 0; k < iterations; k++)
            // {
            // switch previous cells with new ones
            // prevCells = newCells;
            bool[,] newCells = new bool[columns, rows];

            for (int j = 0; j < columns; j++)
            {
                for (int i = 0; i < rows; i++)
                {

                    //compute alive neighbors
                    int aliveNeighbors = 0;
                    int ni, nj;

                    // topleft
                    ni = i + 1;
                    nj = j - 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // top
                    ni = i + 1;
                    nj = j;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // topright
                    ni = i + 1;
                    nj = j + 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // left
                    ni = i;
                    nj = j - 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // right
                    ni = i;
                    nj = j + 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // bottomleft
                    ni = i - 1;
                    nj = j - 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // bottom
                    ni = i - 1;
                    nj = j;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // bottomright
                    ni = i - 1;
                    nj = j + 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if (prevCells[nj, ni] == true) aliveNeighbors++;

                    // Print("alive neighbors: " + aliveNeighbors);
                    nCount[j, i] = aliveNeighbors;

                    //compute new state
                    bool prevState = prevCells[j, i];
                    bool newState = false;

                    //if cells were dead
                    if (prevState == false)
                    {
                        foreach (int b in birthRule)
                        {
                            if (b == aliveNeighbors)
                            {
                                newState = true;
                                break;
                            }
                        }
                    }

                    //if cell was alive
                    else
                    {
                        foreach (int s in survivalRule)
                        {
                            if (s == aliveNeighbors)
                            {
                                newState = true;
                                break;
                            }
                        }
                    }

                    //store new state in array
                    newCells[j, i] = newState;
                }
                // }
            }

            // outputs
            previous = prevCells;
            newIteration = newCells;
            A = nCount;
            //Print("end of script");
            // updateCount++;
            // B = updateCount;

            // store new cells for next iteration
            prevCells = newCells;
        }
        #endregion

        #region Additional
        bool[,] prevCells;
        // int updateCount;
        #endregion
    }
}
