using System.Buffers.Binary;
using System.Reflection;
using System.Text;

namespace MiniVNCClient.Util
{
    internal static class Serializer
    {
        public static void Serialize<T>(BinaryStream stream, T obj)
        {
            var type = typeof(T);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var fieldValue = field.GetValue(obj)!;

                switch (fieldValue)
                {
                    case sbyte value:
                        stream.Write(value);
                        break;
                    case short value:
                        stream.Write(value);
                        break;
                    case int value:
                        stream.Write(value);
                        break;
                    case long value:
                        stream.Write(value);
                        break;
                    case byte value:
                        stream.Write(value);
                        break;
                    case ushort value:
                        stream.Write(value);
                        break;
                    case uint value:
                        stream.Write(value);
                        break;
                    case ulong value:
                        stream.Write(value);
                        break;
                    case string value:
                        stream.Write(Encoding.UTF8.GetBytes(value));
                        break;
                    default:
                        typeof(Serializer)
                            .GetMethod(nameof(Serialize))
                            !.MakeGenericMethod(field.FieldType)
                            .Invoke(null, [stream, fieldValue]);
                        break;
                }
            }
        }

        public static T Deserialize<T>(BinaryStream stream)
        {
            var type = typeof(T);

            var obj = Activator.CreateInstance(type);

            FieldInfo? previousField = null;

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                switch (Type.GetTypeCode(field.FieldType))
                {
                    case TypeCode.SByte:
                        field.SetValue(obj, stream.ReadSByte());
                        break;
                    case TypeCode.Int16:
                        field.SetValue(obj, stream.ReadInt16());
                        break;
                    case TypeCode.Int32:
                        field.SetValue(obj, stream.ReadInt32());
                        break;
                    case TypeCode.Int64:
                        field.SetValue(obj, stream.ReadInt64());
                        break;
                    case TypeCode.Byte:
                        field.SetValue(obj, stream.ReadByte());
                        break;
                    case TypeCode.UInt16:
                        field.SetValue(obj, stream.ReadUInt16());
                        break;
                    case TypeCode.UInt32:
                        field.SetValue(obj, stream.ReadUInt32());
                        break;
                    case TypeCode.UInt64:
                        field.SetValue(obj, stream.ReadUInt64());
                        break;
                    case TypeCode.String:
                        var stringSize = (uint)previousField!.GetValue(obj)!;
                        field.SetValue(obj, Encoding.UTF8.GetString(stream.ReadBytes((int)stringSize)));
                        break;
                    default:
                        field.SetValue(
                            obj,
                            typeof(Serializer)
                            .GetMethod(nameof(Deserialize))
                            !.MakeGenericMethod(field.FieldType)
                            .Invoke(null, [stream])
                        );
                        break;
                }

                previousField = field;
            }

            return (T)obj!;
        }
    }
}
