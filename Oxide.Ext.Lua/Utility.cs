﻿using System;
using System.Collections.Generic;
using System.Linq;

using NLua;

using Oxide.Core.Configuration;

namespace Oxide.Lua
{
    /// <summary>
    /// Contains extension and utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Returns if the Lua table represents an array or not
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool IsArray(this LuaTable table, out int count)
        {
            count = 0;
            foreach (object key in table.Keys)
            {
                if (!(key is double)) return false;
                double numkey = (double)key;
                if (Math.Floor(numkey) != numkey) return false;
                if (numkey < 1.0) return false;
                if (numkey > count) count = (int)numkey;
            }
            return true;
        }

        /// <summary>
        /// Returns if the Lua table represents an array or not
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool IsArray(this LuaTable table)
        {
            int count;
            return IsArray(table, out count);
        }

        /// <summary>
        /// Copies and translates the contents of the specified table into the specified config file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="table"></param>
        public static void SetConfigFromTable(DynamicConfigFile config, LuaTable table)
        {
            config.Clear();
            foreach (object key in table.Keys)
            {
                string keystr = key as string;
                if (keystr != null)
                {
                    object value = TranslateLuaItemToConfigItem(table[key]);
                    if (value != null) config[keystr] = value;
                }
            }
        }

        /// <summary>
        /// Translates a single object from its Lua form to it's C# form for use in a config file
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslateLuaItemToConfigItem(object item)
        {
            if (item is string)
                return item;
            else if (item is double)
            {
                // If it's whole, return it as an int
                double number = (double)item;
                if (Math.Truncate(number) == number)
                    return (int)number;
                else
                    return (float)number;
            }
            else if (item is bool)
                return item;
            else if (item is LuaTable)
            {
                LuaTable table = item as LuaTable;
                int count;
                if (table.IsArray(out count))
                {
                    List<object> list = new List<object>();
                    for (int i = 0; i < count; i++)
                    {
                        object luaobj = table[(double)(i + 1)];
                        if (luaobj != null)
                            list.Add(TranslateLuaItemToConfigItem(luaobj));
                        else
                            list.Add(null);
                    }
                    return list;
                }
                else
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    foreach (object key in table.Keys)
                    {
                        if (key is string)
                            dict.Add(key as string, TranslateLuaItemToConfigItem(table[key]));
                    }
                    return dict;
                }
            }
            else
                return null;
        }

        /// <summary>
        /// Copies and translates the contents of the specified config file into the specified table
        /// </summary>
        /// <param name="config"></param>
        /// <param name="lua"></param>
        /// <returns></returns>
        public static LuaTable TableFromConfig(DynamicConfigFile config, NLua.Lua lua)
        {
            // Make a table
            lua.NewTable("tmp");
            LuaTable tbl = lua["tmp"] as LuaTable;
            lua["tmp"] = null;

            // Loop each item in config
            foreach (var pair in config)
            {
                // Translate and set on table
                tbl[pair.Key] = TranslateConfigItemToLuaItem(lua, pair.Value);
            }

            // Return
            return tbl;
        }

        /// <summary>
        /// Translates a single object from it's C# form to it's Lua form
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslateConfigItemToLuaItem(NLua.Lua lua, object item)
        {
            // Switch on the object type
            if (item is int || item is float || item is double)
                return Convert.ToDouble(item);
            else if (item is bool)
                return Convert.ToBoolean(item);
            else if (item is string)
                return item;
            else if (item is List<object>)
            {
                lua.NewTable("tmplist");
                LuaTable tbl = lua["tmplist"] as LuaTable;
                lua["tmplist"] = null;

                List<object> list = item as List<object>;
                for (int i = 0; i < list.Count; i++)
                {
                    tbl[i + 1] = TranslateConfigItemToLuaItem(lua, list[i]);
                }

                return tbl;
            }
            else if (item is Dictionary<string, object>)
            {
                lua.NewTable("tmpdict");
                LuaTable tbl = lua["tmpdict"] as LuaTable;
                lua["tmpdict"] = null;

                Dictionary<string, object> dict = item as Dictionary<string, object>;
                foreach (var pair in dict)
                {
                    tbl[pair.Key] = TranslateConfigItemToLuaItem(lua, pair.Value);
                }

                return tbl;
            }
            else
                return null;
        }

        /// <summary>
        /// Gets the namespace of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetNamespace(Type type)
        {
            //string name = type.FullName;
            //if (!name.Contains('.')) return string.Empty;
            //int nameindex = name.LastIndexOf('.');
            //return name.Substring(0, nameindex);
            return type.Namespace == null ? string.Empty : type.Namespace;
        }
    }
}
