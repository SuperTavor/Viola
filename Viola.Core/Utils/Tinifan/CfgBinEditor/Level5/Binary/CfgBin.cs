using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tinifan.Tools;
using Tinifan.Level5.Binary.Logic;

namespace Tinifan.Level5.Binary
{
    public class CfgBin
    {
        public Encoding Encoding;

        public List<Entry> Entries;

        public Dictionary<int, string> Strings;

        public CfgBin()
        {
            Entries = new List<Entry>();
            Strings = new Dictionary<int, string>();
        }

        public void Open(byte[] data)
        {
            using (var reader = new BinaryDataReader(data))
            {
                reader.Seek((uint)reader.Length - 0x0A);
                Encoding = SetEncoding(reader.ReadValue<byte>());

                reader.Seek(0x0);
                var header = reader.ReadStruct<CfgBinSupport.Header>();

                byte[] entriesBuffer = reader.GetSection(0x10, header.StringTableOffset);

                byte[] stringTableBuffer = reader.GetSection((uint)header.StringTableOffset, header.StringTableLength);
                Strings = ParseStrings(header.StringTableCount, stringTableBuffer);

                long keyTableOffset = RoundUp(header.StringTableOffset + header.StringTableLength, 16);
                reader.Seek((uint)keyTableOffset);
                int keyTableSize = reader.ReadValue<int>();
                byte[] keyTableBlob = reader.GetSection((uint)keyTableOffset, keyTableSize);
                Dictionary<uint, string> keyTable = ParseKeyTable(keyTableBlob);

                Entries = ParseEntries(header.EntriesCount, entriesBuffer, keyTable);
            }
        }

        public void Open(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream))
            {
                reader.Seek((uint)reader.Length - 0x0A);
                Encoding = SetEncoding(reader.ReadValue<byte>());

                reader.Seek(0x0);
                var header = reader.ReadStruct<CfgBinSupport.Header>();

                byte[] entriesBuffer = reader.GetSection(0x10, header.StringTableOffset);

                byte[] stringTableBuffer = reader.GetSection((uint)header.StringTableOffset, header.StringTableLength);
                Strings = ParseStrings(header.StringTableCount, stringTableBuffer);

                long keyTableOffset = RoundUp(header.StringTableOffset + header.StringTableLength, 16);
                reader.Seek((uint)keyTableOffset);
                int keyTableSize = reader.ReadValue<int>();
                byte[] keyTableBlob = reader.GetSection((uint)keyTableOffset, keyTableSize);
                Dictionary<uint, string> keyTable = ParseKeyTable(keyTableBlob);

                Entries = ParseEntries(header.EntriesCount, entriesBuffer, keyTable);
            }
        }

        public void Save(string fileName)
        {
            Dictionary<string, int> stringsTable = GetStringsTable();

            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                int distinctStringCount = GetDistinctStrings().Length;

                BinaryDataWriter writer = new BinaryDataWriter(stream);

                CfgBinSupport.Header header;
                header.EntriesCount = Count(Entries);
                header.StringTableOffset = 0;
                header.StringTableLength = 0;
                header.StringTableCount = distinctStringCount;

                writer.Seek(0x10);

                foreach (Entry entry in Entries)
                {
                    writer.Write(entry.EncodeEntry(stringsTable));
                }

                writer.WriteAlignment(0x10, 0xFF);
                header.StringTableOffset = (int)writer.Position;

                if (distinctStringCount > 0)
                {
                    writer.Write(EncodeStrings());
                    header.StringTableLength = (int)writer.Position - header.StringTableOffset;
                    writer.WriteAlignment(0x10, 0xFF);
                }

                List<string> uniqueKeysList = Entries
                    .SelectMany(entry => entry.GetUniqueKeys())
                    .Distinct()
                    .ToList();

                writer.Write(EncodeKeyTable(uniqueKeysList));

                writer.Write(new byte[5] { 0x01, 0x74, 0x32, 0x62, 0xFE });
                writer.Write(new byte[4] { 0x01, GetEncoding(), 0x00, 0x01 });
                writer.WriteAlignment();

                writer.Seek(0);
                writer.WriteStruct(header);
            }
        }

        public byte[] Save()
        {
            Dictionary<string, int> stringsTable = GetStringsTable();

            using (MemoryStream stream = new MemoryStream())
            {
                int distinctStringCount = GetDistinctStrings().Length;

                BinaryDataWriter writer = new BinaryDataWriter(stream);

                CfgBinSupport.Header header;
                header.EntriesCount = Count(Entries);
                header.StringTableOffset = 0;
                header.StringTableLength = 0;
                header.StringTableCount = distinctStringCount;

                writer.Seek(0x10);

                foreach (Entry entry in Entries)
                {
                    writer.Write(entry.EncodeEntry(stringsTable));
                }

                writer.WriteAlignment(0x10, 0xFF);
                header.StringTableOffset = (int)writer.Position;

                if (distinctStringCount > 0)
                {
                    writer.Write(EncodeStrings());
                    header.StringTableLength = (int)writer.Position - header.StringTableOffset;
                    writer.WriteAlignment(0x10, 0xFF);
                }

                List<string> uniqueKeysList = Entries
                    .SelectMany(entry => entry.GetUniqueKeys())
                    .Distinct()
                    .ToList();

                writer.Write(EncodeKeyTable(uniqueKeysList));

                writer.Write(new byte[5] { 0x01, 0x74, 0x32, 0x62, 0xFE });
                writer.Write(new byte[4] { 0x01, GetEncoding(), 0x00, 0x01 });
                writer.WriteAlignment();

                writer.Seek(0);
                writer.WriteStruct(header);

                return stream.ToArray();
            }
        }

        public void ImportJson(string fileName)
        {
            // Read the JSON content from the file
            string jsonContent = File.ReadAllText(fileName);

            // Parse the JSON content
            JObject json = JObject.Parse(jsonContent);

            // Retrieve the encoding
            string encodingName = json.Value<string>("Encoding");
            Encoding = Encoding.GetEncoding(encodingName);

            // Retrieve the entries
            JArray entriesArray = json.Value<JArray>("Entries");
            Entries = new List<Entry>();

            if (entriesArray != null)
            {
                foreach (JObject entryObject in entriesArray.Children<JObject>())
                {
                    Entry entry = ParseEntry(entryObject, Encoding);
                    Entries.Add(entry);
                }
            }
        }

        private Entry ParseEntry(JObject entryObject, Encoding encoding)
        {
            Entry entry = new Entry();

            entry.Name = entryObject.Value<string>("Name");
            entry.EndTerminator = entryObject.Value<bool>("EndTerminator");
            entry.Variables = new List<Variable>();
            entry.Children = new List<Entry>();

            JArray variablesArray = entryObject.Value<JArray>("Variables");
            if (variablesArray != null)
            {
                foreach (JObject variableObject in variablesArray.Children<JObject>())
                {
                    Variable variable = ParseVariable(variableObject, encoding);
                    entry.Variables.Add(variable);
                }
            }

            JArray childrenArray = entryObject.Value<JArray>("Children");
            if (childrenArray != null)
            {
                foreach (JObject childObject in childrenArray.Children<JObject>())
                {
                    Entry childEntry = ParseEntry(childObject, encoding);
                    entry.Children.Add(childEntry);
                }
            }

            return entry;
        }

        private Variable ParseVariable(JObject variableObject, Encoding encoding)
        {
            Variable variable = new Variable();

            JToken valueToken = variableObject["Value"];
            (Logic.Type, object) typeAndValue = ParseValue(valueToken, encoding);

            variable.Type = typeAndValue.Item1;
            variable.Value = typeAndValue.Item2;

            return variable;
        }

        private (Logic.Type, object) ParseValue(JToken valueToken, System.Text.Encoding encoding)
        {
            if (valueToken.Type == JTokenType.String)
            {
                string textValue = valueToken.Value<string>();

                int offset = 0;
                if (Strings.Count != 0)
                {
                    if (!Strings.ContainsValue(textValue))
                    {
                        offset = Strings.Keys.Max() + encoding.GetBytes(textValue).Length + 1;
                        Strings.Add(offset, textValue);
                    }
                    else
                    {
                        offset = Strings.FirstOrDefault(x => x.Value == textValue).Key;
                    }
                }
                else
                {
                    Strings.Add(offset, textValue);
                }

                return (Logic.Type.String, textValue);
            }
            else if (valueToken.Type == JTokenType.Integer)
            {
                return (Logic.Type.Int, valueToken.Value<int>());
            }
            else if (valueToken.Type == JTokenType.Float)
            {
                return (Logic.Type.Float, valueToken.Value<float>());
            }
            else
            {
                return (Logic.Type.Int, 0);
            }
        }

        private string GetValueString(object value)
        {
            if (value is string)
            {
                return $"\"{value}\"";
            }
            if (value is int)
            {
                return value.ToString();
            }
            else if (value is float)
            {
                // Utiliser la culture anglaise pour formater la chaîne
                return ((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                return "null";
            }
        }

        public void ReplaceEntry(string entryName, Entry newEntry)
        {
            int entryIndex = Entries.FindIndex(x => x.GetName() == entryName);

            if (entryIndex >= 0)
            {
                Entries[entryIndex] = newEntry;
            }
            else
            {
                Entries.Add(newEntry);
            }
        }

        public void ReplaceEntry<T>(string entryBeginName, string entryName, T[] values) where T : class
        {
            Entry baseBegin = Entries.Where(x => x.GetName() == entryBeginName).FirstOrDefault();
            baseBegin.Children.Clear();

            for (int i = 0; i < values.Count(); i++)
            {
                Entry newBaseEntry = new Entry(entryName + i, new List<Variable>(), Encoding.UTF8);
                newBaseEntry.SetVariablesFromClass(values[i]);
                baseBegin.Children.Add(newBaseEntry);
            }
        }

        public byte GetEncoding()
        {
            if (Encoding != null && Encoding.Equals(Encoding.GetEncoding("SHIFT-JIS")))
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        private Encoding SetEncoding(byte b)
        {
            if (b == 0)
            {
                return Encoding.GetEncoding("SHIFT-JIS");
            }
            else
            {
                return Encoding.UTF8;
            }
        }

        private Dictionary<int, string> ParseStrings(int stringCount, byte[] stringTableBuffer)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();

            using (BinaryDataReader reader = new BinaryDataReader(stringTableBuffer))
            {
                for (int i = 0; i < stringCount; i++)
                {
                    if (!result.ContainsKey((int)reader.Position))
                    {
                        result.Add((int)reader.Position, reader.ReadString(Encoding));
                    }
                }
            }

            return result;
        }

        private Dictionary<uint, string> ParseKeyTable(byte[] buffer)
        {
            Dictionary<uint, string> keyTable = new Dictionary<uint, string>();

            using (var reader = new BinaryDataReader(buffer))
            {
                keyTable = new Dictionary<uint, string>();

                var header = reader.ReadStruct<CfgBinSupport.KeyHeader>();
                byte[] keyStringBlob = reader.GetSection((uint)header.KeyStringOffset, header.keyStringLength);

                for (int i = 0; i < header.KeyCount; i++)
                {
                    uint crc32 = reader.ReadValue<uint>();
                    int stringStart = reader.ReadValue<int>();
                    int stringEnd = Array.IndexOf(keyStringBlob, (byte)0, stringStart);
                    byte[] stringBuf = new byte[stringEnd - stringStart];
                    Array.Copy(keyStringBlob, stringStart, stringBuf, 0, stringEnd - stringStart);
                    string key = Encoding.GetString(stringBuf);
                    keyTable[crc32] = key;
                }
            }

            return keyTable;
        }

        private List<Entry> ParseEntries(int entriesCount, byte[] entriesBuffer, Dictionary<uint, string> keyTable)
        {
            List<Entry> temp = new List<Entry>();

            // Get All entries
            using (BinaryDataReader reader = new BinaryDataReader(entriesBuffer))
            {
                for (int i = 0; i < entriesCount; i++)
                {
                    uint crc32 = reader.ReadValue<uint>();
                    string name = keyTable[crc32];

                    int paramCount = reader.ReadValue<byte>();
                    Logic.Type[] paramTypes = new Logic.Type[paramCount];
                    int paramIndex = 0;

                    for (int j = 0; j < (int)Math.Ceiling((double)paramCount / 4); j++)
                    {
                        byte paramType = reader.ReadValue<byte>();
                        for (int k = 0; k < 4; k++)
                        {
                            if (paramIndex < paramTypes.Length)
                            {
                                int tag = (paramType >> (2 * k)) & 3;

                                switch (tag)
                                {
                                    case 0:
                                        paramTypes[paramIndex] = Logic.Type.String;
                                        break;
                                    case 1:
                                        paramTypes[paramIndex] = Logic.Type.Int;
                                        break;
                                    case 2:
                                        paramTypes[paramIndex] = Logic.Type.Float;
                                        break;
                                    default:
                                        paramTypes[paramIndex] = Logic.Type.Unknown;
                                        break;
                                }

                                paramIndex++;
                            }
                        }
                    }

                    if ((Math.Ceiling((double)paramCount / 4) + 1) % 4 != 0)
                    {
                        reader.Seek((uint)(reader.Position + 4 - (reader.Position % 4)));
                    }

                    List<Variable> variables = new List<Variable>();

                    for (int j = 0; j < paramCount; j++)
                    {
                        if (paramTypes[j] == Logic.Type.String)
                        {
                            int offset = reader.ReadValue<int>();
                            string text = null;

                            if (offset != -1 && Strings.ContainsKey(offset))
                            {
                                text = Strings[offset];
                            }

                            variables.Add(new Variable(Logic.Type.String, text));
                        }
                        else if (paramTypes[j] == Logic.Type.Int)
                        {
                            variables.Add(new Variable(Logic.Type.Int, reader.ReadValue<int>()));
                        }
                        else if (paramTypes[j] == Logic.Type.Float)
                        {
                            variables.Add(new Variable(Logic.Type.Float, reader.ReadValue<float>()));
                        }
                        else if (paramTypes[j] == Logic.Type.Unknown)
                        {
                            variables.Add(new Variable(Logic.Type.Unknown, reader.ReadValue<int>()));
                        }
                    }

                    temp.Add(new Entry(name, variables, Encoding));
                }
            }

            // Reorganize entries
            Dictionary<string, int> entriesKey = new Dictionary<string, int>();
            for (int i = 0; i < temp.Count; i++)
            {
                string entryName = temp[i].Name;

                if (!entriesKey.ContainsKey(entryName))
                {
                    entriesKey[entryName] = 0;
                }

                temp[i].Name = entryName + "_" + entriesKey[entryName];
                entriesKey[entryName] += 1;
            }

            return ProcessEntries(temp);
        }

        public List<Entry> ProcessEntries(List<Entry> entries)
        {
            List<Entry> stack = new List<Entry>();
            List<Entry> output = new List<Entry>();
            Dictionary<string, int> depth = new Dictionary<string, int>();

            int i = 0;

            while (i < entries.Count)
            {
                string name = entries[i].Name;
                List<Variable> variables = entries[i].Variables;

                string[] nameParts = name.Split('_');
                string nodeType = nameParts[nameParts.Length - 2].ToLower();
                string nodeName = string.Join("_", nameParts, 0, nameParts.Length - 1).ToLower();

                if (nodeType.EndsWith("beg") || nodeType.EndsWith("begin") || nodeType.EndsWith("start") || nodeType.EndsWith("ptree") && name.Contains("_PTREE") == false)
                {
                    Entry newNode = new Entry(name, variables, Encoding);

                    if (stack.Count > 0)
                    {
                        string entryNameWithMaxDepth = depth.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                        if (entryNameWithMaxDepth.Contains("_LIST_BEG_"))
                        {
                            entryNameWithMaxDepth = entryNameWithMaxDepth.Replace("_LIST_BEG_", "_BEG_");
                        }
                        string[] entryNameWithMaxDepthParts = entryNameWithMaxDepth.Split('_');
                        string entryBaseName = string.Join("_", entryNameWithMaxDepthParts.Take(entryNameWithMaxDepthParts.Length - 2));

                        if (name.StartsWith(entryBaseName) && (nodeType.EndsWith("beg") || nodeType.EndsWith("begin")))
                        {
                            Entry lastEntry = stack[stack.Count - 1].Children[stack[stack.Count - 1].Children.Count() - 1];
                            lastEntry.Children.Add(newNode);
                        }
                        else
                        {
                            stack[stack.Count - 1].Children.Add(newNode);
                        }
                    }
                    else
                    {
                        output.Add(newNode);
                    }

                    stack.Add(newNode);
                    depth[name] = stack.Count;
                }
                else if (nodeType.EndsWith("end") || name.Contains("_PTREE"))
                {
                    stack[stack.Count - 1].EndTerminator = true;

                    string key = "";
                    if (depth.ContainsKey(name.Replace("_END_", "_BEG_")))
                    {
                        key = name.Replace("_END_", "_BEG_");
                    }
                    else if (depth.ContainsKey(name.Replace("_END_", "_BEGIN_")))
                    {
                        key = name.Replace("_END_", "_BEGIN_");
                    }
                    else if (depth.ContainsKey(name.Replace("_END_", "_START_")))
                    {
                        key = name.Replace("_END_", "_START_");
                    }
                    else if (depth.ContainsKey(name.Replace("_PTREE", "PTREE")))
                    {
                        key = name.Replace("_PTREE", "PTREE");
                    }

                    if (depth.Count > 1)
                    {
                        string[] keys = new string[depth.Keys.Count];
                        depth.Keys.CopyTo(keys, 0);

                        int currentDepth = depth[key];
                        int previousDepth = 0;
                        previousDepth = depth[keys[Array.IndexOf(keys, key)]] - 1;

                        int popCount = currentDepth - previousDepth;
                        for (int j = 0; j < popCount; j++)
                        {
                            stack.RemoveAt(stack.Count - 1);
                        }

                        depth.Remove(key);
                    }
                    else
                    {
                        stack.RemoveAt(stack.Count - 1);
                        depth.Remove(key);
                    }
                }
                else
                {
                    if (depth.Count == 0)
                    {
                        Entry newNode = new Entry(name, variables, Encoding);
                        newNode.EndTerminator = true;

                        output.Add(newNode);
                    }
                    else
                    {
                        Entry newItem = new Entry(name, variables, Encoding);

                        string entryNameWithMaxDepth = depth.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;
                        if (entryNameWithMaxDepth.Contains("_LIST_BEG_"))
                        {
                            entryNameWithMaxDepth = entryNameWithMaxDepth.Replace("_LIST_BEG_", "_BEG_");
                        }
                        string[] entryNameWithMaxDepthParts = entryNameWithMaxDepth.Split('_');
                        string entryBaseName = string.Join("_", entryNameWithMaxDepthParts.Take(entryNameWithMaxDepthParts.Length - 2));

                        if (!name.StartsWith(entryBaseName))
                        {
                            if (!entryNameWithMaxDepth.Contains("BEGIN") && !entryNameWithMaxDepth.Contains("BEG") && !entryNameWithMaxDepth.Contains("START") && !entryNameWithMaxDepth.Contains("PTREE") && name.Contains("_PTREE") == false)
                            {
                                stack.RemoveAt(stack.Count - 1);
                                depth.Remove(entryNameWithMaxDepth);
                                stack[stack.Count - 1].Children.Add(newItem);
                            }
                            else
                            {
                                Entry lastEntry = stack[stack.Count - 1].Children[stack[stack.Count - 1].Children.Count() - 1];
                                lastEntry.Children.Add(newItem);
                                stack.Add(newItem);
                                depth[name] = stack.Count;
                            };
                        }
                        else
                        {
                            stack[stack.Count - 1].Children.Add(newItem);
                        }
                    }
                }

                i++;
            }

            return output;
        }

        private byte[] EncodeStrings()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryDataWriter writer = new BinaryDataWriter(memoryStream))
                {
                    foreach (string myString in GetDistinctStrings())
                    {
                        writer.Write(Encoding.GetBytes(myString));
                        writer.Write((byte)0x00);
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        private Dictionary<string, int> GetStringsTable()
        {
            Dictionary<string, int> output = new Dictionary<string, int>();

            int pos = 0;
            foreach (string myString in GetDistinctStrings())
            {
                output.Add(myString, pos);
                pos += Encoding.GetByteCount(myString) + 1;
            }

            return output;
        }

        public byte[] EncodeKeyTable(List<string> keyList)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                // Calculate the total size required for the header and key strings
                uint headerSize = (uint)Marshal.SizeOf(typeof(CfgBinSupport.KeyHeader));
                uint keyStringsSize = 0;

                foreach (var key in keyList)
                {
                    keyStringsSize += (uint)Encoding.GetByteCount(key) + 1; // +1 for null-terminator
                }

                // Write header
                var header = new CfgBinSupport.KeyHeader
                {
                    KeyCount = keyList.Count,
                    keyStringLength = (int)keyStringsSize
                };

                writer.Seek(0x10);

                int stringOffset = 0;

                // Calculate CRC32 for each key and write key entries
                foreach (var key in keyList)
                {
                    uint crc32 = Crc32.Compute(Encoding.GetBytes(key));
                    writer.Write(crc32);
                    writer.Write(stringOffset);
                    stringOffset += Encoding.GetBytes(key).Count() + 1;
                }

                writer.WriteAlignment(0x10, 0xFF);

                header.KeyStringOffset = (int)writer.Position;

                // Write key strings
                foreach (var key in keyList)
                {
                    byte[] stringBytes = Encoding.GetBytes(key);
                    writer.Write(stringBytes);
                    writer.Write((byte)0); // Null-terminator
                }

                writer.WriteAlignment(0x10, 0xFF);
                header.KeyLength = (int)writer.Position;
                writer.Seek(0x00);
                writer.WriteStruct(header);

                return stream.ToArray();
            }
        }

        private long RoundUp(int n, int exp)
        {
            return ((n + exp - 1) / exp) * exp;
        }

        public int Count(List<Entry> entries)
        {
            int totalCount = 0;

            foreach (Entry entry in entries)
            {
                totalCount += entry.Count();
            }

            return totalCount;
        }

        public string[] GetDistinctStrings()
        {
            return GetDistinctStringsRecursive(Entries).Distinct().ToArray();
        }

        private List<string> GetDistinctStringsRecursive(List<Entry> entries)
        {
            List<string> distinctStrings = new List<string>();

            foreach (Entry entry in entries)
            {
                distinctStrings.AddRange(entry.Variables.Where(x => x.Type == Logic.Type.String).Select(x => Convert.ToString(x.Value)).Distinct().ToArray());
                distinctStrings.AddRange(GetDistinctStringsRecursive(entry.Children));
            }

            return distinctStrings;
        }

        public void ReplaceString(string oldString, string newString)
        {
            foreach (Entry entry in Entries)
            {
                entry.ReplaceString(oldString, newString);

                foreach (Entry child in entry.Children)
                {
                    child.ReplaceString(oldString, newString);
                }
            }
        }

        public List<Entry> FindEntry(string match)
        {
            return FindEntryRecursive(Entries, match).ToList();
        }

        private List<Entry> FindEntryRecursive(List<Entry> entries, string match)
        {
            List<Entry> matchesEntry = new List<Entry>();

            foreach (Entry entry in entries)
            {
                if (entry.MatchEntry(match))
                {
                    matchesEntry.Add(entry);
                }

                matchesEntry.AddRange(FindEntryRecursive(entry.Children, match));
            }

            return matchesEntry;
        }
    }
}