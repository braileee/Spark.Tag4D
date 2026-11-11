using Bentley.DgnPlatformNET;
using Spark.Tag4D.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Tag4D.Microstation.Models
{
    public class ItemTypeCreator
    {
        public ItemTypeCreator(DgnFile dgnFile, string libraryName, List<PropertySetItem> propertySetItems, List<PickList> pickLists)
        {
            LibraryName = libraryName;
            PropertySetItems = propertySetItems;
            PickLists = pickLists;
            DgnFile = dgnFile;
        }

        public string LibraryName { get; }
        public List<PropertySetItem> PropertySetItems { get; } = new List<PropertySetItem>();
        public List<PickList> PickLists { get; } = new List<PickList>();
        public DgnFile DgnFile { get; }

        public List<ItemType> Create()
        {
            // 1. Library
            ItemTypeLibrary library = GetOrCreateLibrary(DgnFile, nameof(Spark.Tag4D.Microstation));

            // 2. Item Type
            List<ItemType> itemTypes = new List<ItemType>();

            foreach (PropertySetItem propertySetItem in PropertySetItems)
            {
                ItemType itemType = GetOrCreateItemType(library, propertySetItem.PropertySetName);

                if (itemType == null)
                {
                    continue;
                }

                itemTypes.Add(itemType);

                // 3. Properties
                foreach (var property in propertySetItem.PropertyItems)
                {
                    var typeKind = TypeKindMapper.Get(property.DataType);
                    CustomProperty customProperty = AddProperty(itemType, property.PropertyName, typeKind);

                    PickList pickList = PickLists.FirstOrDefault(item => item.Name == property.PropertyName);

                    if (pickList != null)
                    {
                        customProperty.PickListSource = "Dgn Files (*.dgn, *.dgnlib)";
                        customProperty.PickListName = property.PropertyName;
                        customProperty.PickListSettings = property.PropertyName;
                    }
                }
            }

            // 4. Save library
            library.Write();

            return itemTypes;
        }

        private ItemTypeLibrary GetOrCreateLibrary(DgnFile dgnFile, string libraryName)
        {
            ItemTypeLibrary library = ItemTypeLibrary.FindByName(libraryName, dgnFile);

            if (library != null)
                return library;

            library = ItemTypeLibrary.Create(
                libraryName,
                dgnFile);

            library.Write();

            return library;
        }

        private ItemType GetOrCreateItemType(Bentley.DgnPlatformNET.ItemTypeLibrary library, string itemTypeName)
        {
            ItemType itemType = library.GetItemTypeByName(itemTypeName);
            if (itemType != null)
                return itemType;

            itemType = library.AddItemType(itemTypeName);
            return itemType;
        }

        private CustomProperty AddProperty(Bentley.DgnPlatformNET.ItemType itemType, string propertyName, Bentley.DgnPlatformNET.CustomProperty.TypeKind typeKind)
        {
            if (itemType.GetPropertyByName(propertyName) != null)
                return itemType.GetPropertyByName(propertyName);

            CustomProperty prop = itemType.AddProperty(propertyName);
            prop.Type = typeKind;

            return prop;
        }
    }
}
