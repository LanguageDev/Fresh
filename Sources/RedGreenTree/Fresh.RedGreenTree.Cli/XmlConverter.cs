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
using Fresh.RedGreenTree.Cli.Model;
using Attribute = Fresh.RedGreenTree.Cli.Model.Attribute;

namespace Fresh.RedGreenTree.Cli;

/// <summary>
/// Converts an XML string to the tree model
/// </summary>
public sealed class XmlConverter
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

        [XmlElement("Node")]
        public List<NodeXml> Nodes { get; set; } = new();
    }

    public sealed class UsingXml
    {
        [XmlAttribute]
        public string? Namespace { get; set; }
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

    private Dictionary<string, Node> convertedNodes = new();

    private Tree Convert(TreeXml treeXml)
    {
        this.convertedNodes = treeXml.Nodes
            .Select(this.Convert)
            .ToDictionary(n => n.Name);
        return new(
            Root: this.convertedNodes[treeXml.Root ?? throw new InvalidOperationException("'Root' attribute missing from Tree XML tag")],
            Namespace: treeXml.Namespace,
            Factory: treeXml.Factory,
            Usings: treeXml.Usings.Select(this.Convert).ToHashSet(),
            // NOTE: We do this to get them in declaration order
            Nodes: treeXml.Nodes.Select(n => this.convertedNodes[n.Name!]).ToList());
    }

    private string Convert(UsingXml usingXml) => usingXml.Namespace
                                              ?? throw new InvalidOperationException("'Namespace' attribute missing from Using XML tag");

    private Node Convert(NodeXml nodeXml) => new(
        Name: nodeXml.Name ?? throw new InvalidOperationException("'Name' attribute missing from Node XML tag"),
        IsAbstract: nodeXml.IsAbstract,
        Base: (nodeXml.Base is null || !this.convertedNodes.TryGetValue(nodeXml.Base, out var node)) ? null : node,
        Attributes: nodeXml.Attributes.Select(this.Convert).ToList());

    private Attribute Convert(AttributeXml attributeXml) => new(
        Name: attributeXml.Name ?? throw new InvalidOperationException("'Name' attribute missing from Attribute XML tag"),
        Type: attributeXml.Type ?? throw new InvalidOperationException("'Type' attribute missing from Attribute XML tag"));
}
