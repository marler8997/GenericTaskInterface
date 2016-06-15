using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Gti
{
    [XmlRootAttribute("Gti")]
    public class GtiXml : IFinalizable
    {
        public static XmlSerializer CreateSerializer()
        {
            return new XmlSerializer(typeof(GtiXml));
        }

        public static readonly XmlDeserializationEvents SerializerEvents;
        static GtiXml()
        {
            SerializerEvents = new XmlDeserializationEvents();
            SerializerEvents.OnUnreferencedObject += OnUnreferencedObject;
            SerializerEvents.OnUnknownAttribute += OnUnknownAttribute;
            SerializerEvents.OnUnknownElement += OnUnknownElement;
            SerializerEvents.OnUnknownNode += OnUnknownNode;
        }
        static void OnUnknownAttribute(Object sender, XmlAttributeEventArgs args)
        {
            throw new FormatException(String.Format("Unknown XML Attribute {0}=\"{1}\"", args.Attr.Name, args.Attr.Value));
        }
        static void OnUnknownElement(Object sender, XmlElementEventArgs args)
        {
            throw new FormatException(String.Format("Unknown XML Element '{0}'", args.Element.Name));
        }
        static void OnUnknownNode(Object sender, XmlNodeEventArgs args)
        {
            if (args.NodeType == XmlNodeType.Attribute)
            {
                throw new FormatException(String.Format("Unknown XML Attribute {0}=\"{1}\"", args.Name, args.Text));
            }
            throw new FormatException(String.Format("Unknown XML node '{0}'", args.Name));
        }
        static void OnUnreferencedObject(Object sender, UnreferencedObjectEventArgs args)
        {
            throw new FormatException("Unreferenced XML Object");
        }

        [XmlElement("Enum")]
        public EnumDefinition[] Enums;

        [XmlElement("Task")]
        public Task[] Tasks;

        [XmlElement("ConsoleApplication")]
        public ConsoleApplication[] ConsoleApplications;

        public void FinalizeDeserialization()
        {
            Enums.FinalizeItems(); // Enums first
            Dictionary<String, EnumDefinition> enumMap = new Dictionary<String, EnumDefinition>((int)Enums.SafeLength());
            foreach (var enumDefinition in Enums.SafeEnumerable())
            {
                enumMap.Add(enumDefinition.Name, enumDefinition);
            }
            EnumTypeReference.ResolveTypes(enumMap);

            Tasks.FinalizeItems(); // Then Tasks
            
            // Then Console Application
            Dictionary<String, Task> taskMap = new Dictionary<String, Task>((int)Tasks.SafeLength());
            foreach (var task in Tasks.SafeEnumerable())
            {
                taskMap.Add(task.Name, task);
            }
            ConsoleApplications.FinalizeItems(taskMap);
        }

        /*
        public static GtiXml DeserializeFrom(String filename)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(GtiXml));
            using (XmlReader xmlReader = XmlReader.Create(new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                return (GtiXml)xmlSerializer.Deserialize(xmlReader, xmlSerializerEvents);
            }
        }
        */

    }

    public class Named
    {
        [XmlAttribute]
        public String Name;
    }
    public interface IFinalizable
    {
        void FinalizeDeserialization();
    }
    public interface IFinalizableWithMap
    {
        void FinalizeDeserialization(Dictionary<String, Task> taskMap);
    }
    public static class FinalizableExtensions
    {
        public static void FinalizeItems(this IEnumerable<IFinalizable> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.FinalizeDeserialization();
                }
            }
        }
        public static void FinalizeItems(this IEnumerable<IFinalizableWithMap> items, Dictionary<String, Task> taskMap)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    item.FinalizeDeserialization(taskMap);
                }
            }
        }
    }

    public class EnumDefinition : Named, IFinalizable
    {
        [XmlElement("Value")]
        public EnumValue[] Values;

        public void FinalizeDeserialization()
        {
            Values.FinalizeItems();
        }
    }
    public class EnumValue : Named, IFinalizable
    {
        public void FinalizeDeserialization()
        {
            if (String.IsNullOrEmpty(Name))
            {
                throw new FormatException("Missing required attribute 'Name'");
            }
        }
    }

    public class EnumTypeReference
    {
        public const String TypePrefix = "Enum:";

        static readonly Dictionary<String, EnumTypeReference> ReferenceMap =
            new Dictionary<String, EnumTypeReference>();
        static readonly List<EnumTypeReference> ReferenceList = new List<EnumTypeReference>();
        public static EnumTypeReference LookupWithPrefix(String withPrefix)
        {
            EnumTypeReference type;
            if (!ReferenceMap.TryGetValue(withPrefix, out type))
            {
                String nameNoPrefix = withPrefix.Substring(TypePrefix.Length);
                type = new EnumTypeReference(nameNoPrefix);
                ReferenceMap.Add(nameNoPrefix, type);
                ReferenceMap.Add(withPrefix, type);
                ReferenceList.Add(type);
            }
            return type;
        }
        public static void ResolveTypes(Dictionary<String, EnumDefinition> enumMap)
        {
            foreach (var enumTypeReference in ReferenceList)
            {
                if (!enumMap.TryGetValue(enumTypeReference.nameNoPrefix, out enumTypeReference.definition))
                {
                    throw new FormatException(String.Format("Enum Type '{0}' was not defined", enumTypeReference.nameNoPrefix));
                }
            }
        }

        public readonly String nameNoPrefix;
        public EnumDefinition definition;
        EnumTypeReference(String nameNoPrefix)
        {
            this.nameNoPrefix = nameNoPrefix;
        }
    }

    public class Task : Named, IFinalizable
    {
        [XmlElement]
        public String Description;

        [XmlElement("Parameter")]
        public Parameter[] Parameters;

        [XmlIgnore]
        public Dictionary<String, Parameter> ParameterNameMap;

        [XmlElement("Flag")]
        public Flag[] Flags;

        public void FinalizeDeserialization()
        {
            Parameters.FinalizeItems();
            ParameterNameMap = new Dictionary<String, Parameter>((int)Parameters.SafeLength());
            foreach (var parameter in Parameters.SafeEnumerable())
            {
                if(!String.IsNullOrEmpty(parameter.Name))
                {
                    ParameterNameMap.Add(parameter.Name, parameter);
                }
            }
        }
    }

    public partial class Parameter : Named, IFinalizable
    {
        [XmlIgnore]
        public ParameterType type;
        [XmlAttribute("Type")]
        public String typeString
        {
            get { return type.ToString(); }
            set
            {
                if (value.StartsWith(EnumTypeReference.TypePrefix))
                {
                    type = ParameterType.Enum;
                    TypeEnumReference = EnumTypeReference.LookupWithPrefix(value);
                }
                else
                {
                    type = (ParameterType)Enum.Parse(typeof(ParameterType), value, false);
                }
            }
        }
        [XmlIgnore]
        public EnumTypeReference TypeEnumReference;

        [XmlIgnore]
        public Boolean optional;
        [XmlAttribute("Optional")]
        public String optionalString
        {
            get { return optional.ToXml(); }
            set { optional = XmlSerial.ParseBoolean(value); }
        }

        [XmlIgnore]
        public Boolean multiple;
        [XmlAttribute("Multiple")]
        public String multipleString
        {
            get { return multiple.ToXml(); }
            set { multiple = XmlSerial.ParseBoolean(value); }
        }


        public void FinalizeDeserialization()
        {
        }
    }
    public class Flag : Named
    {
    }


    public class ConsoleApplication : Named, IFinalizableWithMap
    {
        [XmlElement("TaskImplementation")]
        public ConsoleTaskImplementation[] TaskImplementations;

        public void FinalizeDeserialization(Dictionary<String, Task> taskMap)
        {
            foreach(var impl in TaskImplementations.SafeEnumerable())
            {
                impl.FinalizeDeserialization(this, taskMap);
            }
        }
    }
    public class ConsoleTaskImplementation
    {
        [XmlAttribute]
        public String TaskName;
        [XmlIgnore]
        public Task Task;

        [XmlElement("CustomArguments")]
        public ConsoleCustomArguments CustomArguments;

        public void FinalizeDeserialization(ConsoleApplication app, Dictionary<String,Task> taskMap)
        {
            if (!taskMap.TryGetValue(TaskName, out Task))
            {
                throw new InvalidOperationException(String.Format("ConsoleApplication '{0}' implement non-exists task '{1}'", app.Name, TaskName));
            }
            if (CustomArguments != null)
            {
                CustomArguments.FinalizeDeserialization(Task);
            }
        }
    }
    public class ConsoleCustomArguments
    {
        [XmlElement("Argument")]
        public ConsoleCustomArgument[] Arguments;

        public void FinalizeDeserialization(Task task)
        {
            foreach (var arg in Arguments.SafeEnumerable())
            {
                arg.FinalizeDeserialization(task);
            }
        }
    }


    public class ConsoleCustomArgument
    {
        [XmlAttribute("Condition")]
        public String conditionString;

        [XmlText]
        public String Text;

        public void FinalizeDeserialization(Task task)
        {
            Console.WriteLine("Need to process argument text '{0}'", Text);
        }
    }


    public static class XmlSerial
    {
        public static String ToXml(this Boolean value)
        {
            return value ? "true" : "false";
        }
        public static Boolean ParseBoolean(String xml)
        {
            if (xml.Length == 4 &&
                xml[0] == 't' &&
                xml[1] == 'r' &&
                xml[2] == 'u' &&
                xml[3] == 'e')
            {
                return true;
            }
            if (xml.Length == 5 &&
                xml[0] == 'f' &&
                xml[1] == 'a' &&
                xml[2] == 'l' &&
                xml[3] == 's' &&
                xml[4] == 'e')
            {
                return false;
            }

            throw new FormatException(String.Format("Expected 'true', or 'false', but got '{0}'", xml));
        }
    }
}