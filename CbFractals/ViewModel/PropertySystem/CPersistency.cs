using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using System.IO;
using CbFractals.Tools;
using System.Xml;

namespace CbFractals.ViewModel.PropertySystem
{

    [XmlType("ValueBase")]
    public abstract class CXmlValueBase
    {
        [XmlAttribute("Path")]
        public string Path;

        [XmlAttribute("Value")]
        public string Value;

        [XmlAttribute("Comment")]
        public string Comment;
    }

    [XmlType("Value")]
    public sealed class CXmlValue : CXmlValueBase
    {
    }

    [XmlType("SelectedValueSource")]
    public sealed class CXmlSelectedValueSource : CXmlValueBase
    {
    }

    [XmlRoot("Progression", IsNullable = false)]
    public sealed class CXmlProgressionSnapshot
    {
        [XmlArray("Values")]
        public CXmlValueBase[] Values;

        /*
         <ValueNodes>
             <Data Path="a|b|c" Value="0.0" />
             <SelectedValueSource Path="parameter1" Value="Guid"/>            
         </ValueNodes>
          */

        private const char GuidSeperator = '/';
        private const char CommentSeperator = '.';
        private static string GetPersistentPath(IEnumerable<CNameEnum> aNames)
            => string.Join(GuidSeperator, from aName in aNames select aName.GetGuidAttribute().Guid.ToString());
        private static string GetComment(IEnumerable<CNameEnum> aNames)
            => string.Join(CommentSeperator, from aName in aNames select aName.ToString());

        private static XmlSerializer NewXmlSerializer()
        {
            var aExtraTypes = new Type[] { typeof(CXmlValueBase), typeof(CXmlValue), typeof(CXmlSelectedValueSource) };
            var aSerializer = new XmlSerializer(typeof(CXmlProgressionSnapshot), aExtraTypes);
            return aSerializer;
        }

        internal static void Save(CParameters aParameters, FileInfo aFileInfo)
        {
            var aPath = new List<CNameEnum>();
            var aXmlValues = new List<CXmlValueBase>();
            foreach (var aValueNode in aParameters.StoredNodes)
            {
                FillXmlValues(aValueNode, aPath, aXmlValues);
            }
            var aXmlProgressionSnapshot = new CXmlProgressionSnapshot
            {
                Values = aXmlValues.ToArray(),
            };
            var aXmlSerializer = NewXmlSerializer();
            aFileInfo.Directory.Create();
            if (aFileInfo.Exists)
                aFileInfo.Delete();
            using var aFileStream = File.OpenWrite(aFileInfo.FullName);
            var aXmlWriter = XmlWriter.Create(aFileStream);
            aXmlSerializer.Serialize(aXmlWriter, aXmlProgressionSnapshot);
            aXmlWriter.Flush();
            aFileStream.Flush();
            aFileStream.SetLength(aFileStream.Position);
        }

        private static void FillXmlValues(CValueNode aValueNode, List<CNameEnum> aPath, List<CXmlValueBase> aXmlValues)
        {
            aPath.Add(aValueNode.Name);
            try
            {
                var aSelectedValueSource = aValueNode.ValueSource;
                if (aSelectedValueSource is object)
                {
                    var aXmlSelectedValueSource = new CXmlSelectedValueSource()
                    {
                        Path = GetPersistentPath(aPath),
                        Comment = GetComment(aPath),
                        Value = aSelectedValueSource.Name.GetGuidAttribute().Guid.ToString()
                    };
                    aXmlValues.Add(aXmlSelectedValueSource);
                }
                if (aValueNode.IsData)
                {
                    var aXmlValue = new CXmlValue()
                    {
                        Path = GetPersistentPath(aPath),
                        Comment = GetComment(aPath),
                        Value = aValueNode.StoredData,
                    };
                    aXmlValues.Add(aXmlValue);
                }
                foreach (var aDataNode in aValueNode.StoredNodes)
                {
                    FillXmlValues(aDataNode, aPath, aXmlValues);
                }
            }
            finally
            {
                aPath.RemoveAt(aPath.Count - 1);
            }
        }

    }

}
