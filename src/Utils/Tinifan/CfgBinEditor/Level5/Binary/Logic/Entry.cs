using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tinifan.Tools;

namespace Tinifan.Level5.Binary.Logic
{
    public class Entry
    {
        public string Name;

        public string CustomName;

        public bool EndTerminator;

        public Encoding Encoding;

        public List<Variable> Variables;

        public List<Entry> Children;

        public Entry()
        {

        }

        public Entry(string name, List<Variable> variables, Encoding encoding)
        {
            Name = name;
            Variables = variables;
            Encoding = encoding;
            Children = new List<Entry>();
        }

        public Entry(string name, List<Variable> variables, Encoding encoding, bool endTerminator)
        {
            Name = name;
            Variables = variables;
            Encoding = encoding;
            Children = new List<Entry>();
            EndTerminator = endTerminator;
        }

        public string[] GetDistinctStrings(List<Entry> entries = null)
        {
            List<string> distinctStrings = new List<string>();

            distinctStrings.AddRange(Variables.Where(x => x.Type == Logic.Type.String).Select(x => Convert.ToString(x.Value)).Distinct().ToArray());
            distinctStrings.AddRange(GetDistinctStrings(Children));

            return distinctStrings.Distinct().ToArray();
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

        public string GetName()
        {
            return string.Join("_", Name.Split('_').Reverse().Skip(1).Reverse());
        }

        public List<KeyValuePair<string, int>> GetChildrenCountRecursive()
        {
            var result = new List<KeyValuePair<string, int>>();

            foreach (var child in Children)
            {
                var childCount = child.GetChildrenCountRecursive();
                var childPair = new KeyValuePair<string, int>(child.GetName(), 1);

                // Check if the key already exists in the list
                var existingPair = result.Find(kv => kv.Key == childPair.Key);

                if (existingPair.Equals(default(KeyValuePair<string, int>)))
                {
                    // If the key does not exist, add it to the list
                    result.Add(childPair);
                }
                else
                {
                    // If the key exists, update the value
                    var index = result.IndexOf(existingPair);
                    result[index] = new KeyValuePair<string, int>(childPair.Key, existingPair.Value + 1);
                }

                // Add pairs from the sub-tree
                result.AddRange(childCount);
            }

            return result;
        }

        public void GetEntryNameOccurrences(Dictionary<string, int> nameOccurrences)
        {
            var entryName = GetName();

            if (nameOccurrences.ContainsKey(entryName))
            {
                nameOccurrences[entryName]++;
            }
            else
            {
                nameOccurrences[entryName] = 1;
            }

            foreach (var childEntry in Children)
            {
                childEntry.GetEntryNameOccurrences(nameOccurrences);
            }
        }

        // Recursive cloning function
        public Entry Clone()
        {
            // Clone variables
            List<Variable> clonedVariables = Variables.Select(v => new Variable(v)).ToList();

            // Clone children recursively
            List<Entry> clonedChildren = Children.Select(child => child.Clone()).ToList();

            // Create a new cloned entry
            Entry clonedEntry = new Entry(Name, clonedVariables, Encoding)
            {
                EndTerminator = EndTerminator,
                Children = clonedChildren
            };

            return clonedEntry;
        }

        public int Count()
        {
            int totalCount = 1 + Convert.ToInt32(EndTerminator);

            foreach (Entry child in Children)
            {
                totalCount += child.Count();
            }

            return totalCount;
        }

        public Entry[] FindAll(Func<Entry, bool> predicate)
        {
            List<Entry> matchingEntries = new List<Entry>();

            // Check if the current entry satisfies the predicate
            if (predicate(this))
            {
                matchingEntries.Add(this);
            }

            // Recursively traverse children
            foreach (Entry child in Children)
            {
                Entry[] matchingChildren = child.FindAll(predicate);
                matchingEntries.AddRange(matchingChildren);
            }

            return matchingEntries.ToArray();
        }

        public List<string> GetUniqueKeys()
        {
            HashSet<string> uniqueNames = new HashSet<string>();

            // First, add the name of the current entry if it's not already in the list
            string currentName = GetName();
            if (!uniqueNames.Contains(currentName))
            {
                uniqueNames.Add(currentName);
            }

            // Then, add the names of children
            foreach (Entry child in Children)
            {
                uniqueNames.UnionWith(child.GetUniqueKeys());
            }

            // End terminator
            if (EndTerminator)
            {
                string endTerminatorName = "";

                if (currentName.StartsWith("PTREE"))
                {
                    endTerminatorName = "_PTREE";
                }
                else
                {
                    endTerminatorName = GetName().Replace("BEGIN", "END").Replace("BEG", "END");
                }

                if (!uniqueNames.Contains(endTerminatorName))
                {
                    uniqueNames.Add(endTerminatorName);
                }
            }

            // Convert the HashSet to a list
            List<string> result = uniqueNames.ToList();

            return result;
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

        private byte[] EncodeKeyTable(List<string> keyList)
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
                    stringOffset += Encoding.GetByteCount(key) + 1;
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

        private byte[] EncodeTypes(List<Type> types)
        {
            List<byte> byteArray = new List<byte>();

            // Iterate through types and create type descriptors
            for (int i = 0; i < Math.Ceiling((double)types.Count / 4); i++)
            {
                int typeDesc = 0;

                // Create a type descriptor for each set of 4 types
                for (int j = 4 * i; j < Math.Min(4 * (i + 1), types.Count); j++)
                {
                    int tagValue = 0;

                    // Map Type to tag values
                    switch (types[j])
                    {
                        case Logic.Type.String:
                            tagValue = 0;
                            break;
                        case Logic.Type.Int:
                            tagValue = 1;
                            break;
                        case Logic.Type.Float:
                            tagValue = 2;
                            break;
                        default:
                            tagValue = 0;
                            break;
                    }

                    // Combine tag values to create the type descriptor
                    typeDesc |= tagValue << ((j % 4) * 2);
                }

                // Convert type descriptor to byte array and add to byteArray
                byteArray.Add((byte)typeDesc);
            }

            // Fill the byte array with FF to ensure a size multiple of 4
            while ((byteArray.Count + 1) % 4 != 0)
            {
                byteArray.Add(0xFF);
            }

            return byteArray.ToArray();
        }

        public byte[] EncodeEntry(Dictionary<string, int> stringsTable)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryDataWriter writer = new BinaryDataWriter(memoryStream))
                {
                    string entryName = GetName();
                    writer.Write(Crc32.Compute(Encoding.GetBytes(entryName)));
                    List<Type> types;

                    if (Children.Count > 0)
                    {
                        writer.Write((byte)Variables.Count);

                        types = Variables.Select(x => x.Type).ToList();
                        writer.Write(EncodeTypes(types));

                        foreach (Variable variable in Variables)
                        {
                            switch (variable.Type)
                            {
                                case Type.String:
                                    string valueString = Convert.ToString(variable.Value);
                                    if (stringsTable.ContainsKey(valueString))
                                    {
                                        writer.Write(stringsTable[valueString]);
                                    }
                                    else
                                    {
                                        writer.Write(-1);
                                    }
                                    break;
                                case Type.Int:
                                    writer.Write(Convert.ToInt32(variable.Value));
                                    break;
                                case Type.Float:
                                    writer.Write(Convert.ToSingle(variable.Value));
                                    break;
                                default:
                                    writer.Write(Convert.ToInt32(variable.Value));
                                    break;
                            }
                        }

                        foreach (Entry child in Children)
                        {
                            writer.Write(child.EncodeEntry(stringsTable));
                        }
                    }
                    else
                    {
                        List<Variable> item = Variables;
                        writer.Write((byte)item.Count);

                        types = item.Select(x => x.Type).ToList();
                        writer.Write(EncodeTypes(types));

                        foreach (Variable variable in item)
                        {
                            switch (variable.Type)
                            {
                                case Type.String:
                                    string valueString = Convert.ToString(variable.Value);
                                    if (stringsTable.ContainsKey(valueString))
                                    {
                                        writer.Write(stringsTable[valueString]);
                                    }
                                    else
                                    {
                                        writer.Write(-1);
                                    }
                                    break;
                                case Type.Int:
                                    writer.Write(Convert.ToInt32(variable.Value));
                                    break;
                                case Type.Float:
                                    writer.Write(Convert.ToSingle(variable.Value));
                                    break;
                                default:
                                    writer.Write(Convert.ToInt32(variable.Value));
                                    break;
                            }
                        }
                    }

                    if (EndTerminator)
                    {
                        string endTerminatorName = "";

                        if (entryName.StartsWith("PTREE"))
                        {
                            endTerminatorName = "_PTREE";
                        }
                        else
                        {
                            endTerminatorName = GetName().Replace("BEGIN", "END").Replace("BEG", "END");
                        }

                        writer.Write(Crc32.Compute(Encoding.GetBytes(endTerminatorName)));
                        writer.Write(new byte[4] { 0x00, 0xFF, 0xFF, 0xFF });
                    }

                    return memoryStream.ToArray();
                }
            }
        }

        public byte[] EntryToBin()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryDataWriter writer = new BinaryDataWriter(stream))
            {
                Dictionary<string, int> strings = GetStringsTable();
                strings = strings.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);

                CfgBinSupport.Header header;
                header.EntriesCount = Count();
                header.StringTableOffset = 0;
                header.StringTableLength = 0;
                header.StringTableCount = strings.Count;

                writer.Seek(0x10);

                writer.Write(EncodeEntry(strings));

                writer.WriteAlignment(0x10, 0xFF);
                header.StringTableOffset = (int)writer.Position;

                if (strings.Count > 0)
                {
                    writer.Write(EncodeStrings());
                    header.StringTableLength = (int)writer.Position - header.StringTableOffset;
                    writer.WriteAlignment(0x10, 0xFF);
                }

                List<string> distinctEntry = GetUniqueKeys();
                writer.Write(EncodeKeyTable(distinctEntry));

                writer.Write(new byte[5] { 0x01, 0x74, 0x32, 0x62, 0xFE });
                writer.Write(new byte[4] { 0x01, GetEncoding(), 0x00, 0x01 });
                writer.WriteAlignment();

                writer.Seek(0);
                writer.WriteStruct(header);

                return stream.ToArray();
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

        public void ReplaceString(string oldString, string newString)
        {
            foreach (Variable variable in Variables.Where(x => x.Type == Logic.Type.String))
            {
                if (Convert.ToString(variable.Value) == oldString)
                {
                    variable.Value = newString;
                }
            }
        }

        public bool MatchEntry(string match)
        {
            match = match.ToLower();

            if (GetName().ToLower() == match || GetName().ToLower().StartsWith(match))
            {
                return true;
            }
            else
            {
                foreach (Variable variable in Variables)
                {
                    if (variable.Name != null && (variable.Name.ToLower() == match || variable.Name.ToLower().StartsWith(match)))
                    {
                        return true;
                    }
                    else
                    {
                        if (variable.Type == Logic.Type.String)
                        {
                            string matchString = Convert.ToString(variable.Value).ToLower();
                            if (matchString == match | matchString.StartsWith(match))
                            {
                                return true;
                            }
                        }
                        else if (variable.Type == Logic.Type.Int)
                        {
                            try
                            {
                                int matchToInt = Convert.ToInt32(match);

                                if (matchToInt == Convert.ToInt32(variable.Value))
                                {
                                    return true;
                                }
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }
                        else if (variable.Type == Logic.Type.Float)
                        {
                            try
                            {
                                float matchToInt = Convert.ToSingle(match);

                                if (matchToInt == Convert.ToSingle(variable.Value))
                                {
                                    return true;
                                }
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public void UpdateEntryNames(Dictionary<string, int> nameOccurrences)
        {
            var entryName = GetName();

            if (nameOccurrences.ContainsKey(entryName))
            {
                Name = $"{entryName}_{nameOccurrences[entryName]}";
                nameOccurrences[entryName]++;
            }
            else
            {
                nameOccurrences[entryName] = 1;
            }

            foreach (var childEntry in Children)
            {
                childEntry.UpdateEntryNames(nameOccurrences);
            }
        }

        public T ToClass<T>() where T : class, new()
        {
            T structure = new T();

            int valueIndex = 0;
            var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
            object[] values = Variables.Select(y => y.Value).ToArray();

            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var propertyType = property.PropertyType;

                if (propertyType.IsArray)
                {
                    int arrayLength = ((Array)property.GetValue(structure)).Length;
                    Array.Copy(values, valueIndex, (Array)property.GetValue(structure), 0, arrayLength);
                    valueIndex += arrayLength;
                }
                else
                {
                    object convertedValue = Convert.ChangeType(values[valueIndex], propertyType);
                    property.SetValue(structure, convertedValue);
                    valueIndex++;
                }
            }

            return structure;
        }

        public void SetVariablesFromClass<T>(T structure) where T : class
        {
            List<Variable> variableList = new List<Variable>();

            var properties = typeof(T).GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

            foreach (var property in properties)
            {
                var propertyType = property.PropertyType;
                var propertyValue = property.GetValue(structure);

                Type variableType;
                object variableValue;

                if (propertyValue != null)
                {
                    if (propertyType.IsArray)
                    {
                        var arrayElements = ((Array)propertyValue).Cast<object>().ToArray();
                        for (int i = 0; i < arrayElements.Length; i++)
                        {
                            Type variableTypeElementFromArray;

                            switch (System.Type.GetTypeCode(arrayElements[i].GetType()))
                            {
                                case TypeCode.String:
                                    variableTypeElementFromArray = Type.String;
                                    break;
                                case TypeCode.Int32:
                                    variableTypeElementFromArray = Type.Int;
                                    break;
                                case TypeCode.Single:
                                    variableTypeElementFromArray = Type.Float;
                                    break;
                                default:
                                    variableTypeElementFromArray = Type.Int;
                                    arrayElements[i] = 0;
                                    break;
                            }

                            variableValue = arrayElements[i];
                            variableList.Add(new Variable(variableTypeElementFromArray, variableValue));
                        }
                    }
                    else
                    {
                        switch (System.Type.GetTypeCode(propertyType))
                        {
                            case TypeCode.String:
                                variableType = Type.String;
                                break;
                            case TypeCode.Int32:
                                variableType = Type.Int;
                                break;
                            case TypeCode.Single:
                                variableType = Type.Float;
                                break;
                            default:
                                variableType = Type.Int;
                                propertyValue = 0;
                                break;
                        }

                        variableValue = propertyValue;
                        variableList.Add(new Variable(variableType, variableValue));
                    }
                }

            }

            Variables = variableList;
        }
    }
}
