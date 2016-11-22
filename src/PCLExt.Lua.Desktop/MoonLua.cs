using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop.RegistrationPolicies;
using MoonSharp.Interpreter.Loaders;

using PCLExt.FileStorage;


namespace PCLExt.Lua
{
    internal class FileSystemScriptLoader : IScriptLoader
    {
        private static IFolder Modules => Storage.LuaFolder.CreateFolderAsync("modules", CreationCollisionOption.OpenIfExists).Result;

        public object LoadFile(string file, Table globalContext)
        {
            if (file.StartsWith("module_"))
            {
                using (var stream = Storage.LuaFolder.GetFileAsync(file).Result.OpenAsync(FileStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
            else
            {
                using (var stream = Modules.GetFileAsync(file).Result.OpenAsync(FileStorage.FileAccess.Read).Result)
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public string ResolveFileName(string filename, Table globalContext) => $"{filename}.lua";

        public string ResolveModuleName(string modname, Table globalContext) => $"module_{modname}";
    }

    public class MoonLua : LuaScript
    {
        public static implicit operator Script(MoonLua moonLua) => moonLua.LuaScript;
        static MoonLua()
        {
            UserData.RegistrationPolicy = new AutomaticRegistrationPolicy();
            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<LuaTable>(
                (script, obj) => DynValue.NewTable((MoonLuaTable) obj));
            Script.GlobalOptions.CustomConverters.SetClrToScriptCustomConversion<MoonLuaTable>(
                (script, obj) => DynValue.NewTable(obj));
        }
        

        private string LuaName { get; }
        private Script LuaScript { get; }

        public MoonLua(string luaName = "", bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Script();
            LuaScript.Options.ScriptLoader = new FileSystemScriptLoader();


            // Register custom modules that we allow to use.
            foreach (var module in CustomModules)
                RegisterModule(module);
            
            RegisterCustom(LuaScript.Globals);


            if (instantInit)
                ReloadFile();
        }
        public MoonLua(string luaName, string[] modules, bool instantInit = false)
        {
            LuaName = luaName;

            LuaScript = new Script();
            LuaScript.Options.ScriptLoader = new FileSystemScriptLoader();


            // Register custom modules that we allow to use.
            foreach (var module in modules)
                RegisterModule(module);

            RegisterCustom(LuaScript.Globals);


            if (instantInit)
                ReloadFile();
        }


        public override object this[string fullPath] { get { return LuaScript.Globals[fullPath]; } set { LuaScript.Globals[fullPath] = value; } }

        public override object[] CallFunction(string functionName, params object[] args)
        {
            var ret = LuaScript.Call(LuaScript.Globals[functionName], args).Tuple;
            return ret?.Any() == true ? ret.Select(dynVal => dynVal.ToObject()).ToArray() : new object[0];
        }

        public override bool ReloadFile()
        {
            using (var stream = Storage.LuaFolder.GetFileAsync(LuaName).Result.OpenAsync(FileStorage.FileAccess.Read).Result)
            using (var reader = new StreamReader(stream))
            {
                var code = reader.ReadToEnd();
                LuaScript.DoString(code);
                return true;
            }
        }


        private Table CompileFile(string path)
        {
            var modules = CoreModules.Preset_SoftSandbox;
            var folder = FileSystem.Current.BaseStorage.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var file = Path.GetFileName(path);
            var text = folder.GetFileAsync(file).Result.ReadAllTextAsync().Result;

            var table = RegisterCustom(new Table(LuaScript).RegisterCoreModules(modules));
            LuaScript.DoString(text, table);

            return table;
        }
        private Table GetFiles(string path)
        {
            var folder = Storage.LuaFolder;

            var dirs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Reverse().Skip(1).Reverse();
            foreach (var dir in dirs)
                folder = folder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists).Result;

            var files = folder.GetFilesAsync().Result;

            return (MoonLuaTable) Lua.CreateTable(this, "files", files.Select(file => file.Name).ToList());
        }

        private void RegisterModule(string moduleName, string tableName = "", CoreModules modules = CoreModules.Preset_SoftSandbox)
        {
            if (string.IsNullOrEmpty(tableName))
                tableName = moduleName;

            var table = RegisterCustom(new Table(LuaScript).RegisterCoreModules(modules));
            LuaScript.DoFile(moduleName, table);
            LuaScript.Globals[tableName] = table;
        }
        private Table RegisterCustom(Table table)
        {
            foreach (var context in CustomContext)
                table[context.Key] = context.Value;

            table["CompileFile"] = (Func<string, Table>) CompileFile;
            table["GetFiles"] = (Func<string, Table>) GetFiles;

            return table;
        }
    }

    public class MoonLuaTable : LuaTable
    {
        public static implicit operator Table(MoonLuaTable moonLua) => moonLua.ScriptTable;


        private Table ScriptTable { get; }

        /// <summary>
        /// Using existing Table.
        /// </summary>
        internal MoonLuaTable(Table tableScript) { ScriptTable = tableScript; }
        /// <summary>
        /// Using existing Table.
        /// </summary>
        internal MoonLuaTable(LuaScript luaScript, string tableName) { ScriptTable = luaScript[tableName] as Table; }

        /// <summary>
        /// Creating new Table.
        /// </summary>
        internal MoonLuaTable(LuaScript luaScript, string tableName, IList list)
        {
            ScriptTable = new Table((MoonLua) luaScript);
            for (var i = 0; i < list.Count; i++)
                ScriptTable[i + 1] = list[i];

            luaScript[tableName] = ScriptTable;
        }


        public override object this[object field]
        {
            get
            {
                var table = ScriptTable[field] as Table;
                return table != null ? new MoonLuaTable(table) : ScriptTable[field];
            }
            set { ScriptTable[field] = value; }
        }
        public override object this[string field]
        {
            get
            {
                var table = ScriptTable[field] as Table;
                return table != null ? new MoonLuaTable(table) : ScriptTable[field];
            }
            set { ScriptTable[field] = value; }
        }

        public override object[] CallFunction(string functionName, params object[] args)
        {
            var ret = ScriptTable.OwnerScript.Call(ScriptTable[functionName], args).Tuple;
            return ret?.Any() == true ? ret.Select(dynVal => dynVal.ToObject()).ToArray() : new object[0];
        }


        public override Dictionary<object, object> ToDictionary() => ScriptTable.Pairs.ToDictionary<TablePair, object, object>(pair => pair.Key, pair => RecursiveParse(pair.Value));
        private static object RecursiveParse(object value)
        {
            var table = value as Table;
            if (table != null)
                return RecursiveParse(new MoonLuaTable(table).ToDictionary());

            return value;
        }

        public override List<object> ToList() => ScriptTable.Values.Cast<object>().ToList();
        public override object[] ToArray() => ScriptTable.Values.Cast<object>().ToArray();
    }
}