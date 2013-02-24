using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernMinas.Update.Api.Resolvers
{
    [ResolverName("var")]
    public class VariableResolver : ResolverBase
    {
        #region Static

        private static Dictionary<string, object> _vars = new Dictionary<string, object>();

        public static void Clear()
        {
            _vars.Clear();
        }

        public static void Set(string name, object value)
        {
            if (_vars.ContainsKey(name))
                _vars.Add(name, value);
            else
                _vars[name] = value;
        }

        public static object Get(string name)
        {
            return _vars.ContainsKey(name) ? _vars[name] : null;
        }

        public static void Unset(string name)
        {
            if (_vars.ContainsKey(name))
                _vars.Remove(name);
        }

        public static string ExpandInternal(string input)
        {
            foreach(string n in _vars.Keys)
            {
                input = input.Replace(string.Format("${1}{0}{2}", n, "{", "}"), _vars[n] != null ? _vars[n].ToString() : null);
            }
            return input;
        }

        #endregion

        public override string ResolveToString()
        {
            if (resolverNode.SelectSingleNode("child::name") == null)
                throw new InvalidOperationException();

            string name = resolverNode.SelectSingleNode("child::name").InnerText;
            string value = Expand(Input.ToString());

            Set(name, value);

            return value;
        }
    }
}
