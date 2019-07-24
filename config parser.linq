<Query Kind="Program">
  <Namespace>System.Xml.Serialization</Namespace>
  <Namespace>System.Drawing</Namespace>
</Query>

void Main()
{
    //DrawingInstruction.InstructionParsers["DrawText"](null).Dump();
    //return;
    
    var layout = Blueprint.Load(@"D:\Code\LinqPad Queries\develop\barcode\res\tea label.blueprint").Dump(2);
}

// Define other methods and classes here
public class Blueprint
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int HorizontalResolution { get; set; }
    public int VerticalResolution { get; set; }

    public List<DrawingInstruction> Instructions { get; set; }

    public static Blueprint Load(string uri) => Parse(XElement.Load(uri, LoadOptions.SetLineInfo));
    public static Blueprint Parse(XElement xml)
    {
        var layout = new Blueprint
        {
            Width = (int)xml.Attribute("Width"),
            Height = (int)xml.Attribute("Height"),
            HorizontalResolution = (int)xml.Attribute("HorizontalResolution"),
            VerticalResolution = (int)xml.Attribute("VerticalResolution"),
        };
        layout.Instructions = xml.Elements().Select(DrawingInstruction.Parse).ToList();

        return layout;
    }
}
public partial class DrawingInstruction
{
    public string Hint { get; set; }
    public Rectangle DesiredArea { get; set; }
    public ContentAlignment Alignment { get; set; }

    private static readonly Dictionary<String, Func<XElement, DrawingInstruction>> InstructionParsers = Assembly.GetCallingAssembly().GetTypes()
        .Where(x => x.IsSubclassOf(typeof(DrawingInstruction)))
        .ToDictionary(
            x => Regex.Replace(x.Name, "Instruction$", string.Empty),
            x =>
            {
                var method = x.GetMethod("Parse", new Type[] { typeof(XElement) });
                var parse = (Func<XElement, DrawingInstruction>)method.CreateDelegate(typeof(Func<XElement, DrawingInstruction>), null);

                return parse;
            }
        );
    public static DrawingInstruction Parse(XElement xml)
    {
        var name = xml.Name.LocalName;
        if (!InstructionParsers.ContainsKey(name))
        {
            var line = (IXmlLineInfo)xml;
            var reference = line.HasLineInfo() ? $"on line {line.LineNumber} at {line.LinePosition}" : null;

            throw new ArgumentException($"Invalid DrawingInstruction: {name} {reference}".Trim());
        };

        var instruction = InstructionParsers[xml.Name.LocalName](xml);
        instruction.Hint = (string)xml.Attribute("Hint");
        instruction.Alignment = (ContentAlignment)Enum.Parse(typeof(ContentAlignment), (string)xml.Attribute("Alignment") ?? nameof(ContentAlignment.TopLeft));
        instruction.DesiredArea = (Rectangle)new RectangleConverter().ConvertFromString((string)xml.Attribute("DesiredArea"));

        return instruction;
    }
}
public class DrawTextInstruction : DrawingInstruction
{

    public static new DrawTextInstruction Parse(XElement xml)
    {
        return new DrawTextInstruction();
    }
}
public class DrawBarcodeInstruction : DrawingInstruction
{
    public int Height { get; set; }
    public int BrushSize { get; set; }

    public static new DrawBarcodeInstruction Parse(XElement xml)
    {
        return new DrawBarcodeInstruction()
        {
            Height = (int)xml.Attribute("Height"),
            BrushSize = (int)xml.Attribute("BrushSize"),
        };
    }
}