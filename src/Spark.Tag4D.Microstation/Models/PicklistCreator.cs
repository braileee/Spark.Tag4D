using Bentley.DgnPlatformNET;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Models
{
    public class PicklistCreator
    {
        public PicklistCreator(DgnFile dgnFile, List<PicklistGroup> picklistGroups)
        {
            DgnFile = dgnFile;
            PicklistGroups = picklistGroups;
        }

        public DgnFile DgnFile { get; }
        public List<PicklistGroup> PicklistGroups { get; } = new List<PicklistGroup>();

        public List<PickList> Create()
        {
            // Load or create the picklist library in the file
            PickListLibrary library = PickListLibrary.Create();

            List<PickList> pickLists = new List<PickList>();

            foreach (PicklistGroup picklistGroup in PicklistGroups)
            {
                PickList pickList = library.AddPickList(picklistGroup.GroupName, DgnFile);

                foreach (var value in picklistGroup.Values)
                {
                    pickList.AddValue(value);
                }

                pickLists.Add(pickList);
            }

            PickListLibrary.SavePickListLibToDgn(DgnFile, library, isUpdateRelatedObjects: true);

            return pickLists;
        }
    }
}
