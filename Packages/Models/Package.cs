using System.Collections.Generic;

namespace Packages.Models;

public class Package
{
    public Package()
    {
        Items = new List<Item>();
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public IEnumerable<Item> Items { get; set; }

    public override string ToString()
    {
        return $"{Name} - {Description}";
    }
}