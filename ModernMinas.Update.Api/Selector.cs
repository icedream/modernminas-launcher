using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModernMinas.Update.Api
{
    public class Selector
    {
        private int _currentIndex = 0;
        public int CurrentIndex { get { return _currentIndex; } set { if (_currentIndex >= AvailableOptions.Count) throw new InvalidOperationException("Index out of range."); _currentIndex = value; } }
        public Option CurrentOption { get { return AvailableOptions[CurrentIndex]; } }
        public List<Option> AvailableOptions { get; private set; }

        public Selector(XmlNode selectorXmlNode)
        {
            AvailableOptions = new List<Option>();
            foreach (XmlNode optionXmlNode in selectorXmlNode.SelectNodes("child::choice").OfType<XmlNode>())
            {
                var d = new Option(optionXmlNode);
                if (d.IsDefault)
                    _currentIndex = AvailableOptions.Count;
                AvailableOptions.Add(d);
            }
        }
    }
}
