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
                string var = string.Format("${1}{0}{2}", n, "{", "}");
                input = input.Replace(var, _vars[n] != null ? _vars[n].ToString() : null);
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

            Log.DebugFormat("Variable: {0} = {1}", name, value);
            Set(name, value);

            return value;
        }
    }
}
