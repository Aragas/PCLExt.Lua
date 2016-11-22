using System.Collections.Generic;

namespace PCLExt.Lua
{
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