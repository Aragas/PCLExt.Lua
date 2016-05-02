using System.Collections.Generic;

namespace PCLExt.Lua
{
    public abstract class LuaScript
    {
        protected static Dictionary<string, object> CustomContext { get; } = new Dictionary<string, object>();
        public static void RegisterCustomFunc(string name, object function) { CustomContext.Add(name, function); }

        protected static List<string> CustomModules { get; } = new List<string>();
        public static void RegisterModule(string name) { CustomModules.Add(name); }

        public abstract bool ReloadFile();

        public abstract object this[string index] { get; set; }

        public abstract object[] CallFunction(string functionName, params object[] args);
    }

    public abstract class LuaTable
    {
        public abstract object this[object field] { get; set; }
        public abstract object this[string field] { get; set; }

        public abstract object[] CallFunction(string functionName, params object[] args);

        public abstract Dictionary<object, object> ToDictionary();

        public abstract List<object> ToList();
        public abstract object[] ToArray();
    }
}