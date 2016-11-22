using System;
using System.Collections;

namespace PCLExt.Lua
{
    /// <summary>
    /// 
    /// </summary>
    public static class Lua
    {
        private static Exception NotImplementedInReferenceAssembly() =>
            new NotImplementedException(@"This functionality is not implemented in the portable version of this assembly.
You should reference the PCLExt.Lua NuGet package from your main application project in order to reference the platform-specific implementation.");


        public static LuaScript CreateLuaScript(string luaScriptName = "")
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            return new MoonLua(luaScriptName);
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static LuaTable CreateTable(LuaScript luaScript, string tableName)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            return new MoonLuaTable(luaScript, tableName);
#endif

            throw NotImplementedInReferenceAssembly();
        }
        public static LuaTable CreateTable(LuaScript luaScript, string tableName, IList list)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            return new MoonLuaTable(luaScript, tableName, list);
#endif

            throw NotImplementedInReferenceAssembly();
        }

        /// <summary>
        /// Converts a Lua Table to LuaTable
        /// </summary>
        public static LuaTable ToLuaTable(object obj)
        {
#if DESKTOP || ANDROID || __IOS__ || MAC
            var dynValue = obj as MoonSharp.Interpreter.DynValue;
            if (dynValue?.Table != null)
                obj = dynValue.Table;
            

            var table = obj as MoonSharp.Interpreter.Table;
            if (table != null)
                return new MoonLuaTable(table);


            return null;
#endif

            throw NotImplementedInReferenceAssembly();
        }

        public static void RegisterCustomFunc(string name, object function) => LuaScript.RegisterCustomFunc(name, function);

        public static void RegisterModule(string name) => LuaScript.RegisterModule(name);
    }
}