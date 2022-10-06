using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace nuge.Utils
{
    public class XmlReadHelper
    {


        public static XmlNode GetDocument(string xmlContent)
        {
            if (!string.IsNullOrEmpty(xmlContent) )
            {
                XmlDocument doc = new XmlDocument();

                doc.LoadXml(xmlContent);

                return doc;
            }

            return null;
        }
        public string FindValueFor(XmlNode node, string nodeName)
        {
            return ExtractValues(node, nodeName).FirstOrDefault();
        }


        public List<Dictionary<string, string>> ExtractMappedNodeData(XmlNode node, string nodeName)
        {
            return ExtractData(node, nodeName, MapNodeValues);
        }

        public List<string> ExtractValues(XmlNode node, string nodeName)
        {
            return ExtractData(node, nodeName, n => n.InnerText);
        }

        public List<T> ExtractData<T>(XmlNode node, string nodeName, Func<XmlNode, T> evaluator)
        {
            var result = new List<T>();

            SearchNodes(node, nodeName, evaluator, result);

            return result;
        }


        private void SearchNodes<T>(XmlNode node, string nodeName, Func<XmlNode, T> evaluator, List<T> result)
        {
            if (node.Name == nodeName)
            {
                var name = node.InnerText;

                result.Add(evaluator(node));
            }
            else
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    SearchNodes(child, nodeName, evaluator, result);
                }
            }
        }


        private Dictionary<string, string> MapNodeValues(XmlNode node)
        {
            var result = new Dictionary<string, string>();

            result.Add(node.Name, node.InnerText);

            foreach (var attribute in node.Attributes)
            {
                if (attribute is XmlNode xAtt)
                {
                    result.Add(xAtt.Name, xAtt.InnerText);
                }
            }

            return result;
        }
    }
}