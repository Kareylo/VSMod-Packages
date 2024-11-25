namespace Packages.Models;

public class Item
{
    public Item()
    {
        Quantity = 1;
    }
    
    public string Name { get; set; }
    public int Quantity { get; set; }
}