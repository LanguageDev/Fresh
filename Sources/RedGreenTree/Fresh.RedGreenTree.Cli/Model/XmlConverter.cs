// Copyright (c) 2022 Fresh.
// Licensed under the Apache License, Version 2.0.
// Source repository: https://github.com/LanguageDev/Fresh

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Fresh.RedGreenTree.Cli.Model;

/// <summary>
/// Converts an XML string to the tree model
/// </summary>
public static class XmlConverter
{
    [XmlRoot("Tree")]
    public sealed class TreeXml
    {
        [XmlAttribute]
        public string? Root { get; set; }

        [XmlAttribute]
        public string? Namespace { get; set; }

        [XmlAttribute]
        public string? Factory { get; set; }

        [XmlElement("Using")]
        public List<UsingXml> Usings { get; set; } = new();

        [XmlElement("Primitive")]
        public List<PrimitiveXml> Primitives { get; set; } = new();

        [XmlElement("Node")]
        public List<NodeXml> Nodes { get; set; } = new();
    }

    public sealed class UsingXml
    {
        [XmlAttribute]
        public string? Namespace { get; set; }
    }

    public sealed class PrimitiveXml
    {
        [XmlAttribute]
        public string? Name { get; set; }
    }

    public sealed class NodeXml
    {
        [XmlAttribute]
        public string? Name { get; set; }

        [XmlAttribute]
        public bool IsAbstract { get; set; } = false;

        [XmlAttribute]
        public string? Base { get; set; }

        [XmlElement("Attribute")]
        public List<AttributeXml> Attributes { get; set; } = new();
    }

    public sealed class AttributeXml
    {
        [XmlAttribute]
        public string? Name { get; set; }

        [XmlAttribute]
        public string? Type { get; set; }
    }

    /// <summary>
    /// Converts the given XML string to the tree model.
    /// </summary>
    /// <param name="xml">The XML text to convert.</param>
    /// <returns>The converted tree model.</returns>
    public static Tree Convert(string xml)
    {
        var serializer = new XmlSerializer(typeof(TreeXml));
        var xmlTree = (TreeXml?)serializer.Deserialize(new StringReader(xml)) ?? throw new InvalidOperationException();
        throw new NotImplementedException();
    }
}
