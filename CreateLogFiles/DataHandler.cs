using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Avenger
{
    public class DataHandler
    {
        private XDocument xmlDocument;

        public  void CreateNewXdocumentByString(string xmlData)
        {
           try
            {
                xmlDocument = XDocument.Parse(xmlData);
            }
            catch(Exception e)
            {
            }
        }

        public Dictionary<string, string> GetFieldsAndValuesFromXml()
        {
           XElement whereNode = (from element in xmlDocument.Root.Elements().Elements()
                                  where element.Name == "Where"
                                  select element).FirstOrDefault();
            return whereNode.Elements().ToDictionary(property => (string)property.Attribute("Name"),
                                                     property => (string)property.Attribute("Value"));

        }
    }
}
