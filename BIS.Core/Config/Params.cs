using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

using BIS.Core.Streams;

namespace BIS.Core.Config
{
    #region Enums
    public enum EntryType : byte
    {
        Class,
        Value,
        Array,
        ClassDecl,
        ClassDelete,
        ArraySpec
    }

    public enum ValueType : byte
    {
        Generic, // generic = string
        Float,
        Int,
        Array, //not used?
        Expression,
        NSpecValueType,
        Int64,
    }
    #endregion

    #region ParamEntries
    public abstract class ParamEntry
    {
        public string Name { get; protected set; }

        public static ParamEntry ReadParamEntry(BinaryReaderEx input)
        {
            var entryType = (EntryType)input.ReadByte();

            switch(entryType)
            {
                case EntryType.Class:
                    return new ParamClass(input);

                case EntryType.Array:
                    return new ParamArray(input);

                case EntryType.Value:
                    return new ParamValue(input);

                case EntryType.ClassDecl:
                    return new ParamExternClass(input);

                case EntryType.ClassDelete:
                    return new ParamDeleteClass(input);

                case EntryType.ArraySpec:
                    return new ParamArraySpec(input);

                default: throw new ArgumentException("Unknown ParamEntry Type", nameof(entryType));
            }
        }

        public virtual string ToString(int indentionLevel = 0) => base.ToString();
        public override string ToString() => ToString(0);
    }

    public class ParamClass : ParamEntry
    {
        public string BaseClassName { get; private set; }
        public List<ParamEntry> Entries { get; private set; }

        public ParamClass()
        {
            BaseClassName = "";
            Name = "";
            Entries = new List<ParamEntry>(20);
        }

        public ParamClass(string name, string baseclass, IEnumerable<ParamEntry> entries)
        {
            BaseClassName = baseclass;
            Name = name;
            Entries = entries.ToList();
        }

        public ParamClass(string name, IEnumerable<ParamEntry> entries): this(name, "", entries) { }

        public ParamClass(string name, params ParamEntry[] entries) : this(name, (IEnumerable<ParamEntry>)entries) { }

        public ParamClass(BinaryReaderEx input)
        {
            Name = input.ReadAsciiz();
            var offset = input.ReadUInt32();
            var oldPos = input.Position;
            input.Position = offset;
            ReadCore(input);
            input.Position = oldPos;
        }

        public ParamClass(BinaryReaderEx input, string fileName)
        {
            Name = fileName;
            ReadCore(input);
        }

        public ParamClass GetClass(string name)
        {
            return Entries.OfType<ParamClass>().FirstOrDefault(c => c.Name == name);
        }
        public T[] GetArray<T>(string name)
        {
            return Entries.OfType<ParamArray>().FirstOrDefault(c => c.Name == name)?.ToArray<T>();
        }

        private void ReadCore(BinaryReaderEx input)
        {
            BaseClassName = input.ReadAsciiz();

            var nEntries = input.ReadCompactInteger();
            Entries = Enumerable.Range(0, nEntries).Select(_ => ReadParamEntry(input)).ToList();
        }

        public string ToString(int indentionLevel, bool onlyClassBody)
        {
            var ind = new string(' ', indentionLevel * 4);
            var classBody = new StringBuilder();

            var indLvl = (onlyClassBody) ? indentionLevel : indentionLevel + 1;
            foreach (var entry in Entries)
                classBody.AppendLine(entry.ToString(indLvl));

            var classHead = (string.IsNullOrEmpty(BaseClassName)) ?
                $"{ind}class {Name}" :
                $"{ind}class {Name} : {BaseClassName}";

            if (onlyClassBody)
                return classBody.ToString();

            return
$@"{classHead}
{ind}{{
{classBody.ToString()}{ind}}};";
        }

        public override string ToString(int indentionLevel = 0) => ToString(indentionLevel, false);
    }

    public class ParamArray : ParamEntry
    {
        public RawArray Array { get; private set; }

        public ParamArray(BinaryReaderEx input)
        {
            Name = input.ReadAsciiz();
            Array = new RawArray(input);
        }

        public ParamArray(string name, IEnumerable<RawValue> values)
        {
            Name = name;
            Array = new RawArray(values);
        }

        public ParamArray(string name, params RawValue[] values): this(name, (IEnumerable < RawValue >)values) { }

        public T[] ToArray<T>()
        {
            return Array.Entries.Select(e => e.Get<T>()).ToArray();
        }

        public override string ToString(int indentionLevel = 0)
        {
            return $"{new string(' ', indentionLevel * 4)}{Name}[]={Array.ToString()};";
        }
    }

    public class ParamArraySpec : ParamEntry
    {
        public int Flag { get; }

        public RawArray Array { get; private set; }

        public ParamArraySpec(BinaryReaderEx input)
        {
            Flag = input.ReadInt32();
            Name = input.ReadAsciiz();
            Array = new RawArray(input);
        }

        public ParamArraySpec(string name, int flag, IEnumerable<RawValue> values)
        {
            Name = name;
            Flag = flag;
            Array = new RawArray(values);
        }

        public ParamArraySpec(string name, int flag, params RawValue[] values) : this(name, flag, (IEnumerable<RawValue>)values) { }

        public T[] ToArray<T>()
        {
            return Array.Entries.Select(e => e.Get<T>()).ToArray();
        }

        public override string ToString(int indentionLevel = 0)
        {
            if (Flag == 1)
            {
                return $"{new string(' ', indentionLevel * 4)}{Name}[]+={Array.ToString()};";
            }
            return $"{new string(' ', indentionLevel * 4)}{Name}[]={Array.ToString()}; // Unknown flag {Flag}";
        }
    }

    public class ParamValue : ParamEntry
    {
        public RawValue Value { get; private set; }

        public ParamValue(string name, bool value)
        {
            Name = name;
            Value = new RawValue(value ? 1 : 0);
        }
        public ParamValue(string name, int value)
        {
            Name = name;
            Value = new RawValue(value);
        }
        public ParamValue(string name, float value)
        {
            Name = name;
            Value = new RawValue(value);
        }
        public ParamValue(string name, string value)
        {
            Name = name;
            Value = new RawValue(value);
        }

        public ParamValue(BinaryReaderEx input)
        {
            var subtype = (ValueType)input.ReadByte();
            Name = input.ReadAsciiz();
            Value = new RawValue(input, subtype);
        }

        public override string ToString(int indentionLevel=0)
        {
            return $"{new string(' ', indentionLevel * 4)}{Name}={Value.ToString()};";
        }
    }

    public class ParamExternClass : ParamEntry
    {
        public ParamExternClass(BinaryReaderEx input) : this(input.ReadAsciiz()) { }

        public ParamExternClass(string name)
        {
            Name = name;
        }

        public override string ToString(int indentionLevel = 0)
        {
            return $"{new string(' ', indentionLevel * 4)}class {Name};";
        }
    }
    public class ParamDeleteClass : ParamEntry
    {
        public ParamDeleteClass(BinaryReaderEx input) : this(input.ReadAsciiz()) { }

        public ParamDeleteClass(string name)
        {
            Name = name;
        }

        public override string ToString(int indentionLevel = 0)
        {
            return $"{new string(' ', indentionLevel * 4)}delete {Name};";
        }
    }
    #endregion

    #region ParamValues
    public class RawArray
    {
        public List<RawValue> Entries { get; private set; }

        public RawArray(IEnumerable<RawValue> values)
        {
            Entries = values.ToList();
        }

        public RawArray(BinaryReaderEx input)
        {
            var nEntries = input.ReadCompactInteger();
            Entries = Enumerable.Range(0, nEntries).Select(_ => new RawValue(input)).ToList();
        }

        public override string ToString()
        {
            var valStr = string.Join(", ", Entries.Select(x => x.ToString()));
            return $"{{{valStr}}}";
        }
    }

    public class RawValue
    {
        public ValueType Type { get; protected set; }
        public object Value { get; private set; }

        public RawValue(string v)
        {
            Type = ValueType.Generic;
            Value = v;
        }

        public RawValue(int v)
        {
            Type = ValueType.Int;
            Value = v;
        }

        public RawValue(long v)
        {
            Type = ValueType.Int64;
            Value = v;
        }

        public RawValue(float v)
        {
            Type = ValueType.Float;
            Value = v;
        }

        public RawValue(BinaryReaderEx input) : this(input, (ValueType)input.ReadByte()) { }

        public RawValue(BinaryReaderEx input, ValueType type)
        {
            Type = type;
            switch (Type)
            {
                case ValueType.Expression: goto case ValueType.Generic;
                case ValueType.Generic:
                    Value = input.ReadAsciiz();
                    break;
                case ValueType.Float:
                    Value = input.ReadSingle();
                    break;
                case ValueType.Int:
                    Value = input.ReadInt32();
                    break;
                case ValueType.Int64:
                    Value = input.ReadInt64();
                    break;
                case ValueType.Array:
                    Value = new RawArray(input);
                    break;

                default: throw new ArgumentException();
            }
        }

        public override string ToString()
        {
            if (Type == ValueType.Expression || Type == ValueType.Generic)
                return $"\"{Value}\"";

            if (Type == ValueType.Float)
                return ((float)Value).ToString(CultureInfo.InvariantCulture);

            return Value.ToString();
        }

        internal T Get<T>()
        {
            return (T)Convert.ChangeType(Value, typeof(T));
        }
    }
    #endregion
}