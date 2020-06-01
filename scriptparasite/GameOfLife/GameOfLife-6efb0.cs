
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
        private void RunScript(List<bool> cells, int rows, int columns, List<int> birthRule, List<int> survivalRule, bool run, bool reset, ref object A)
        {
            //Sanity
            if (run == false) {
                return;
            }

            if (cells.Count != rows * columns) {
                Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "something weird with dimension of grid");
                return;
            }



            //turn list into 2d array
            bool[,] cellArray = new bool[columns, rows];
            int it = 0;

            for (int j = 0; j < columns; j++) {
                for (int i = 0; i < rows; i++) {
                    cellArray[j, i] = cells[it];
                    it++;
                }
            }
            
            //new array of alive/dead cells for next iteration
            
            bool[,] newCells = new bool[columns, rows];
            for (int j = 0; j < columns; j++) {
                for (int i = 0; i < rows; i++) {
                    bool prevState = cellArray[j, i];
                    
                    //compute alive neighbors
                    int aliveNeighbors = 0;
                    int ni, nj;

                    // topleft
                    ni = i + 1;
                    nj = i - 1;
                    if (ni > rows - 1) ni = 0;
                    else if (ni < 0) ni = rows - 1;
                    if (nj > columns - 1) nj = 0;
                    else if (nj < 0) nj = columns - 1;
                    if(cellArray[nj, ni] == true) aliveNeighbors++;
                }
            }
            
            
            A = cellArray;
            //Print("end of script");
        }
        #endregion

        #region Additional

        #endregion
    }
}
