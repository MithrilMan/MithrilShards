using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace Xunit
{
   public class JsonFileDataAttribute : DataAttribute
   {
      class DataSet
      {
         public IEnumerable<string> Fields { get; set; }

         public IEnumerable<DataSetEntry> Data { get; set; }
      }

      class DataSetEntry
      {
         public IEnumerable<object> Items { get; set; }
         public string Comment { get; set; }
      }

      private readonly string _filePath;
      private readonly string _propertyName;

      /// <summary>
      /// Load data from a JSON file as the data source for a theory
      /// </summary>
      /// <param name="filePath">The absolute or relative path to the JSON file to load</param>
      public JsonFileDataAttribute(string filePath)
          : this(filePath, null) { }

      /// <summary>
      /// Load data from a JSON file as the data source for a theory
      /// </summary>
      /// <param name="filePath">The absolute or relative path to the JSON file to load</param>
      /// <param name="propertyName">The name of the property on the JSON file that contains the data for the test</param>
      public JsonFileDataAttribute(string filePath, string propertyName)
      {
         _filePath = filePath;
         _propertyName = propertyName;
      }

      /// <inheritDoc />
      public override IEnumerable<object[]> GetData(MethodInfo testMethod)
      {
         if (testMethod == null) { throw new ArgumentNullException(nameof(testMethod)); }

         // Get the absolute path to the JSON file
         var path = Path.IsPathRooted(_filePath)
             ? _filePath
             : Path.GetRelativePath(Directory.GetCurrentDirectory(), _filePath);

         if (!File.Exists(path))
         {
            throw new ArgumentException($"Could not find file at path: x {path}");
         }

         // Load the file
         var fileData = File.ReadAllText(_filePath);

         if (string.IsNullOrEmpty(_propertyName))
         {
            //whole file is the data
            return JsonConvert.DeserializeObject<List<object[]>>(fileData);
         }

         // Only use the specified property as the data
         JObject allData = JObject.Parse(fileData);
         JToken data = allData[_propertyName];


         DataSet dataSet = data.ToObject<DataSet>();

         return dataSet.Data.Select(entry => entry.Items.ToArray()).ToList();
      }

      public override bool Match(object obj)
      {
         return base.Match(obj);
      }
   }
}
