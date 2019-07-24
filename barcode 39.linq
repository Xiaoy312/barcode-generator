<Query Kind="Program">
  <NuGetReference>Rx-Main</NuGetReference>
  <NuGetReference>Splat</NuGetReference>
  <NuGetReference>System.Linq.Dynamic</NuGetReference>
  <Namespace>LINQPad.ObjectModel</Namespace>
  <Namespace>Splat</Namespace>
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Drawing2D</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Drawing.Text</Namespace>
  <Namespace>System.Reactive.Disposables</Namespace>
</Query>

void Main()
{
    InitializeResources();
    
    GenerationTest();
    Generate2018Labels();
}

IEnumerable<TeaModel> Get2018SpringTeas()
{
    var infusionInfos = new Dictionary<string, InfusionInfo>
    {
        ["green-family"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 80, Duration = "2", InfusionNumber = 3 },
        //["template"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 0, Duration = "0", InfusionNumber = 0 },

        ["An Ji Bai Cha"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 70, Duration = "2", InfusionNumber = 3 },
        ["Golden Eyebrow"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 100, Duration = "1-2", InfusionNumber = 5 },
        ["Silver Needle-family"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 60, Duration = "5-10", InfusionNumber = 3 },
        ["White dragon ball"] = new InfusionInfo { TeaQuantity = 2, WaterQuantity = 100, WaterTemperature = 80, Duration = "2-3", InfusionNumber = 3 },
    };

    return @"
An Ji Bai Cha,,10150,An Ji Bai Cha
Puits du dragon de l'Outest,Dragon's well West Lake,10151,green-family
ShiFeng LongJing,,10152,green-family
ShiFeng LongJing+,,10153,green-family
Green spring spirle,Spirial verte du printanier,10154,green-family
White dragon ball,Balle blanche du dragon,10410,White dragon ball
Plum & Orchid,Prune et orchidées,10155,green-family
Silver Needle,Aiguille d'argent,10411,Silver Needle-family
Silver Needle king,Roi de l'Aiguille d'argent,10412,Silver Needle-family
Golden Eyebrow,Sourcil d'or,10323,Golden Eyebrow
".Trim().Split('\n')
    .Select(x => x.Trim().Split(','))
    .SelectMany(arr => Enumerable.Range(1, 2).Select(x => new TeaModel
    {
        Name = $"{arr[0]} 2018",
        NameLocalized = string.IsNullOrEmpty(arr[1]) ? null : $"{arr[1]} 2018",
        SKU = int.Parse(arr[2]) * 100 + x,
        Size = 50 * x,
        InfusionInfo = infusionInfos.ContainsKey(arr[3]) ? infusionInfos[arr[3]] : null,
    }));
}
void GenerationTest()
{
    var blueprint = LabelBlueprint<TeaModel>.Load(@"D:\Code\LinqPad Queries\develop\barcode\res\tea label.l10n.blueprint");
    Util.OnDemand("blueprint", () => blueprint).Dump();

    var tea = new TeaModel
    {
        Name = "Plum & Orchid 2017",
        NameLocalized = "Plum & Orchidée",
        SKU = 1014102,
        Size = 50,
        InfusionInfo = new InfusionInfo
        {
            TeaQuantity = 2,
            WaterQuantity = 100,
            WaterTemperature = 80,
            Duration = "2",
            InfusionNumber = 3,
        },
    };

    blueprint.Create(tea)
        .Dump()
        .Save($@"D:\temp\label test\{tea.SKU}.generated.jpg", ImageFormat.Jpeg);
}
void Generate2018Labels()
{
    var isLocalizedToBlueprint = new Dictionary<bool, LabelBlueprint<TeaModel>>
    {
        [false] = LabelBlueprint<TeaModel>.Load(@"D:\Code\LinqPad Queries\develop\barcode\res\tea label.blueprint"),
        [true] = LabelBlueprint<TeaModel>.Load(@"D:\Code\LinqPad Queries\develop\barcode\res\tea label.l10n.blueprint"),
    };

    var teas = Get2018SpringTeas();
    foreach (var tea in teas)
    {
        var blueprint = isLocalizedToBlueprint[tea.NameLocalized != null];
        
        blueprint.Create(tea)
            .Save($@"D:\temp\label test\{tea.SKU}.generated.jpg", ImageFormat.Jpeg);
    }
}

void InitializeResources()
{
    var customFonts = Directory.GetFiles(Util.CurrentQuery.Find(@"res\custom fonts\"), "*.ttf")
       .Aggregate(new PrivateFontCollection(), (collection, path) =>
       {
           collection.AddFontFile(path);
           return collection;
       });
    var fontFamilyMapper = Enumerable
        .Concat(customFonts.Families, FontFamily.Families)
        .ToDictionary(x => x.Name);

    Locator.CurrentMutable.RegisterConstant<Dictionary<string, FontFamily>>(fontFamilyMapper);
}

public class ProductModel
{
    public string Name { get; set; }
    public string NameLocalized { get; set; }
    public int SKU { get; set; }
}
public class TeaModel : ProductModel
{
    public int Size { get; set; }

    public InfusionInfo InfusionInfo { get; set; }
}
public class InfusionInfo
{
    public double TeaQuantity { get; set; }
    public double WaterQuantity { get; set; }

    public double WaterTemperature { get; set; }
    public string Duration { get; set; }

    public int InfusionNumber { get; set; }
}

public class LabelBlueprint<TModel> where TModel : ProductModel
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int XDpi { get; set; }
    public int YDpi { get; set; }
    public Color Background { get; set; }

    public List<DrawingInstruction<TModel>> Instructions { get; set; }

    public Image Create(TModel model)
    {
        var canvas = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
        canvas.SetResolution(XDpi, YDpi);

        using (var g = Graphics.FromImage(canvas))
        {
            g.Clear(Background);

            Instructions.ForEach(x => x.Draw(g, model));
        }

        return canvas;
    }

    public static LabelBlueprint<TModel> Load(string uri) => Parse(XElement.Load(uri, LoadOptions.SetLineInfo));
    public static LabelBlueprint<TModel> Parse(XElement xml)
    {
        var layout = new LabelBlueprint<TModel>
        {
            Width = (int)xml.Attribute("Width"),
            Height = (int)xml.Attribute("Height"),
            XDpi = (int)xml.Attribute("XDpi"),
            YDpi = (int)xml.Attribute("YDpi"),
            Background = ColorTranslator.FromHtml((string)xml.Attribute("Background")),
        };
        layout.Instructions = xml.Elements()
            .Select(DrawingInstruction<TModel>.Parse)
            .ToList();

        return layout;
    }
}
public abstract class DrawingInstruction<TModel>
{
    public Rectangle DesiredArea { get; set; }
    public ContentAlignment Alignment { get; set; }

    public abstract void Draw(Graphics g, TModel model);

    private static readonly Dictionary<String, Type> InstructionTypes = Assembly.GetCallingAssembly().GetTypes()
        .Where(x => x.BaseType?.IsGenericType ?? false && x.BaseType.GetGenericTypeDefinition() == typeof(DrawingInstruction<>))
        .ToDictionary(x => Regex.Replace(x.Name, "Instruction(`1)?$", string.Empty));
    public static DrawingInstruction<TModel> Parse(XElement xml)
    {
        var name = xml.Name.LocalName;
        if (!InstructionTypes.ContainsKey(name))
        {
            var line = (IXmlLineInfo)xml;
            var reference = line.HasLineInfo() ? $"on line {line.LineNumber} at {line.LinePosition}" : null;

            throw new ArgumentException($"Invalid DrawingInstruction: {name} {reference}".Trim());
        };

        try
        {
            var instruction = (DrawingInstruction<TModel>)InstructionTypes[xml.Name.LocalName]
                .MakeGenericType(typeof(TModel))
                .InvokeMember("Parse", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new[] { xml });
            instruction.Alignment = (ContentAlignment)Enum.Parse(typeof(ContentAlignment), (string)xml.Attribute("Alignment") ?? nameof(ContentAlignment.TopLeft));
            instruction.DesiredArea = DesiredAreaHelper.Parse((string)xml.Attribute("DesiredArea"));

            return instruction;
        }
        catch (Exception e)
        {
            var line = (IXmlLineInfo)xml;
            var reference = line.HasLineInfo() ? $"on line {line.LineNumber}" : null;

            throw new ArgumentException($"Error while parsing {name} {reference}: {e.Message}", e);
        }
    }
}
public class DrawTextInstruction<TModel> : DrawingInstruction<TModel> where TModel : ProductModel
{
    public string Text { get; set; }
    public Font Font { get; set; }
    public Color Background { get; set; }

    public override void Draw(Graphics g, TModel model)
    {
        var text = FormatText(model);

        var canvas = GenerateCanvas();
        using (var gCanvas = Graphics.FromImage(canvas))
        {
            gCanvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            gCanvas.Clear(Background);
            gCanvas.DrawString(text, Font, Brushes.Black, 0, 0);
        }
        using (g.SnapshotSettings())
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.CompositingQuality = CompositingQuality.AssumeLinear;
            g.SmoothingMode = SmoothingMode.None;

            g.DrawImage(canvas,
                DrawingHelper.Align(DesiredArea, Alignment, canvas.Size),
                new Rectangle(Point.Empty, canvas.Size),
                GraphicsUnit.Pixel);
        }

        Bitmap GenerateCanvas()
        {
            using (var g0 = Graphics.FromHwndInternal(IntPtr.Zero))
            {
                g0.TextRenderingHint = TextRenderingHint.AntiAlias;
                var size = g0.MeasureString(text, Font);

                return new Bitmap((int)size.Width, (int)size.Height);
            }
        }
    }
    private string FormatText(TModel model)
    {
        return Regex.Replace(Text, @"{(?<expr>[^}]+)}", m =>
        {
            var parameter = Expression.Parameter(typeof(TModel), nameof(model));
            var expression = System.Linq.Dynamic.DynamicExpression.ParseLambda(
                new[] { parameter },
                null,
                m.Groups["expr"].Value
            );

            return expression.Compile().DynamicInvoke(model)?.ToString();
        });
    }

    public static new DrawTextInstruction<TModel> Parse(XElement xml)
    {
        return new DrawTextInstruction<TModel>()
        {
            Text = (string)xml.Attribute("Text"),
            Font = FontParser.Parse((string)xml.Attribute("Font")),
            Background = ColorTranslator.FromHtml((string)xml.Attribute("Background")),
        };
    }
}
public class DrawBarcodeInstruction<TModel> : DrawingInstruction<TModel> where TModel : ProductModel
{
    public int BrushSize { get; set; }

    public override void Draw(Graphics g, TModel model)
    {
        Code39.DrawBarcode(g, DesiredArea, Alignment, model.SKU.ToString(), BrushSize, addCheckSum: false);
    }

    public static new DrawBarcodeInstruction<TModel> Parse(XElement xml)
    {
        return new DrawBarcodeInstruction<TModel>()
        {
            BrushSize = (int)xml.Attribute("BrushSize"),
        };
    }
}

void PaintCustomString(Graphics srcG, string text, Rectangle desiredArea, int scale)
{
    const TextRenderingHint TextRenderingHint = TextRenderingHint.AntiAlias;
    var font = new Font("Arial", 11, FontStyle.Bold, GraphicsUnit.Pixel);

    var canvas = GenerateCanvas();
    using (var g = Graphics.FromImage(canvas))
    {
        g.TextRenderingHint = TextRenderingHint;

        g.Clear(Color.White);
        g.DrawString(text, font, Brushes.Black, 0, 0);
    }

    using (srcG.SnapshotSettings())
    {
        srcG.InterpolationMode = InterpolationMode.NearestNeighbor;
        srcG.CompositingQuality = CompositingQuality.AssumeLinear;
        srcG.SmoothingMode = SmoothingMode.None;

        srcG.DrawImage(canvas,
            new Rectangle(
                desiredArea.Left + Math.Max(0, desiredArea.Width - canvas.Width * scale) / 2,
                desiredArea.Top,
                canvas.Size.Width * scale,
                canvas.Size.Height * scale),
            new Rectangle(Point.Empty, canvas.Size),
            GraphicsUnit.Pixel);
    }

    Bitmap GenerateCanvas()
    {
        using (var g = Graphics.FromHwndInternal(IntPtr.Zero))
        {
            g.TextRenderingHint = TextRenderingHint;
            var size = g.MeasureString(text, font);

            return new Bitmap((int)size.Width, (int)size.Height);
        }
    }
}

public static class Code39
{
    private static IReadOnlyDictionary<char, Code39CharDefinition> _encoding = Code39CharDefinition.GenerateDefinitions();
    private static char[] ValidCharacters = _encoding.Where(x => x.Value.CheckValue.HasValue).Select(x => x.Key).ToArray();

    private static void ValidateString(string input)
    {
        if (!IsValidString(input))
            throw new ArgumentException("Input contains the following invalid characters: " +
                string.Join(", ", GetInvalidChars(input)));
    }
    public static bool IsValidString(string input)
    {
        return input.All(ValidCharacters.Contains);
    }
    public static IEnumerable<char> GetInvalidChars(string input)
    {
        return input.ToCharArray()
            .Distinct()
            .Where(c => !ValidCharacters.Contains(c))
            .Distinct();
    }


    public static int ComputeChecksum(string input)
    {
        ValidateString(input);

        var checksum = input.Sum(c => _encoding[c].CheckValue.Value);

        return _encoding.Values.Single(x => x.CheckValue == checksum % 43).Char;
    }

    public static Bitmap GenerateBarcode(string input, int unitWidth, int height, bool addCheckSum = true)
    {
        if (height <= 0) throw new ArgumentException("height must be greater than 0");
        ValidateString(input);

        input = addCheckSum ? $"*{input}{ComputeChecksum(input)}*" : $"*{input}*";
        var barcode = string.Join("_", input.Select(c => _encoding[c].BarcodeEncoding));

        var bitmap = new Bitmap(barcode.Length * unitWidth, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.FillRectangle(Brushes.White, 0, 0, bitmap.Width, bitmap.Height);

            var pen = new Pen(Color.Black) { Width = unitWidth, Alignment = PenAlignment.Right, };

            var bars = barcode
                .Select((c, i) => new { Code = c, Left = i * unitWidth })
                .Where(x => x.Code != '_') // debug separator
                .Where(x => x.Code != '0');
            foreach (var bar in bars)
            {
                g.FillRectangle(
                    bar.Code == '1' ? Brushes.Black : Brushes.Pink,
                    bar.Left, 0, unitWidth, height);
            }
        }

        return bitmap;
    }
    public static void DrawBarcode(Graphics g, Rectangle desiredArea, ContentAlignment alignment, string input, int brushSize, bool addCheckSum = true)
    {
        if (desiredArea.Height <= 0) throw new ArgumentException("height must be greater than 0");
        ValidateString(input);

        input = addCheckSum ? $"*{input}{ComputeChecksum(input)}*" : $"*{input}*";
        var barcode = string.Join("_", input.Select(c => _encoding[c].BarcodeEncoding));

        var size = new Size(barcode.Length * brushSize, desiredArea.Height);
        var location = DrawingHelper.Align(desiredArea, alignment, size).Location;

        var pen = new Pen(Color.Black) { Width = brushSize, Alignment = PenAlignment.Right, };

        var bars = barcode
            .Select((c, i) => new { Code = c, Left = i * brushSize })
            .Where(x => x.Code == '1');
        foreach (var bar in bars)
        {
            g.FillRectangle(
                bar.Code == '1' ? Brushes.Black : Brushes.Pink,
                location.X + bar.Left, location.Y, brushSize, desiredArea.Height);
        }
    }
}
public partial class Code39CharDefinition
{
    public int? CheckValue { get; private set; }
    public char Char { get; private set; }
    public string WidthEncoding { get; private set; }
    public string BarcodeEncoding { get; private set; }
}
public partial class Code39CharDefinition
{
    // credit: www.barcodeisland.com/code39.phtml
    private const string Map =
    @"  0	0	NNNWWNWNN	101001101101	22	M	WNWNNNNWN	110110101001
        1	1	WNNWNNNNW	110100101011	23	N	NNNNWNNWW	101011010011
        2	2	NNWWNNNNW	101100101011	24	O	WNNNWNNWN	110101101001
        3	3	WNWWNNNNN	110110010101	25	P	NNWNWNNWN	101101101001
        4	4	NNNWWNNNW	101001101011	26	Q	NNNNNNWWW	101010110011
        5	5	WNNWWNNNN	110100110101	27	R	WNNNNNWWN	110101011001
        6	6	NNWWWNNNN	101100110101	28	S	NNWNNNWWN	101101011001
        7	7	NNNWNNWNW	101001011011	29	T	NNNNWNWWN	101011011001
        8	8	WNNWNNWNN	110100101101	30	U	WWNNNNNNW	110010101011
        9	9	NNWWNNWNN	101100101101	31	V	NWWNNNNNW	100110101011
        10	A	NNWWNNWNN	110101001011	32	W	WWWNNNNNN	110011010101
        11	B	NNWNNWNNW	101101001011	33	X	NWNNWNNNW	100101101011
        12	C	WNWNNWNNN	110110100101	34	Y	WWNNWNNNN	110010110101
        13	D	NNNNWWNNW	101011001011	35	Z	NWWNWNNNN	100110110101
        14	E	WNNNWWNNN	110101100101	36	-	NWNNNNWNW	100101011011
        15	F	NNWNWWNNN	101101100101	37	.	WWNNNNWNN	110010101101
        16	G	NNNNNWWNW	101010011011	38	 	NWWNNNWNN	100110101101
        17	H	WNNNNWWNN	110101001101	39	$	NWNWNWNNN	100100100101
        18	I	NNWNNWWNN	101101001101	40	/	NWNWNNNWN	100100101001
        19	J	NNNNWWWNN	101011001101	41	+	NWNNNWNWN	100101001001
        20	K	WNNNNNNWW	110101010011	42	%	NNNWNWNWN	101001001001
        21	L	NNWNNNNWW	101101010011	n/a	*	NWNNWNWNN	100101101101";

    public static IReadOnlyDictionary<char, Code39CharDefinition> GenerateDefinitions()
    {
        return Map
            .Split('\n')
            .Select(x => x.Trim().Split('\t'))
            .SelectMany(x => Enumerable.Range(0, 2).Select(i => new Code39CharDefinition
            {
                CheckValue = int.TryParse(x[i * 4 + 0], out var result) ? result : (int?)null,
                Char = x[i * 4 + 1][0],
                WidthEncoding = x[i * 4 + 2],
                BarcodeEncoding = x[i * 4 + 3],
            }))
            .ToDictionary(x => x.Char);
    }
}

public static class GraphicsExtensions
{
    public static IDisposable SnapshotSettings(this Graphics g)
    {
        var settings = new
        {
            g.CompositingMode,
            g.CompositingQuality,
            g.InterpolationMode,
            g.PixelOffsetMode,
            g.SmoothingMode,
            g.TextContrast,
            g.TextRenderingHint
        };

        return Disposable.Create(() =>
        {
            g.CompositingMode = settings.CompositingMode;
            g.CompositingQuality = settings.CompositingQuality;
            g.InterpolationMode = settings.InterpolationMode;
            g.PixelOffsetMode = settings.PixelOffsetMode;
            g.SmoothingMode = settings.SmoothingMode;
            g.TextContrast = settings.TextContrast;
            g.TextRenderingHint = settings.TextRenderingHint;
        });
    }
}
public static class ImageExtensions
{
    public static Bitmap ScaleUp(this Bitmap image, int scale)
    {
        if (scale < 1) throw new ArgumentException();
        if (scale == 1) return image;

        var result = new Bitmap(image.Width * scale, image.Height * scale, image.PixelFormat);
        for (int x = 0; x < image.Width; x++)
            for (int y = 0; y < image.Height; y++)
            {
                var color = image.GetPixel(x, y);

                for (int dx = scale * x; dx < scale * x + scale; dx++)
                    for (int dy = scale * y; dy < scale * y + scale; dy++)
                        result.SetPixel(dx, dy, color);
            }

        return result;
    }
}
public static class DrawingHelper
{
    public static Rectangle Align(Rectangle desiredArea, ContentAlignment alignment, Size actualSize)
    {
        var vAlignMapping = new Dictionary<ContentAlignment, int>
        {
            [ContentAlignment.TopCenter] = desiredArea.Top,
            [ContentAlignment.MiddleCenter] = Center(desiredArea.Top, desiredArea.Bottom, actualSize.Height),
            [ContentAlignment.BottomCenter] = desiredArea.Bottom - actualSize.Height,
        }.ToDictionary(x => (int)Math.Log((int)x.Key, 16), x => x.Value);
        var hAlignMapping = new Dictionary<ContentAlignment, int>
        {
            [ContentAlignment.MiddleLeft] = desiredArea.Left,
            [ContentAlignment.MiddleCenter] = Center(desiredArea.Left, desiredArea.Right, actualSize.Width),
            [ContentAlignment.MiddleRight] = desiredArea.Right - actualSize.Width,
        }.ToDictionary(x => Math.Log((int)x.Key, 2) % 4, x => x.Value);

        var location = new Point(
            hAlignMapping[Math.Log((int)alignment, 2) % 4],
            vAlignMapping[(int)Math.Log((int)alignment, 16)]
        );

        return new Rectangle(location, actualSize);

        int Center(int p0, int p1, int length) => (p0 + p1 - length) / 2;
    }
}
public static class UserQueryExtensions
{
    public static string Find(this Query query, string path)
    {
        return Path.Combine(
            Path.GetDirectoryName(query.FilePath),
            path
        );
    }
}
public static class DesiredAreaHelper
{
    public static Rectangle Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException($"Desired area cannot be empty");

        var values = text.Split(',').Select(x => x.Trim()).ToArray();
        if (values.Length != 4)
            throw new ArgumentException($"Invalid desired area: {text}");

        var coords = values.Select(int.Parse).ToArray();
        var index = 0;
        return Rectangle.FromLTRB(coords[index++], coords[index++], coords[index++], coords[index++]);
    }
}
public static class FontParser
{
    public static Font Parse(string text)
    {
        var match = Regex.Match(text, @"^(?<family>[\w\d\s]+)(, ?(?<size>[\d\.]+)(?<unit>px|pt)?(, ?(?<style>[\w]+))?)?$");
        if (!match.Success)
            throw new ArgumentException($"'{text}' cannot be parsed. The expected format is 'font[, size[unit][, style]]'");

        var fontFamilyMapper = Locator.Current.GetService<Dictionary<string, FontFamily>>();
        var unitMapper = new Dictionary<string, GraphicsUnit>
        {
            ["px"] = GraphicsUnit.Pixel,
            ["pt"] = GraphicsUnit.Point,
            ["in"] = GraphicsUnit.Inch,
            ["mm"] = GraphicsUnit.Millimeter,
        };
        var styleMapper = new Dictionary<string, FontStyle>(
            Enum.GetValues(typeof(FontStyle)).Cast<FontStyle>().ToDictionary(x => x.ToString()),
            StringComparer.OrdinalIgnoreCase
        );

        var family = GetValueOrDefault("family");
        var size = GetValueOrDefault("size", "8.25");
        var unit = GetValueOrDefault("unit", "pt");
        var style = GetValueOrDefault("style", "regular");

        try
        {
            return new Font(
                fontFamilyMapper[family],
                float.Parse(size),
                styleMapper[style],
                unitMapper[unit]
            );
        }
        catch (Exception e)
        {

            throw new ArgumentException($"'{text}' cannot be parsed.", e);
        }

        string GetValueOrDefault(string name, string defaultValue = null) => match.Groups[name].Success ? match.Groups[name].Value : defaultValue;
    }
}