using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ModernMinas.Update.Api
{
    public class Option
    {
        public string Value { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }

        internal Option(XmlNode optionXmlNode)
        {
            this.Title = optionXmlNode.InnerText;
            this.Value = optionXmlNode.Attributes["value"].Value;

            var descriptionAttribute = optionXmlNode.Attributes["description"];
            if (descriptionAttribute == null)
                this.Description = string.Empty;
            else
                this.Description = descriptionAttribute.Value;

            var defaultAttribute = optionXmlNode.Attributes["default"];
            if (defaultAttribute == null)
                this.IsDefault = false;
            else
                this.IsDefault = bool.Parse(defaultAttribute.Value);
        }
    }
}
