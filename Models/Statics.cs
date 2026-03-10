using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace Nat.Rpt.Models
{
    public class Statics
    {
        public static string Compress(byte[] _byteArray)
        {
            //Prepare for compress
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            System.IO.Compression.GZipStream sw = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress);

            //Compress
            sw.Write(_byteArray, 0, _byteArray.Length);
            //Close, DO NOT FLUSH cause bytes will go missing...
            sw.Close();

            //Transform byte[] zip data to string
            _byteArray = ms.ToArray();
            System.Text.StringBuilder sB = new System.Text.StringBuilder(_byteArray.Length);
            foreach (byte item in _byteArray)
            {
                sB.Append((char)item);
            }
            ms.Close();
            sw.Dispose();
            ms.Dispose();
            return Convert.ToBase64String(_byteArray);
        }
        public static string RemoveNameSpace(string vivstrXml)
        {
            XmlDocument lioXmlDocumentFrom = new XmlDocument();
            lioXmlDocumentFrom.LoadXml(vivstrXml.ToString());
            XmlDocument lioXmlDocumentTo = new XmlDocument();
            XmlNode lioXmlNode = RecursiveRemoveNameSpace(lioXmlDocumentTo, lioXmlDocumentFrom);
            lioXmlDocumentTo.AppendChild(lioXmlNode);
            return (lioXmlDocumentTo.OuterXml);
        }
        private static System.Xml.XmlElement RecursiveRemoveNameSpace(XmlDocument vioXmlDocument, XmlNode vioXmlChildNode)
        {
            string livNewElementNamespace = (vioXmlChildNode.NamespaceURI != String.Empty ? String.Empty : String.Empty);
            XmlElement lioXmlElement = vioXmlDocument.CreateElement(vioXmlChildNode.LocalName, livNewElementNamespace);
            if (vioXmlChildNode.ChildNodes != null)
                foreach (System.Xml.XmlNode lioXmlNode in vioXmlChildNode.ChildNodes)
                {
                    if (lioXmlNode is System.Xml.XmlElement)
                        lioXmlElement.AppendChild(RecursiveRemoveNameSpace(vioXmlDocument, (System.Xml.XmlElement)lioXmlNode));
                    else
                        lioXmlElement.InnerXml = vioXmlChildNode.InnerXml;
                }
            if (vioXmlChildNode.Attributes != null)
                foreach (XmlAttribute lioXmlAttribute in vioXmlChildNode.Attributes)
                {
                    if (lioXmlAttribute.Name != "xmlns" && lioXmlAttribute.Name != "xmlns:xsi" && lioXmlAttribute.Name != "xmlns:xsd")
                    {
                        XmlAttribute lioXmlAttributeNew = vioXmlDocument.CreateAttribute(lioXmlAttribute.Name);
                        lioXmlAttributeNew.Value = lioXmlAttribute.Value;
                        lioXmlElement.Attributes.Append(lioXmlAttributeNew);
                    }
                }
            return lioXmlElement;
        }

    }
}
