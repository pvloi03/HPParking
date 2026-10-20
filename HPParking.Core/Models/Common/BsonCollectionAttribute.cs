using System;

namespace HPParking.Core.Models.Common
{
    /// <summary>
    /// Chỉ định tên Collection tùy chỉnh trong cơ sở dữ liệu MongoDB cho Entity (thay vì mặc định Pluralize)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class BsonCollectionAttribute : Attribute
    {
        public string CollectionName { get; }

        public BsonCollectionAttribute(string collectionName)
        {
            CollectionName = collectionName;
        }
    }
}
