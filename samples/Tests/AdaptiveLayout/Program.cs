using GameplaySamples.AdaptiveLayout;

var tests = new (string Name, Action Run)[]
{
    ("portrait keeps the whole world visible", PortraitFit),
    ("landscape adds room on the wide axis", LandscapeFit),
    ("asymmetric safe area anchors account for viewport origin", OffsetSafeArea),
    ("safe area is intersected with the camera viewport", SafeAreaOutsideViewport),
    ("safe area does not change the camera frame", IndependentPolicies),
    ("orientation round trips are stable", OrientationRoundTrip),
    ("empty resized viewports keep the last valid layout", InvalidResize),
    ("invalid input is rejected on the first measurement", InvalidFirstMeasurement)
};

foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (InvalidOperationException failure)
    {
        Console.Error.WriteLine($"FAIL {name}: {failure.Message}");
        return 1;
    }
}
return 0;

static void PortraitFit()
{
    var viewport = new LayoutRect(0, 0, 900, 1600);
    var world = new LayoutRect(-2, 3, 12, 7);
    var result = Calculate(viewport, viewport, world);
    CheckRect(result.UiAnchors, 0, 0, 1, 1);
    RequireNear(12, result.CameraBounds.Width);
    RequireNear(12 * 1600f / 900f, result.CameraBounds.Height);
    RequireNear(world.CenterX, result.CameraBounds.CenterX);
    RequireNear(world.CenterY, result.CameraBounds.CenterY);
}

static void LandscapeFit()
{
    var viewport = new LayoutRect(0, 0, 1950, 900);
    var result = Calculate(viewport, viewport, new LayoutRect(-6, -3.5f, 12, 7));
    RequireNear(3.5f, result.OrthographicSize);
    RequireNear(7 * 1950f / 900f, result.CameraBounds.Width);
}

static void OffsetSafeArea()
{
    var viewport = new LayoutRect(100, 50, 1000, 500);
    var safe = new LayoutRect(220, 70, 830, 440);
    var result = Calculate(viewport, safe, new LayoutRect(-6, -3.5f, 12, 7));
    CheckRect(result.UiAnchors, 0.12f, 0.04f, 0.83f, 0.88f);
}

static void SafeAreaOutsideViewport()
{
    var viewport = new LayoutRect(100, 50, 1000, 500);
    var safe = new LayoutRect(0, 100, 900, 500);
    var result = Calculate(viewport, safe, new LayoutRect(-6, -3.5f, 12, 7));
    CheckRect(result.UiAnchors, 0, 0.1f, 0.8f, 0.9f);
}

static void IndependentPolicies()
{
    var viewport = new LayoutRect(0, 0, 900, 1600);
    var world = new LayoutRect(-6, -3.5f, 12, 7);
    LayoutRect fullCamera = Calculate(viewport, viewport, world).CameraBounds;
    LayoutRect safeCamera = Calculate(viewport, new LayoutRect(20, 100, 850, 1400), world).CameraBounds;
    RequireSame(fullCamera, safeCamera);
}

static void OrientationRoundTrip()
{
    var state = new AdaptiveLayoutState();
    var portrait = new LayoutRect(30, 50, 900, 1600);
    var safe = new LayoutRect(80, 120, 830, 1000);
    var landscape = new LayoutRect(0, 0, 1600, 900);
    var world = new LayoutRect(-6, -3.5f, 12, 7);
    Require(state.Update(portrait, safe, world), "Portrait was invalid.");
    LayoutResult initial = state.Current;
    for (int index = 0; index < 20; index++)
    {
        Require(state.Update(landscape, landscape, world), "Landscape was invalid.");
        Require(state.Update(portrait, safe, world), "Portrait was invalid after rotating.");
        RequireSame(initial.UiAnchors, state.Current.UiAnchors);
        RequireSame(initial.CameraBounds, state.Current.CameraBounds);
    }
}

static void InvalidResize()
{
    var state = new AdaptiveLayoutState();
    var viewport = new LayoutRect(10, 30, 600, 1200);
    var world = new LayoutRect(-6, -3.5f, 12, 7);
    Require(state.Update(viewport, viewport, world), "Initial measurement was invalid.");
    var initial = state.Current;
    Require(!state.Update(new LayoutRect(10, 30, 0, 1200), viewport, world), "Empty viewport was accepted.");
    Require(!state.Update(viewport, new LayoutRect(900, 0, 20, 30), world), "Disjoint safe area was accepted.");
    Require(state.HasLayout, "A previously valid layout was lost.");
    RequireSame(initial.UiAnchors, state.Current.UiAnchors);
    RequireSame(initial.CameraBounds, state.Current.CameraBounds);
}

static void InvalidFirstMeasurement()
{
    var state = new AdaptiveLayoutState();
    var viewport = new LayoutRect(10, 30, 600, 1200);
    var invalidWorld = new LayoutRect(-6, -3.5f, float.PositiveInfinity, 7);
    Require(!state.Update(viewport, viewport, invalidWorld), "Non-finite world was accepted.");
    Require(!state.HasLayout, "An invalid first measurement produced a layout.");
}

static LayoutResult Calculate(LayoutRect viewport, LayoutRect safeArea, LayoutRect world)
{
    Require(LayoutGeometry.TryCalculate(viewport, safeArea, world, out LayoutResult result), "Could not compute a layout.");
    return result;
}

static void RequireSame(LayoutRect expected, LayoutRect actual)
{
    Require(expected.X == actual.X && expected.Y == actual.Y &&
        expected.Width == actual.Width && expected.Height == actual.Height, "Layout changed.");
}

static void CheckRect(LayoutRect actual, float x, float y, float width, float height)
{
    RequireNear(x, actual.X);
    RequireNear(y, actual.Y);
    RequireNear(width, actual.Width);
    RequireNear(height, actual.Height);
}

static void RequireNear(float expected, float actual)
{
    Require(Math.Abs(expected - actual) < 0.0001f, $"Expected {expected}, saw {actual}.");
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
