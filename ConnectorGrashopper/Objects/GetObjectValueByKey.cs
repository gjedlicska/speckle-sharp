﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConnectorGrashopper.Objects
{
    public class GetObjectValueByKey: SelectKitComponentBase
    {
        public GetObjectValueByKey() : base("Speckle Object Value by Key", "Object K/V", "Gets the value of a specific key in a Speckle object.", "Speckle 2", "Object Management")
        {
        }
        protected override Bitmap Icon => Properties.Resources.GetObjectValueByKey;
        
        public override GH_Exposure Exposure => GH_Exposure.secondary;
        
        public override Guid ComponentGuid => new Guid("5750B5B2-FB1E-4A1C-9DBE-025D6615D64E");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "K", "List of keys", GH_ParamAccess.item);
            pManager.AddGenericParameter("Key", "K", "List of keys", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Value", "V", "Speckle object", GH_ParamAccess.item);
            pManager.AddGenericParameter("debug", "d", "debug Speckle object", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize local variables
            var speckleObj = new GH_SpeckleBase();
            var key = "";

            // Get data from inputs
            if (!DA.GetData(0, ref speckleObj)) return;
            if (!DA.GetData(1, ref key)) return;
            
            var b = speckleObj.Value;
            if (b == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,"Object is not a Speckle object");
                return;
            }
            // Get the value and output.
            var value = b[key];
            if (value == null) value = b["@" + key];

            if (value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,"Key not found in object: " + key);
                return;
            }
            // Convert and add to corresponding output structure
            if (value is List<object> list)
            {
                var converted = list.Select(
                    item => new GH_ObjectWrapper(TryConvertItem(item)));
                DA.SetDataList(0, converted);
            }
            else
                DA.SetData(0, new GH_ObjectWrapper(TryConvertItem(value)));
        }
    }
}