using System.Text.Json;
using GameplaySamples.LayeredLayout;

var json = new JsonSerializerOptions { IncludeFields = true };
LayerDocument Read(string filename) =>
    JsonSerializer.Deserialize<LayerDocument>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, filename)), json);

void Check(bool passed, string expected)
{
    if (!passed)
        throw new InvalidOperationException(expected);
}

void Reject(LayerDocument document, string fragment)
{
    Check(!LayerMap.TryCreate(document, out LayerMap invalid, out string reason) &&
        invalid == null && reason.Contains(fragment, StringComparison.Ordinal), fragment);
}

LayerDocument document = Read("Valid.json");
Check(LayerMap.TryCreate(document, out LayerMap layout, out _), "The valid artwork fixture should import.");
Check(layout.Layers.Select(item => item.Name).SequenceEqual(new[] { "item_key", "item_satchel", "item_beacon" }),
    "The layout should use layer order, independent of source array order.");
Check(layout.Layers[0].PixelX == 102 && layout.Layers[0].PixelY == 86, "Source position must survive compilation.");
Check(LayerMap.TryCreate(Read("Moved.json"), out LayerMap moved, out _), "The moved layer fixture should import.");
Check(moved.Layers.Take(2).SequenceEqual(layout.Layers.Take(2)) && moved.Layers[2].Name == "item_beacon" &&
    moved.Layers[2].PixelX == 344 && moved.Layers[2].PixelY == 160 && moved.Layers[2].Order == 3,
    "Moving one source layer must preserve the other positions and order.");

Reject(Read("Invalid.json"), "falls outside the canvas");
Reject(Read("InvalidName.json"), "Layer 1 ('beacon') needs a name of the form item_something");
document.Layers[0].Name = "beacon";
Reject(document, "item_something");
document.Layers[0].Name = document.Layers[1].Name;
Reject(document, "Duplicate layer name");
document.Layers[0].Name = "item_beacon";
document.Layers[0].Order = document.Layers[1].Order;
Reject(document, "positive, unique layer order");
document.Width = 0;
Reject(document, "canvas width and height");

Console.WriteLine("Layered Layout PASS: source positions, moved layer, stable order, bounds, named diagnostics and duplicates.");
