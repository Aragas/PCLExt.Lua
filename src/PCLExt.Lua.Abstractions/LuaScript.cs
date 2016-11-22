using System.Collections.Generic;

namespace PCLExt.Lua
{
    public abstract class LuaScript
    {
        protected static Dictionary<string, object> CustomContext { get; } = new Dictionary<string, object>();
        public static void RegisterCustomFunc(string name, object function) { if(!CustomContext.ContainsKey(name)) CustomContext.Add(name, function); }

        protected static List<string> CustomModules { get; } = new List<string>();
        public static void RegisterModule(string name) { if(!CustomModules.Contains(name)) CustomModules.Add(name); }


        public abstract object this[string index] { get; set; }

        public abstract object[] CallFunction(string functionName, params object[] args);

        public abstract bool ReloadFile();
    }
}