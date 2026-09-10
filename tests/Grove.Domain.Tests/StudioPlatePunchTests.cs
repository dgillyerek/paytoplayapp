using Grove.Domain.Art;

namespace Grove.Domain.Tests;

public sealed class StudioPlatePunchTests
{
    [Fact]
    public void Cream_plate_with_a_red_seed_punches_corners_and_keeps_the_object()
    {
        const int w = 32;
        const int h = 32;
        var rgba = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                rgba[i] = 245;
                rgba[i + 1] = 235;
                rgba[i + 2] = 220;
                rgba[i + 3] = 255;
            }
        }

        for (var y = 11; y < 21; y++)
        {
            for (var x = 11; x < 21; x++)
            {
                var i = (y * w + x) * 4;
                rgba[i] = 196;
                rgba[i + 1] = 90;
                rgba[i + 2] = 60;
                rgba[i + 3] = 255;
            }
        }

        var punched = StudioPlatePunch.Punch(rgba, w, h);
        Assert.True(punched > 100);
        Assert.True(StudioPlatePunch.CornersTransparent(rgba, w, h));
        var center = ((h / 2) * w + w / 2) * 4;
        Assert.True(rgba[center + 3] > 200);
        Assert.Equal(196, rgba[center]);
        Assert.False(StudioPlatePunch.ShouldPunch("UI_OrderTray_Card", "hud"));
        Assert.True(StudioPlatePunch.ShouldPunch("WF_T02_Sprout", "piece"));
    }

    [Fact]
    public void Already_transparent_corners_are_left_alone()
    {
        const int w = 16;
        const int h = 16;
        var rgba = new byte[w * h * 4];
        for (var i = 3; i < rgba.Length; i += 4)
        {
            rgba[i] = 0;
        }

        rgba[(8 * w + 8) * 4 + 3] = 255;
        Assert.Equal(0, StudioPlatePunch.Punch(rgba, w, h));
        Assert.Equal(255, rgba[(8 * w + 8) * 4 + 3]);
    }

    [Fact]
    public void Opaque_rect_crops_to_the_object_and_clear_rgb_kills_plate_color()
    {
        const int w = 32;
        const int h = 32;
        var rgba = new byte[w * h * 4];
        for (var i = 0; i < w * h; i++)
        {
            rgba[i * 4] = 245;
            rgba[i * 4 + 1] = 235;
            rgba[i * 4 + 2] = 220;
            rgba[i * 4 + 3] = 0;
        }

        for (var y = 12; y < 20; y++)
        {
            for (var x = 10; x < 18; x++)
            {
                var i = (y * w + x) * 4;
                rgba[i] = 196;
                rgba[i + 1] = 90;
                rgba[i + 2] = 60;
                rgba[i + 3] = 255;
            }
        }

        Assert.True(StudioPlatePunch.TryOpaqueRect(rgba, w, h, 16, 2, out var x0, out var y0, out var rw, out var rh));
        Assert.Equal(8, x0);
        Assert.Equal(10, y0);
        Assert.Equal(12, rw);
        Assert.Equal(12, rh);

        var cleared = StudioPlatePunch.ClearTransparentRgb(rgba, w, h);
        Assert.True(cleared > 100);
        Assert.Equal(0, rgba[0]);
        Assert.Equal(0, rgba[1]);
        Assert.Equal(0, rgba[2]);
        var center = ((h / 2) * w + w / 2) * 4;
        Assert.Equal(196, rgba[center]);
        Assert.Equal(255, rgba[center + 3]);
    }
}
